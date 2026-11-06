Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Cheques

    ''' <summary>
    ''' Anular y borrar cheques: CmdAnular_Click y CmdBorrar_Click de SD1501\FrmAnularCheque.frm.
    ''' Las tablas son MyISAM, sin transacciones: si algo falla a la mitad, lo anterior queda hecho.
    ''' A diferencia del original, un fallo en el libro de bancos no se oculta: se propaga.
    ''' </summary>
    Public NotInheritable Class ResultadoMover
        Public Property Cheque As Long
        ''' <summary>El consecutivo ya existía: no se creó el cheque, pero sí su movimiento en el libro.</summary>
        Public Property Repetido As Boolean
    End Class

    Public NotInheritable Class AnulacionCheques

        ''' <summary>Parámetro con los días que puede tener un cheque para que se pueda borrar.</summary>
        Public Const ParametroDiasBorrar As String = "ECCDiasBorrar"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>
        ''' Solo se borra un cheque sin conciliar y dentro del plazo. Sin el parámetro el plazo es 0:
        ''' el 1000 de SD_LoadFromParametros2 es el local global, no una cantidad de días.
        ''' </summary>
        Public Shared Function SePuedeBorrar(cheque As ChequeConsulta, diasPermitidos As Long, hoy As Date) As Boolean
            Return Not cheque.Conciliado AndAlso (hoy.Date - cheque.Fecha.Date).TotalDays <= diasPermitidos
        End Function

        Public Async Function AnularAsync(banco As Integer, cuenta As String, numero As Integer, motivo As String, local As Long, uid As Long, Optional ct As CancellationToken = Nothing) As Task
            Const nulo As String = "INSERT INTO eccchequesnulos (Local, Banco, Cuenta, Cheque, Motivo, UIDC, FC) VALUES (@local, @banco, @cuenta, @numero, @motivo, @uid, CURDATE())"
            Const anulado As String = "UPDATE ecccheques SET anulado = '1' WHERE banco = @banco AND cuenta = @cuenta AND cheque = @numero"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await AnularPagoCxpAsync(cn, banco, cuenta, numero, motivo, ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(nulo, New With {local, banco, cuenta, numero, motivo, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(anulado, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                Await AnularEnLibroAsync(cn, banco, cuenta, numero, local, uid, ct).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function BorrarAsync(banco As Integer, cuenta As String, numero As Integer, motivo As String, local As Long, uid As Long, Optional ct As CancellationToken = Nothing) As Task
            Const cheque As String = "DELETE FROM ecccheques WHERE banco = @banco AND cuenta = @cuenta AND cheque = @numero"
            Const asiento As String = "DELETE FROM eccchequesconta WHERE banco = @banco AND cuenta = @cuenta AND cheque = @numero"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await AnularPagoCxpAsync(cn, banco, cuenta, numero, motivo, ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(cheque, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(asiento, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                Await AnularEnLibroAsync(cn, banco, cuenta, numero, local, uid, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' CmdAnular_Click de SD1501\FrmMoveCheque.frm: anula el cheque y crea otro igual, con fecha de
        ''' hoy y el siguiente consecutivo, y le pasa el recibo de CxP o de café. Dos diferencias con
        ''' Fuentes, según los datos: el asiento va con las 14 columnas reales (SaveDContable manda 15) y
        ''' el cheque nuevo conserva id y codigo. Si el número ya existe sigue adelante como el
        ''' original; las líneas repetidas del asiento se omiten, igual que al fallar allá en silencio.
        ''' </summary>
        Public Async Function MoverAsync(banco As Integer, cuenta As String, cheque As ChequeConsulta, motivo As String, local As Long, estacion As String, uid As Long, Optional ct As CancellationToken = Nothing) As Task(Of ResultadoMover)
            Const nulo As String = "INSERT INTO eccchequesnulos (Local, Banco, Cuenta, Cheque, Motivo, UIDC, FC) VALUES (@local, @banco, @cuenta, @numero, @motivo, @uid, CURDATE())"
            Const anulado As String = "UPDATE ecccheques SET anulado = '1' WHERE banco = @banco AND cuenta = @cuenta AND cheque = @numero"
            Const reciboCxp As String = "UPDATE cxppagos SET dctpago = @nuevo WHERE tipopago = 1 AND banco = @banco AND cuenta = @cuenta AND dctpago = @viejo LIMIT 1"
            Const reciboCafe As String = "UPDATE gpcpagosrecdet SET docdes = @nuevo, descri = @descripcion WHERE origen = 4 AND docdes = @viejo"
            Const existe As String = "SELECT COUNT(*) FROM ecccheques WHERE banco = @banco AND cuenta = @cuenta AND cheque = @nuevo"
            Const nuevoCheque As String = "INSERT INTO ecccheques (local, banco, cuenta, cheque, fecha, id, codigo, nombre, monto, otro, anulado, observaciones, uidc) " &
                                          "VALUES (@local, @banco, @cuenta, @nuevo, @hoy, @id, @codigo, @nombre, @monto, @otro, '0', @detalle, @uid)"
            Const linea As String = "INSERT IGNORE INTO eccchequesconta (local, banco, cuenta, cheque, pos, ccontable, detalle, md, mh, System, contabilizado, tipoac, uidc, fc) " &
                                    "VALUES (@local, @banco, @cuenta, @nuevo, @pos, @contable, @detalle, @debe, @haber, @sistema, '0', @tipoAc, @uid, CURDATE())"
            Const libro As String = "INSERT INTO ccblibro VALUES (NULL, @local, @banco, @cuenta, @hoy, @documento, 2, '0', @monto, @detalle, '0', '1900-01-01', '', @uid, NOW())"

            Dim numero = cheque.Numero
            Dim viejo = numero.ToString(CultureInfo.InvariantCulture)
            Dim hoy = Date.Today
            Dim monto = Math.Round(cheque.Monto, 2, MidpointRounding.AwayFromZero)
            Dim detalle = If(cheque.Observaciones, "")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(nulo, New With {local, banco, cuenta, numero, motivo, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(anulado, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                Await AnularEnLibroAsync(cn, banco, cuenta, numero, local, uid, ct).ConfigureAwait(False)

                Dim nuevo = Await ConsecutivoCheques.SiguienteAsync(cn, local, estacion, banco, cuenta, True, ct).ConfigureAwait(False)
                Dim texto = nuevo.ToString(CultureInfo.InvariantCulture)
                Await cn.ExecuteAsync(New CommandDefinition(reciboCxp, New With {.nuevo = texto, banco, cuenta, viejo}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(reciboCafe, New With {.nuevo = texto, .descripcion = "CHEQUE #" & texto, viejo}, cancellationToken:=ct)).ConfigureAwait(False)

                Dim repetido = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(existe, New With {banco, cuenta, nuevo}, cancellationToken:=ct)).ConfigureAwait(False) > 0
                If Not repetido Then
                    Dim otro = $"{cuenta.Trim()} *-* {texto} *-* {monto.ToString("0.##", CultureInfo.InvariantCulture)}"
                    Await cn.ExecuteAsync(New CommandDefinition(nuevoCheque, New With {local, banco, cuenta, nuevo, hoy, .id = If(cheque.Id, ""), .codigo = If(cheque.Codigo, ""), .nombre = cheque.Nombre, monto, otro, detalle, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                End If

                ' El original guarda en todas las líneas el tipoac de la última que leyó.
                Dim tipoAc = If(cheque.Lineas.Count = 0, 0, cheque.Lineas(cheque.Lineas.Count - 1).TipoAc)
                For i = 0 To cheque.Lineas.Count - 1
                    Dim l = cheque.Lineas(i)
                    Await cn.ExecuteAsync(New CommandDefinition(linea, New With {local, banco, cuenta, nuevo, .pos = i + 1, .contable = l.Cuenta, .detalle = l.Detalle, .debe = l.Debe, .haber = l.Haber, .sistema = l.Sistema, tipoAc, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Next

                Dim detalleLibro = cheque.Nombre & If(detalle.Length > 0, $" ({detalle.Trim()})", "")
                Await cn.ExecuteAsync(New CommandDefinition(libro, New With {local, banco, .cuenta = cuenta.Trim(), hoy, .documento = texto, monto, .detalle = detalleLibro, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Return New ResultadoMover With {.Cheque = nuevo, .Repetido = repetido}
            End Using
        End Function

        ''' <summary>Si el cheque pagó un recibo de CxP, anula el recibo y reabre sus facturas.</summary>
        Private Shared Async Function AnularPagoCxpAsync(cn As MySqlConnection, banco As Integer, cuenta As String, numero As Integer, motivo As String, ct As CancellationToken) As Task
            Const consulta As String = "SELECT local AS Local, recibo AS Recibo FROM cxppagos WHERE tipopago = 1 AND banco = @banco AND cuenta = @cuenta AND dctpago = @documento LIMIT 1"
            Const pago As String = "UPDATE cxppagos SET anulado = '1', fanulado = CURDATE(), motivo = @motivo WHERE local = @Local AND recibo = @Recibo"
            Const detalle As String = "SELECT localcxp AS Local, cxp AS Cxp FROM cxppagosdetalle WHERE local = @Local AND recibo = @Recibo"
            Const factura As String = "UPDATE cxp SET cancelada = '0' WHERE local = @Local AND cxp = @Cxp"

            Dim documento = numero.ToString()
            Dim recibo = Await cn.QueryFirstOrDefaultAsync(Of ReciboCxp)(New CommandDefinition(consulta, New With {banco, cuenta, documento}, cancellationToken:=ct)).ConfigureAwait(False)
            If recibo Is Nothing Then Return
            Await cn.ExecuteAsync(New CommandDefinition(pago, New With {motivo, recibo.Local, recibo.Recibo}, cancellationToken:=ct)).ConfigureAwait(False)
            Dim facturas = (Await cn.QueryAsync(Of FacturaCxp)(New CommandDefinition(detalle, recibo, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            For Each f In facturas
                Await cn.ExecuteAsync(New CommandDefinition(factura, f, cancellationToken:=ct)).ConfigureAwait(False)
            Next
        End Function

        Private Shared Async Function AnularEnLibroAsync(cn As MySqlConnection, banco As Integer, cuenta As String, numero As Integer, local As Long, uid As Long, ct As CancellationToken) As Task
            Const consulta As String = "SELECT ids FROM ccblibro WHERE banco = @banco AND cuenta = @cuenta AND documento = @documento AND tipomov = 2 AND tipo = 0 LIMIT 1"
            Dim documento = numero.ToString()
            Dim ids = Await cn.ExecuteScalarAsync(Of Long?)(New CommandDefinition(consulta, New With {banco, cuenta, documento}, cancellationToken:=ct)).ConfigureAwait(False)
            If ids.HasValue Then Await LibroBancos.AnularAsync(cn, banco, cuenta, ids.Value, local, uid, ct).ConfigureAwait(False)
        End Function

        Private NotInheritable Class ReciboCxp
            Public Property Local As Long
            Public Property Recibo As Long
        End Class

        Private NotInheritable Class FacturaCxp
            Public Property Local As Long
            Public Property Cxp As Long
        End Class

    End Class

End Namespace
