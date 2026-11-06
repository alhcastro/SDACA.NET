Imports System.Data
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports SDACA.Core.Datos

Namespace Bancos

    ''' <summary>Elemento de un combo con su código.</summary>
    Public NotInheritable Class OpcionTransferencia
        Public Property Codigo As Long
        Public Property Descripcion As String = ""

        Public Overrides Function ToString() As String
            Return Descripcion
        End Function
    End Class

    ''' <summary>Fila de <c>ccbtransferenciasenc</c>.</summary>
    Public NotInheritable Class Transferencia
        Public Shared ReadOnly SinAplicar As Date = #1900-01-01#

        Public Property Local As Long
        Public Property Modulo As Long
        Public Property NumTra As Long
        Public Property Concepto As String = ""
        Public Property NumCli As String = ""
        Public Property Cuenta As String = ""
        Public Property Fecha As Date
        Public Property NumTraInt As String = ""
        Public Property NumTraBan As String = "0"
        Public Property TipTra As String = "1"
        Public Property TcCompra As Double
        Public Property TcVenta As Double
        Public Property FAplicada As Date = SinAplicar
        Public Property Anulado As Boolean
        Public Property Creacion As String = ""
        Public Property Lineas As New List(Of LineaTransferencia)
    End Class

    ''' <summary>Fila de <c>ccbtransferenciasdet</c>.</summary>
    Public NotInheritable Class LineaTransferencia
        Public Property Tipo As String = "D"
        Public Property Id As String = ""
        Public Property NumCuenta As String = ""
        Public Property Concepto As String = ""
        Public Property NumCom As String = ""
        Public Property Monto As Double
        Public Property Anulado As Boolean
        Public Property Fa As Date = Transferencia.SinAplicar
    End Class

    ''' <summary>Resumen que se muestra antes de generar el archivo.</summary>
    Public NotInheritable Class ResumenTransferencia
        Public Property Concepto As String = ""
        Public Property Cuenta As String = ""
        Public Property Fecha As Date
        Public Property Lineas As Long
        Public Property Total As Double
    End Class

    ''' <summary>Movimiento que quedó en el libro al generar antes la misma transferencia.</summary>
    Public NotInheritable Class MovimientoGenerado
        Public Property Fecha As Date
        Public Property Monto As Double
        Public Property Conciliado As Boolean
    End Class

    ''' <summary>Lo necesario para escribir el archivo y registrarlo en el libro.</summary>
    Public NotInheritable Class ContenidoTransferencia
        Public Property Banco As Long
        Public Property Cuenta As String = ""
        Public Property Monto As Double
        Public Property Modulo As String = ""
        Public Property Lineas As New List(Of String)
    End Class

    ''' <summary>
    ''' Transferencias que generan Café y Nómina. Migración de SD1401\FrmMantTransferencias.frm y
    ''' FrmTransferenciasBancos.frm. Como en VB6, faplicada nunca se llena y estas sentencias no se replican.
    ''' </summary>
    Public NotInheritable Class TransferenciasBancarias

        Private Const Listado As String =
            "SELECT T.local AS 'LOCAL', T.modulo AS 'MODULO', " &
            "UCASE(CONCAT(CAST(T.local AS CHAR),' - ',IFNULL(L.Descripcion,''),', ',CAST(T.modulo AS CHAR),' - ',IFNULL(M.Descripcion,''))) AS 'ORIGEN', " &
            "T.numtra AS 'NUMERO', T.concepto AS 'CONCEPTO', T.cuenta AS 'CUENTA_ORIGEN', T.fecha AS 'FECHA', T.numtraint AS 'INTERNA', T.numtraban AS 'BANCO', " &
            "(SELECT COUNT(local) FROM ccbtransferenciasdet WHERE local = T.local AND modulo = T.modulo AND numtra = T.numtra) AS 'LINEAS' " &
            "FROM ccbtransferenciasenc T LEFT JOIN gatlocales L ON (L.Local = T.local) LEFT JOIN gatmodulos M ON (M.Modulo = T.modulo) " &
            "WHERE T.anulado = '0' AND T.faplicada = '1900-01-01 00:00:00' "

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Async Function OpcionesAsync(sql As String, parametros As Object, ct As CancellationToken) As Task(Of List(Of OpcionTransferencia))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of OpcionTransferencia)(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        Private Async Function TablaAsync(sql As String, parametros As Object, ct As CancellationToken) As Task(Of DataTable)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        Public Function ListarLocalesAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionTransferencia))
            Return OpcionesAsync("SELECT Local AS Codigo, CONCAT(Local,' - ',Descripcion) AS Descripcion FROM gatlocales ORDER BY Local", Nothing, ct)
        End Function

        Public Function ListarModulosAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionTransferencia))
            Return OpcionesAsync("SELECT Modulo AS Codigo, CONCAT(Modulo,' - ',Descripcion) AS Descripcion FROM gatmodulos ORDER BY Modulo", Nothing, ct)
        End Function

        Public Function ListarLocalesPendientesAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionTransferencia))
            Const sql As String = "SELECT T.local AS Codigo, UCASE(CONCAT(CAST(T.local AS CHAR),' => ',IFNULL(L.Descripcion,''),' (',COUNT(T.local),' TRANSFERENCIAS)')) AS Descripcion " &
                                  "FROM ccbtransferenciasenc T LEFT JOIN gatlocales L ON (L.Local = T.local) " &
                                  "WHERE T.anulado = '0' AND T.faplicada = '1900-01-01 00:00:00' GROUP BY T.local ORDER BY T.local"
            Return OpcionesAsync(sql, Nothing, ct)
        End Function

        Public Function ListarModulosPendientesAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionTransferencia))
            Const sql As String = "SELECT T.modulo AS Codigo, UCASE(CONCAT(CAST(T.modulo AS CHAR),' => ',IFNULL(M.Descripcion,''),' (',COUNT(T.local),' TRANSFERENCIAS)')) AS Descripcion " &
                                  "FROM ccbtransferenciasenc T LEFT JOIN gatmodulos M ON (M.Modulo = T.modulo) " &
                                  "WHERE T.local = @local AND T.anulado = '0' AND T.faplicada = '1900-01-01 00:00:00' GROUP BY T.local, T.modulo ORDER BY T.modulo"
            Return OpcionesAsync(sql, New With {local}, ct)
        End Function

        ''' <summary>Las sin aplicar, de la más reciente a la más vieja. Como faplicada nunca se llena, son casi todas.</summary>
        Public Function BuscarAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Return TablaAsync(Listado & "AND T.numtraban = '0' ORDER BY T.fecha DESC", Nothing, ct)
        End Function

        Public Function BuscarPorModuloAsync(local As Long, modulo As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Return TablaAsync(Listado & "AND T.local = @local AND T.modulo = @modulo ORDER BY T.fecha DESC", New With {local, modulo}, ct)
        End Function

        Public Function BuscarPersonasAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT CONCAT(Nombre,' ',Apellido1,' ',Apellido2) AS NOMBRE, id AS IDENTIFICACION FROM gacdp WHERE activo = '1' AND fdef = '1900-01-01'"
            Return TablaAsync(sql, Nothing, ct)
        End Function

        Public Async Function SiguienteAsync(local As Long, modulo As Long, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Const sql As String = "SELECT IFNULL(MAX(numtra)+1,1) FROM ccbtransferenciasenc WHERE local = @local AND modulo = @modulo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(sql, New With {local, modulo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function ObtenerAsync(local As Long, modulo As Long, numtra As Long, Optional ct As CancellationToken = Nothing) As Task(Of Transferencia)
            Const encabezado As String = "SELECT E.local AS Local, E.modulo AS Modulo, E.numtra AS NumTra, E.concepto AS Concepto, E.numcli AS NumCli, E.cuenta AS Cuenta, E.fecha AS Fecha, " &
                                         "E.numtraint AS NumTraInt, E.numtraban AS NumTraBan, E.tiptra AS TipTra, E.tccom AS TcCompra, E.tcven AS TcVenta, E.faplicada AS FAplicada, " &
                                         "E.anulado = '1' AS Anulado, CONCAT('Creado el: ',CAST(E.FC AS CHAR),' por ',CAST(IFNULL(CONCAT(E.UIDC,' - ',U.Usuario),E.UIDC) AS CHAR)) AS Creacion " &
                                         "FROM ccbtransferenciasenc E LEFT JOIN gatusuarios U ON (U.UID = E.UIDC) WHERE E.local = @local AND E.modulo = @modulo AND E.numtra = @numtra"
            Const detalle As String = "SELECT tipo AS Tipo, ID AS Id, numcuenta AS NumCuenta, concepto AS Concepto, numcom AS NumCom, monto AS Monto, anu = '1' AS Anulado, fa AS Fa " &
                                      "FROM ccbtransferenciasdet WHERE local = @local AND modulo = @modulo AND numtra = @numtra"
            Dim parametros = New With {local, modulo, numtra}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim transferencia = Await cn.QueryFirstOrDefaultAsync(Of Transferencia)(New CommandDefinition(encabezado, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                If transferencia Is Nothing Then Return Nothing
                transferencia.Lineas = (Await cn.QueryAsync(Of LineaTransferencia)(New CommandDefinition(detalle, parametros, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                Return transferencia
            End Using
        End Function

        ''' <summary>
        ''' SD_Guardar. Las líneas se reemplazan todas: en VB6, cambiar el ID o el comprobante de una línea
        ''' deja la vieja en la tabla y se paga dos veces. Una línea ya anulada conserva su fecha.
        ''' </summary>
        Public Async Function GuardarAsync(t As Transferencia, uid As Long, ahora As Date, Optional ct As CancellationToken = Nothing) As Task
            Const encabezado As String = "REPLACE INTO ccbtransferenciasenc (local,modulo,numtra,concepto,numcli,cuenta,fecha,numtraint,numtraban,tiptra,tccom,tcven,faplicada,anulado,UIDC,FC) " &
                                         "VALUES (@Local,@Modulo,@NumTra,@Concepto,@NumCli,@Cuenta,@Fecha,@NumTraInt,@NumTraBan,@TipTra,@TcCompra,@TcVenta,@FAplicada,@anulado,@uid,@ahora)"
            Const borrar As String = "DELETE FROM ccbtransferenciasdet WHERE local = @Local AND modulo = @Modulo AND numtra = @NumTra"
            Const linea As String = "REPLACE INTO ccbtransferenciasdet (local,modulo,numtra,tipo,ID,numcuenta,concepto,numcom,monto,anu,fa) " &
                                    "VALUES (@Local,@Modulo,@NumTra,@Tipo,@Id,@NumCuenta,@Concepto,@NumCom,@Monto,@anu,@fa)"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim datos = New With {t.Local, t.Modulo, t.NumTra, t.Concepto, t.NumCli, t.Cuenta, t.Fecha, t.NumTraInt, t.NumTraBan, t.TipTra, t.TcCompra, t.TcVenta, t.FAplicada,
                                      .anulado = If(t.Anulado, "1", "0"), uid, ahora}
                Await cn.ExecuteAsync(New CommandDefinition(encabezado, datos, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(borrar, New With {t.Local, t.Modulo, t.NumTra}, cancellationToken:=ct)).ConfigureAwait(False)
                For Each l In t.Lineas
                    Dim fa = If(Not l.Anulado, Transferencia.SinAplicar, If(l.Fa > Transferencia.SinAplicar, l.Fa, ahora))
                    Dim fila = New With {t.Local, t.Modulo, t.NumTra, l.Tipo, l.Id, l.NumCuenta, l.Concepto, l.NumCom, l.Monto, .anu = If(l.Anulado, "1", "0"), fa}
                    Await cn.ExecuteAsync(New CommandDefinition(linea, fila, cancellationToken:=ct)).ConfigureAwait(False)
                Next
            End Using
        End Function

        Public Async Function BorrarAsync(local As Long, modulo As Long, numtra As Long, Optional ct As CancellationToken = Nothing) As Task
            Const encabezado As String = "DELETE FROM ccbtransferenciasenc WHERE local = @local AND modulo = @modulo AND numtra = @numtra"
            Const detalle As String = "DELETE FROM ccbtransferenciasdet WHERE local = @local AND modulo = @modulo AND numtra = @numtra"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(encabezado, New With {local, modulo, numtra}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(detalle, New With {local, modulo, numtra}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function AnularAsync(local As Long, modulo As Long, numtra As Long, ahora As Date, Optional ct As CancellationToken = Nothing) As Task
            Const encabezado As String = "UPDATE ccbtransferenciasenc SET anulado = '1' WHERE local = @local AND modulo = @modulo AND numtra = @numtra"
            Const detalle As String = "UPDATE ccbtransferenciasdet SET anu = '1', fa = @ahora WHERE local = @local AND modulo = @modulo AND numtra = @numtra"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(encabezado, New With {local, modulo, numtra}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(detalle, New With {local, modulo, numtra, ahora}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function ResumenAsync(local As Long, modulo As Long, numtra As Long, Optional ct As CancellationToken = Nothing) As Task(Of ResumenTransferencia)
            Const sql As String = "SELECT E.concepto AS Concepto, E.cuenta AS Cuenta, E.fecha AS Fecha, COUNT(D.monto) AS Lineas, IFNULL(SUM(D.monto),0) AS Total FROM ccbtransferenciasenc E " &
                                  "LEFT JOIN ccbtransferenciasdet D ON (D.local = E.local AND D.modulo = E.modulo AND D.numtra = E.numtra AND D.anu != '1') " &
                                  "WHERE E.local = @local AND E.modulo = @modulo AND E.numtra = @numtra GROUP BY E.local, E.modulo, E.numtra"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of ResumenTransferencia)(New CommandDefinition(sql, New With {local, modulo, numtra}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Consultas de SD_Gen_Archivo_ENV. Solo lee: el archivo se escribe aparte. Nothing si el banco no tiene formato.</summary>
        Public Async Function ArmarAsync(local As Long, modulo As Long, numtra As Long, Optional ct As CancellationToken = Nothing) As Task(Of ContenidoTransferencia)
            Const cuentaBanco As String = "SELECT C.banco AS Banco, C.cuenta AS Cuenta FROM ccbtransferenciasenc E INNER JOIN ccbcuentas C ON (C.cuentab = E.cuenta) WHERE E.modulo = @modulo AND E.numtra = @numtra"
            Const formatos As String = "SELECT seccion, formato FROM ccbtransferenciasconfig WHERE banco = @banco AND tipo = 'TRANSFERENCIA'"
            Const encabezado As String = "SELECT 1 AS 'ENCTR1', E.numcli AS 'ENCCLI', E.fecha AS 'ENCFEC', E.numtraint AS 'ENCINT', E.numtraban AS 'ENCREA', E.tiptra AS 'ENCTIP', 0 AS 'ENCERR', " &
                                         "(SELECT SUM(MONTO) FROM ccbtransferenciasdet WHERE local = E.local AND modulo = E.modulo AND numtra = E.numtra AND anu != '1') AS 'ENCTOT', " &
                                         "E.tccom AS 'ENCTCC', E.tcven AS 'ENCTCV', '' AS 'ENCFIL' FROM ccbtransferenciasenc E WHERE E.local = @local AND E.modulo = @modulo AND E.numtra = @numtra"
            Const cuerpo As String = "SELECT 2 AS 'CUETR2', CONCAT(MID(REPLACE(E.cuenta,'-',''),6,3), MID(REPLACE(E.cuenta,'-',''),1,3), MID(REPLACE(E.cuenta,'-',''),4,2), MID(REPLACE(E.cuenta,'-',''),9)) AS 'CUECUE', " &
                                     "E.numtraint AS 'CUENUM', SUM(D.monto) 'CUETOT', E.concepto AS 'CUECON', 0 AS 'CUEEST' FROM ccbtransferenciasenc E " &
                                     "LEFT JOIN ccbtransferenciasdet D ON (D.local = E.local AND D.modulo = E.modulo AND D.numtra = E.numtra AND D.anu != '1') " &
                                     "WHERE E.local = @local AND E.modulo = @modulo AND E.numtra = @numtra AND E.tiptra = '1' GROUP BY E.local, E.modulo, E.numtra " &
                                     "UNION ALL " &
                                     "SELECT 3 AS 'CUETR2', CONCAT(MID(REPLACE(numcuenta,'-',''),6,3), MID(REPLACE(numcuenta,'-',''),1,3), MID(REPLACE(numcuenta,'-',''),4,2), MID(REPLACE(numcuenta,'-',''),9)) AS 'CUECUE', " &
                                     "D.numcom AS 'CUENUM', D.monto AS 'CUETOT', D.concepto AS 'CUECON', 0 AS 'CUEEST' FROM ccbtransferenciasenc E " &
                                     "LEFT JOIN ccbtransferenciasdet D ON (D.local = E.local AND D.modulo = E.modulo AND D.numtra = E.numtra AND D.anu != '1') " &
                                     "WHERE E.local = @local AND E.modulo = @modulo AND E.numtra = @numtra AND E.tiptra = '1'"
            Const pie As String = "SELECT 4 AS 'PIETR3', SUM(D.monto) * 2 'PIESUM', (SELECT IFNULL(SUM(CTA),0) FROM " &
                                  "(SELECT CAST(SUBSTRING(REPLACE(numcuenta,'-',''),9,6) AS SIGNED) AS 'CTA' FROM ccbtransferenciasdet WHERE local = @local AND modulo = @modulo AND numtra = @numtra AND anu != '1' " &
                                  "UNION ALL SELECT CAST(SUBSTRING(REPLACE(cuenta,'-',''),9,6) AS SIGNED) AS 'CTA' FROM ccbtransferenciasenc WHERE local = @local AND modulo = @modulo AND numtra = @numtra) AS A) AS 'PIECUE', " &
                                  "'' AS 'PIEKEY', 0 AS 'PIEUSD', SUM(D.monto) 'PIENAC', '' AS 'PIEFIL' FROM ccbtransferenciasdet D " &
                                  "WHERE D.local = @local AND D.modulo = @modulo AND D.numtra = @numtra AND D.anu != '1' GROUP BY D.local, D.modulo, D.numtra"
            Dim parametros = New With {local, modulo, numtra}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim destino = Await cn.QueryFirstOrDefaultAsync(Of ContenidoTransferencia)(New CommandDefinition(cuentaBanco, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                If destino Is Nothing Then Return Nothing
                Dim secciones = (Await cn.QueryAsync(Of (Seccion As Integer, Formato As String))(New CommandDefinition(formatos, New With {destino.Banco}, cancellationToken:=ct)).ConfigureAwait(False)).
                    ToDictionary(Function(s) s.Seccion, Function(s) s.Formato)
                If secciones.Count < 3 Then Return Nothing

                destino.Lineas.AddRange(Await LineasAsync(cn, encabezado, parametros, secciones(0), ct).ConfigureAwait(False))
                destino.Lineas.AddRange(Await LineasAsync(cn, cuerpo, parametros, secciones(1), ct).ConfigureAwait(False))
                Dim piePagina = Await FilasAsync(cn, pie, parametros, ct).ConfigureAwait(False)
                If piePagina.Count > 0 Then destino.Monto = Conversion.Val(piePagina(0)("PIESUM")) / 2
                destino.Lineas.AddRange(piePagina.Select(Function(f) ArchivoTransferencia.Linea(secciones(2), f)))
                destino.Modulo = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT descripcion FROM gatmodulos WHERE modulo = @modulo", New With {modulo}, cancellationToken:=ct)).ConfigureAwait(False)
                Return destino
            End Using
        End Function

        Private Shared Async Function LineasAsync(cn As MySqlConnection, sql As String, parametros As Object, formato As String, ct As CancellationToken) As Task(Of IEnumerable(Of String))
            Dim filas = Await FilasAsync(cn, sql, parametros, ct).ConfigureAwait(False)
            Return filas.Select(Function(f) ArchivoTransferencia.Linea(formato, f))
        End Function

        ''' <summary>Cada fila como pares de nombre y texto, igual que SD_RS2Str.</summary>
        Private Shared Async Function FilasAsync(cn As MySqlConnection, sql As String, parametros As Object, ct As CancellationToken) As Task(Of List(Of IReadOnlyDictionary(Of String, String)))
            Dim filas As New List(Of IReadOnlyDictionary(Of String, String))
            Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                While lector.Read()
                    Dim fila As New Dictionary(Of String, String)
                    For i = 0 To lector.FieldCount - 1
                        fila(lector.GetName(i)) = ArchivoTransferencia.Texto(lector.GetValue(i))
                    Next
                    filas.Add(fila)
                End While
            End Using
            Return filas
        End Function

        Public Shared Function Documento(modulo As Long, numtra As Long) As String
            Return $"{modulo}-{numtra}"
        End Function

        ''' <summary>Movimiento de una generación anterior. Regenerar lo borra, aunque esté conciliado.</summary>
        Public Async Function MovimientoExistenteAsync(local As Long, contenido As ContenidoTransferencia, modulo As Long, numtra As Long, Optional ct As CancellationToken = Nothing) As Task(Of MovimientoGenerado)
            Const sql As String = "SELECT fecha AS Fecha, monto AS Monto, conciliado = '1' AS Conciliado FROM ccblibro WHERE local = @local AND banco = @Banco AND cuenta = @Cuenta AND documento = @documento LIMIT 1"
            Dim parametros = New With {local, contenido.Banco, contenido.Cuenta, .documento = Documento(modulo, numtra)}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of MovimientoGenerado)(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' SD_AgregarMovLibros: cambia el movimiento de la transferencia por uno de hoy. Como en VB6 no se
        ''' replica. A diferencia de VB6, rehace los cierres, para que Saldo Actual incluya el pago.
        ''' </summary>
        Public Async Function RegistrarEnLibroAsync(local As Long, modulo As Long, numtra As Long, contenido As ContenidoTransferencia, localSesion As Long, uid As Long, hoy As Date, Optional ct As CancellationToken = Nothing) As Task
            Const fechaAnterior As String = "SELECT MIN(fecha) FROM ccblibro WHERE local = @local AND banco = @Banco AND cuenta = @Cuenta AND documento = @documento"
            Const borrar As String = "DELETE FROM ccblibro WHERE local = @local AND banco = @Banco AND cuenta = @Cuenta AND documento = @documento"
            Const insertar As String = "INSERT INTO ccblibro VALUES (@ids, @local, @Banco, @Cuenta, @hoy, @documento, 0, 0, @Monto, @detalle, '0', '1900-01-01', '', @uid, NOW())"
            Dim documento = TransferenciasBancarias.Documento(modulo, numtra)
            Dim llave = New With {local, contenido.Banco, contenido.Cuenta, documento}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim anterior = Await cn.ExecuteScalarAsync(Of Date?)(New CommandDefinition(fechaAnterior, llave, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(borrar, llave, cancellationToken:=ct)).ConfigureAwait(False)
                Dim ids = Await Consecutivo.SiguienteAsync(cn, "ccblibro", "ids", localSesion, ct:=ct).ConfigureAwait(False)
                Dim detalle = $"Transferencia automatica modulo de {contenido.Modulo} #{numtra}."
                Dim fila = New With {ids, local, contenido.Banco, contenido.Cuenta, hoy, documento, contenido.Monto, detalle, uid}
                Await cn.ExecuteAsync(New CommandDefinition(insertar, fila, cancellationToken:=ct)).ConfigureAwait(False)
                Dim desde = If(anterior.HasValue AndAlso anterior.Value < hoy, anterior.Value, hoy)
                Await CierresDiarios.RecalcularAsync(cn, localSesion, contenido.Banco, contenido.Cuenta, desde, hoy, ct).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
