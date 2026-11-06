Imports System.Data
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports SDACA.Core.Datos

Namespace Cheques

    Public NotInheritable Class CuentaEmision
        Public Property Cuenta As String = ""
        Public Property Descripcion As String = ""
        Public Property Contable As String = ""
        Public Property DetalleContable As String = ""
        Public Property Conta As String = ""
        Public Property Moneda As Integer
        Public Property LocalCuenta As String = ""

        Public ReadOnly Property LlevaConta As Boolean
            Get
                Return Conta = "1"
            End Get
        End Property

        Public ReadOnly Property Etiqueta As String
            Get
                Return $"{Cuenta} - {Descripcion}"
            End Get
        End Property
    End Class

    Public NotInheritable Class TipoAsiento
        Public Property Tipo As Integer
        Public Property Descripcion As String = ""
    End Class

    ''' <summary>Línea del asiento en captura. Sistema 100 es la cuenta del banco, 0 las del usuario.</summary>
    Public NotInheritable Class LineaCaptura
        Public Const SistemaBanco As Integer = 100

        Public Property Sistema As Integer
        Public Property Cuenta As String = ""
        Public Property Detalle As String = ""
        Public Property Debe As Double
        Public Property Haber As Double
    End Class

    Public NotInheritable Class TotalesAsiento
        Public Property Debe As Double
        Public Property Haber As Double
        Public Property Monto As Double
        ''' <summary>Debe menos haber de las líneas que no son del banco. Puede ser negativa.</summary>
        Public Property Diferencia As Double
    End Class

    Public NotInheritable Class AsientoCaptura

        Private Sub New()
        End Sub

        ''' <summary>
        ''' ECCMontoCheque, en colones y sin cuenta de conversión: la cuenta del banco recibe en el haber
        ''' la diferencia de las demás líneas, y esa diferencia es el monto del cheque.
        ''' </summary>
        Public Shared Function Recalcular(lineas As IList(Of LineaCaptura)) As TotalesAsiento
            Dim debe = 0.0
            Dim haber = 0.0
            Dim banco As LineaCaptura = Nothing
            For Each l In lineas
                If l.Sistema = LineaCaptura.SistemaBanco Then
                    banco = l
                Else
                    debe += l.Debe
                    haber += l.Haber
                End If
            Next
            Dim monto = debe - haber
            If banco IsNot Nothing Then
                banco.Debe = 0
                banco.Haber = monto
            End If
            Return New TotalesAsiento With {.Debe = debe, .Haber = haber + monto, .Diferencia = monto, .Monto = If(monto > 0, Math.Round(monto, 2, MidpointRounding.AwayFromZero), 0)}
        End Function

    End Class

    Public Enum ResultadoEmision
        Correcto
        ''' <summary>El número ya existe en el local: no se tocó nada.</summary>
        YaRegistrado
        ''' <summary>El número ya existe en otro local. El consecutivo sí avanzó, como en el original.</summary>
        Repetido
    End Enum

    Public NotInheritable Class ChequeEmision
        Public Property Banco As Integer
        Public Property Cuenta As String = ""
        Public Property Numero As Long
        Public Property Fecha As Date
        Public Property Id As String = ""
        Public Property Codigo As String = ""
        Public Property Nombre As String = ""
        Public Property Monto As Double
        Public Property Detalle As String = ""
        Public Property LlevaConta As Boolean
        Public Property TipoAsiento As Integer
        Public Property Lineas As IReadOnlyList(Of LineaCaptura) = Array.Empty(Of LineaCaptura)()
    End Class

    Public NotInheritable Class PersonaCheque
        Public Property Id As String = ""
        Public Property Nombre As String = ""
        Public Property Codigo As String = ""
    End Class

    Public NotInheritable Class CuentaCatalogo
        Public Property Descripcion As String = ""
        Public Property Auxiliar As String = ""
        Public Property EnOtroLocal As Boolean
        Public Property Codigo As String = ""
    End Class

    ''' <summary>
    ''' Captura y registro de cheques: SD1501\FrmPrintCheque.frm abierto desde el menú. INIT, que lo
    ''' llaman CxP, Café y Retiros con el cheque ya armado, no se migró. Tampoco las cuentas en otra
    ''' moneda ni los tipos de asiento con sqltext: hoy ninguna cuenta ni tipo los usa.
    ''' </summary>
    Public NotInheritable Class EmisionCheques

        Private Const CedulaCoopepalmares As String = "3-004-045003"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Async Function ListarCuentasAsync(local As Long, banco As Integer, uid As Long, todasLasCuentas As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of List(Of CuentaEmision))
            Dim permisoC = If(todasLasCuentas, "", " INNER JOIN ccbusuarioscuentas uc ON (uc.banco = c.banco AND uc.cuenta = c.cuenta AND uc.usuario = @uid) ")
            Dim permisoA = If(todasLasCuentas, "", " INNER JOIN ccbusuarioscuentas uc ON (uc.banco = a.banco AND uc.cuenta = a.cuenta AND uc.usuario = @uid) ")
            Dim sql = "SELECT x.Cuenta, x.Descripcion, x.Contable, x.DetalleContable, x.Conta, " &
                      "(SELECT m.moneda FROM ccbcuentas m WHERE m.banco = @banco AND m.cuenta = x.Cuenta LIMIT 1) AS Moneda, " &
                      "(SELECT m.local FROM ccbcuentas m WHERE m.banco = @banco AND m.cuenta = x.Cuenta LIMIT 1) AS LocalCuenta FROM (" &
                      "SELECT c.cuenta AS Cuenta, c.descripcion AS Descripcion, ca.ccontable AS Contable, ca.cdetalle AS DetalleContable, ca.conta AS Conta FROM ccbcuentas c " &
                      "INNER JOIN ccbcuentas ca ON (ca.local = @local AND ca.banco = @banco AND ca.cuenta = c.cuenta AND ca.cheques = '1') " & permisoC &
                      "UNION ALL " &
                      "SELECT a.cuenta, b.descripcion, a.cc, a.descripcion, IF(a.cc = '', '0', '1') FROM ccbcuentasconta a " &
                      "INNER JOIN ccbcuentas b ON (b.banco = a.banco AND b.cuenta = a.cuenta AND b.cheques = '1') " & permisoA &
                      "WHERE a.local = @local AND a.banco = @banco) x"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of CuentaEmision)(New CommandDefinition(sql, New With {local, banco, uid}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        Public Async Function ListarTiposAsientoAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of TipoAsiento))
            Const sql As String = "SELECT tipo AS Tipo, Descripcion FROM eccconta WHERE local = @local ORDER BY tipo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of TipoAsiento)(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>Las cuentas fijas del tipo y al final la del banco: ECC_AsientoCheque con monto 0.</summary>
        Public Async Function CargarAsientoAsync(local As Long, tipo As Integer, cuenta As CuentaEmision, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of LineaCaptura))
            Const calculado As String = "SELECT sqltext FROM eccconta WHERE tipo = @tipo LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim texto = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(calculado, New With {tipo}, cancellationToken:=ct)).ConfigureAwait(False)
                If Not String.IsNullOrEmpty(texto) AndAlso texto <> "x" Then
                    Throw New NotSupportedException("El tipo de asiento se calcula con una consulta. Use el sistema anterior.")
                End If
                Dim catalogo = Await CatalogoAsync(cn, cedula, ct).ConfigureAwait(False)
                Dim sql = "SELECT a.cuenta AS Cuenta, IFNULL(b.descripcion, a.descripcion) AS Detalle, a.dh AS Dh, a.clase AS Clase, a.valor AS Valor " &
                          $"FROM ecccontadetalle a LEFT JOIN {catalogo}sgccatalogo b ON b.codigo = a.cuenta WHERE a.local = @local AND a.tipo = @tipo ORDER BY a.pos"
                Dim lineas As New List(Of LineaCaptura)
                For Each d In Await cn.QueryAsync(Of DetalleTipo)(New CommandDefinition(sql, New With {local, .tipo = tipo.ToString(CultureInfo.InvariantCulture)}, cancellationToken:=ct)).ConfigureAwait(False)
                    ' Clase 3 es un porcentaje del monto total, que aquí es 0.
                    Dim importe = If(d.Clase = 3, 0, d.Valor)
                    lineas.Add(New LineaCaptura With {.Cuenta = d.Cuenta, .Detalle = d.Detalle, .Debe = If(d.Dh = 0, importe, 0), .Haber = If(d.Dh = 0, 0, importe)})
                Next
                lineas.Add(Await LineaBancoAsync(cn, catalogo, cuenta, ct).ConfigureAwait(False))
                Return lineas
            End Using
        End Function

        Public Async Function LineaBancoAsync(cuenta As CuentaEmision, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of LineaCaptura)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await LineaBancoAsync(cn, Await CatalogoAsync(cn, cedula, ct).ConfigureAwait(False), cuenta, ct).ConfigureAwait(False)
            End Using
        End Function

        Private Shared Async Function LineaBancoAsync(cn As MySqlConnection, catalogo As String, cuenta As CuentaEmision, ct As CancellationToken) As Task(Of LineaCaptura)
            Dim sql = $"SELECT descripcion FROM {catalogo}sgccatalogo WHERE codigo = @codigo LIMIT 1"
            Dim detalle = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {.codigo = cuenta.Contable}, cancellationToken:=ct)).ConfigureAwait(False)
            Return New LineaCaptura With {.Sistema = LineaCaptura.SistemaBanco, .Cuenta = cuenta.Contable, .Detalle = If(detalle, cuenta.DetalleContable)}
        End Function

        ''' <summary>
        ''' Prefijo de la base del catálogo contable (columna DB de sgcempresas). En el servidor de
        ''' pruebas esa base no existe y sgccatalogo está en el catálogo de trabajo.
        ''' </summary>
        Friend Shared Async Function CatalogoAsync(cn As MySqlConnection, cedula As String, ct As CancellationToken) As Task(Of String)
            Const sql As String = "SELECT s.schema_name FROM sgcempresas e INNER JOIN information_schema.schemata s ON s.schema_name = e.DB WHERE e.Cedula = @cedula LIMIT 1"
            Dim db = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {cedula}, cancellationToken:=ct)).ConfigureAwait(False)
            If String.IsNullOrEmpty(db) OrElse Not Regex.IsMatch(db, "^[A-Za-z0-9_]+$") Then Return ""
            Return db & "."
        End Function

        ''' <summary>Locales cuyas cuentas contables se pueden usar. En Coopepalmares, 1, 2 y 3 son uno solo.</summary>
        Public Shared Function LocalesContables(localCuenta As String, cedulaConGuiones As String) As String()
            If cedulaConGuiones = CedulaCoopepalmares AndAlso (localCuenta = "1" OrElse localCuenta = "2" OrElse localCuenta = "3") Then
                Return {"", "1", "2", "3"}
            End If
            Return {"", localCuenta}
        End Function

        Public Async Function BuscarCatalogoAsync(locales As String(), formato As FormatoCuenta, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim catalogo = Await CatalogoAsync(cn, cedula, ct).ConfigureAwait(False)
                Dim sql = $"SELECT Descripcion AS 'Descripción', {formato.ExpresionSql("codigo")} AS Cuenta, Codigo FROM {catalogo}sgccatalogo " &
                          "WHERE Auxiliar = '1' AND local IN @locales ORDER BY descripcion"
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {locales}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <param name="cuenta">Como la ve el usuario, sin los niveles ocultos.</param>
        Public Async Function ObtenerCuentaCatalogoAsync(cuenta As String, locales As String(), formato As FormatoCuenta, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of CuentaCatalogo)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim catalogo = Await CatalogoAsync(cn, cedula, ct).ConfigureAwait(False)
                Dim sql = "SELECT Descripcion, auxiliar AS Auxiliar, local NOT IN @locales AS EnOtroLocal, codigo AS Codigo " &
                          $"FROM {catalogo}sgccatalogo WHERE {formato.ExpresionSql("codigo")} = @cuenta LIMIT 1"
                Return Await cn.QueryFirstOrDefaultAsync(Of CuentaCatalogo)(New CommandDefinition(sql, New With {cuenta, locales}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function BuscarPersonasAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT nombre AS 'Nombre Completo', id AS 'Identificación', Codigo FROM gacdp2 WHERE activo = '1' AND fdef = '1900-01-01' ORDER BY nombre"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        Public Async Function ObtenerPersonaPorIdAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of PersonaCheque)
            Const persona As String = "SELECT id AS Id, TRIM(CAST(CONCAT(apellido1, ' ', apellido2, ' ', nombre) AS CHAR)) AS Nombre FROM gacdp WHERE id = @id LIMIT 1"
            Const codigo As String = "SELECT codigo FROM gaccodclientes b LEFT JOIN gactiposcodigos c ON c.TIPO = LEFT(b.CODIGO, 3) WHERE id = @id ORDER BY `order`, codigo LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim hallada = Await cn.QueryFirstOrDefaultAsync(Of PersonaCheque)(New CommandDefinition(persona, New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
                If hallada Is Nothing Then Return Nothing
                hallada.Codigo = If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(codigo, New With {id}, cancellationToken:=ct)).ConfigureAwait(False), "")
                Return hallada
            End Using
        End Function

        Public Async Function ObtenerPersonaPorCodigoAsync(codigo As String, Optional ct As CancellationToken = Nothing) As Task(Of PersonaCheque)
            Const sql As String = "SELECT id FROM gaccodclientes WHERE codigo = @codigo LIMIT 1"
            Dim id As String
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                id = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
            If String.IsNullOrEmpty(id) Then Return Nothing
            Dim persona = Await ObtenerPersonaPorIdAsync(id, ct).ConfigureAwait(False)
            If persona IsNot Nothing Then persona.Codigo = codigo
            Return persona
        End Function

        Public Async Function SiguienteNumeroAsync(local As Long, estacion As String, banco As Integer, cuenta As String, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await ConsecutivoCheques.SiguienteAsync(cn, local, estacion, banco, cuenta, False, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>ECCVerifCheque: si el número ya se usó en el local.</summary>
        Public Async Function ExisteAsync(local As Long, banco As Integer, cuenta As String, numero As Long, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Const sql As String = "SELECT COUNT(*) FROM ecccheques WHERE local = @local AND banco = @banco AND cuenta = @cuenta AND cheque = @numero"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(sql, New With {local, banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False) > 0
            End Using
        End Function

        ''' <summary>
        ''' La parte de CmdPrinter1_Click que graba. Igual que el original, el consecutivo avanza una vez
        ''' aunque el número se haya escrito a mano. Las tablas son MyISAM, sin transacciones.
        ''' </summary>
        Public Async Function RegistrarAsync(c As ChequeEmision, local As Long, estacion As String, uid As Long, Optional ct As CancellationToken = Nothing) As Task(Of ResultadoEmision)
            Const enLocal As String = "SELECT COUNT(*) FROM ecccheques WHERE local = @local AND banco = @Banco AND cuenta = @Cuenta AND cheque = @Numero"
            Const enCuenta As String = "SELECT COUNT(*) FROM ecccheques WHERE banco = @Banco AND cuenta = @Cuenta AND cheque = @Numero"
            Const cheque As String = "INSERT INTO ecccheques (local, banco, cuenta, cheque, fecha, id, codigo, nombre, monto, otro, anulado, observaciones, uidc) " &
                                     "VALUES (@local, @Banco, @Cuenta, @Numero, @Fecha, @Id, @Codigo, @Nombre, @Monto, @otro, '0', @Detalle, @uid)"
            Const linea As String = "INSERT INTO eccchequesconta (local, banco, cuenta, cheque, pos, ccontable, detalle, md, mh, System, contabilizado, tipoac, uidc, fc) " &
                                    "VALUES (@local, @Banco, @Cuenta, @Numero, @pos, @contable, @detalle, @debe, @haber, @sistema, '0', @TipoAsiento, @uid, CURDATE())"
            Const libro As String = "INSERT INTO ccblibro VALUES (NULL, @local, @Banco, @Cuenta, @Fecha, @documento, 2, '0', @Monto, @detalle, '0', '1900-01-01', '', @uid, CURDATE())"

            Dim p As New DynamicParameters(c)
            p.Add("local", local)
            p.Add("uid", uid)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                If Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(enLocal, p, cancellationToken:=ct)).ConfigureAwait(False) > 0 Then Return ResultadoEmision.YaRegistrado
                Await ConsecutivoCheques.SiguienteAsync(cn, local, estacion, c.Banco, c.Cuenta, True, ct).ConfigureAwait(False)
                If Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(enCuenta, p, cancellationToken:=ct)).ConfigureAwait(False) > 0 Then Return ResultadoEmision.Repetido

                Dim texto = c.Numero.ToString(CultureInfo.InvariantCulture)
                p.Add("otro", $"{c.Cuenta.Trim()} *-* {texto} *-* {c.Monto.ToString("0.00", CultureInfo.InvariantCulture)}")
                Await cn.ExecuteAsync(New CommandDefinition(cheque, p, cancellationToken:=ct)).ConfigureAwait(False)

                If c.LlevaConta Then
                    For i = 0 To c.Lineas.Count - 1
                        Dim l = c.Lineas(i)
                        Dim fila As New DynamicParameters(p)
                        fila.AddDynamicParams(New With {.pos = i + 1, .contable = l.Cuenta, .detalle = l.Detalle, .debe = l.Debe, .haber = l.Haber, .sistema = l.Sistema})
                        Await cn.ExecuteAsync(New CommandDefinition(linea, fila, cancellationToken:=ct)).ConfigureAwait(False)
                    Next
                End If

                Dim movimiento As New DynamicParameters(p)
                movimiento.AddDynamicParams(New With {.documento = texto, .detalle = c.Nombre & If(c.Detalle.Length > 0, $" ({c.Detalle.Trim()})", "")})
                Await cn.ExecuteAsync(New CommandDefinition(libro, movimiento, cancellationToken:=ct)).ConfigureAwait(False)
                Return ResultadoEmision.Correcto
            End Using
        End Function

        ''' <summary>
        ''' SaveDContable de SD1501\FrmEditarCheque.frm: cambia el detalle y reemplaza el asiento. El monto
        ''' del cheque no cambia. Las líneas vuelven a quedar sin contabilizar, igual que en el original.
        ''' Sin transacciones: si falla tras borrar, el asiento queda incompleto y hay que guardar de nuevo.
        ''' </summary>
        ''' <param name="local">El local del cheque, no el del usuario.</param>
        Public Async Function GuardarAsientoAsync(banco As Integer, cuenta As String, numero As Long, local As Long, detalle As String, tipo As Integer, lineas As IReadOnlyList(Of LineaCaptura), uid As Long, Optional ct As CancellationToken = Nothing) As Task
            Const encabezado As String = "UPDATE ecccheques SET observaciones = @detalle WHERE banco = @banco AND cuenta = @cuenta AND cheque = @numero"
            Const borrado As String = "DELETE FROM eccchequesconta WHERE cheque = @numero AND cuenta = @cuenta AND banco = @banco"
            Const linea As String = "INSERT INTO eccchequesconta (local, banco, cuenta, cheque, pos, ccontable, detalle, md, mh, System, contabilizado, tipoac, uidc, fc) " &
                                    "VALUES (@local, @banco, @cuenta, @numero, @pos, @contable, @texto, @debe, @haber, @sistema, '0', @tipo, @uid, CURDATE())"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(encabezado, New With {detalle, banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(borrado, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                For i = 0 To lineas.Count - 1
                    Dim l = lineas(i)
                    Await cn.ExecuteAsync(New CommandDefinition(linea, New With {local, banco, cuenta, numero, .pos = i + 1, .contable = l.Cuenta, .texto = l.Detalle, .debe = l.Debe, .haber = l.Haber, .sistema = l.Sistema, tipo, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Next
            End Using
        End Function

        Private NotInheritable Class DetalleTipo
            Public Property Cuenta As String = ""
            Public Property Detalle As String = ""
            Public Property Dh As Integer
            Public Property Clase As Integer
            Public Property Valor As Double
        End Class

    End Class

End Namespace
