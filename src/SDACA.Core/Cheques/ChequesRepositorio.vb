Imports System.Data
Imports System.Threading
Imports Dapper
Imports SDACA.Core.Datos

Namespace Cheques

    Public NotInheritable Class BancoCheque
        Public Property Codigo As Integer
        Public Property Descripcion As String = ""

        Public ReadOnly Property Etiqueta As String
            Get
                Return $"{Codigo} - {Descripcion}"
            End Get
        End Property
    End Class

    Public NotInheritable Class CuentaCheque
        Public Property Cuenta As String = ""
        Public Property Descripcion As String = ""

        Public ReadOnly Property Etiqueta As String
            Get
                Return $"{Cuenta} - {Descripcion}"
            End Get
        End Property
    End Class

    Public NotInheritable Class LineaAsiento
        Public Property Cuenta As String = ""
        Public Property Detalle As String = ""
        Public Property Debe As Double
        Public Property Haber As Double
        Public Property Sistema As Integer
        Public Property TipoAc As Integer
    End Class

    Public NotInheritable Class ChequeConsulta
        Public Property Numero As Integer
        Public Property Local As Long
        ''' <summary>Date.MinValue en los cheques viejos que tienen fecha 0000-00-00.</summary>
        Public Property Fecha As Date
        Public Property Nombre As String = ""
        Public Property Monto As Double
        ''' <summary>Columna <c>otro</c>: el código de seguridad.</summary>
        Public Property CodigoSeguridad As String = ""
        Public Property Observaciones As String = ""
        Public Property Anulado As String = ""
        Public Property Usuario As String = ""
        Public Property Id As String = ""
        Public Property Codigo As String = ""
        Public Property Conciliado As Boolean
        Public Property Lineas As IReadOnlyList(Of LineaAsiento) = Array.Empty(Of LineaAsiento)()

        Public ReadOnly Property EstaAnulado As Boolean
            Get
                Return Anulado = "1"
            End Get
        End Property
    End Class

    ''' <summary>Cheque emitido que todavía no se le entregó a nadie.</summary>
    Public NotInheritable Class ChequeEntrega
        Public Property Banco As Integer
        Public Property Cuenta As String = ""
        Public Property Cheque As Integer
        Public Property Fecha As Date
        Public Property Id As String = ""
        Public Property Codigo As String = ""
        Public Property Nombre As String = ""
        Public Property Monto As Double
        Public Property Detalle As String = ""
    End Class

    ''' <summary>Columnas por las que filtra Entrega de Cheques. Son las de CmbFiltro en FrmEntrega.frm.</summary>
    Public Enum FiltroEntrega
        Nombre
        Cheque
    End Enum

    ''' <summary>
    ''' Consultas de SD1501\FrmverCheque.frm sobre ecccheques, eccchequesconta y ccblibro. No filtran
    ''' por local a propósito: el original tampoco, y un cheque se consulta desde cualquier local.
    ''' </summary>
    Public NotInheritable Class ChequesRepositorio

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <param name="todasLasCuentas">Privilegio 14007. Sin él solo se ven las de ccbusuarioscuentas.</param>
        Public Async Function ListarBancosAsync(local As Long, uid As Long, todasLasCuentas As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of List(Of BancoCheque))
            Dim permiso = If(todasLasCuentas, "", " INNER JOIN ccbusuarioscuentas uc ON (uc.banco = b.banco AND uc.usuario = @uid) ")
            Dim sql = "SELECT DISTINCT * FROM (" &
                      "SELECT DISTINCT b.banco AS Codigo, b.descripcion AS Descripcion FROM gatbancos b " &
                      "INNER JOIN ccbcuentas ca ON (ca.local = @local AND ca.banco = b.banco AND ca.cheques = '1') " & permiso &
                      "UNION ALL " &
                      "SELECT DISTINCT b.banco AS Codigo, b.descripcion AS Descripcion FROM gatbancos b " &
                      "INNER JOIN ccbcuentasconta a ON (a.local = @local AND a.banco = b.banco) " &
                      "INNER JOIN ccbcuentas ca ON (ca.banco = b.banco AND ca.cheques = '1') " & permiso &
                      ") a ORDER BY Codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of BancoCheque)(New CommandDefinition(sql, New With {local, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        Public Async Function ListarCuentasAsync(local As Long, banco As Integer, uid As Long, todasLasCuentas As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of List(Of CuentaCheque))
            Dim permisoC = If(todasLasCuentas, "", " INNER JOIN ccbusuarioscuentas uc ON (uc.banco = c.banco AND uc.cuenta = c.cuenta AND uc.usuario = @uid) ")
            Dim permisoA = If(todasLasCuentas, "", " INNER JOIN ccbusuarioscuentas uc ON (uc.banco = a.banco AND uc.cuenta = a.cuenta AND uc.usuario = @uid) ")
            Dim sql = "SELECT c.cuenta AS Cuenta, c.descripcion AS Descripcion FROM ccbcuentas c " &
                      "INNER JOIN ccbcuentas ca ON (ca.local = @local AND ca.banco = @banco AND ca.cuenta = c.cuenta AND ca.cheques = '1') " & permisoC &
                      "UNION ALL " &
                      "SELECT a.cuenta AS Cuenta, b.descripcion AS Descripcion FROM ccbcuentasconta a " &
                      "INNER JOIN ccbcuentas b ON (b.banco = a.banco AND b.cuenta = a.cuenta AND b.cheques = '1') " & permisoA &
                      "WHERE a.local = @local AND a.banco = @banco"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of CuentaCheque)(New CommandDefinition(sql, New With {local, banco, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        ''' <summary>Tabla para el buscador. La consulta incluye los anulados y la anulación no, igual que el original.</summary>
        Public Async Function ListarChequesAsync(banco As Integer, cuenta As String, Optional soloVigentes As Boolean = False, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Dim sql = "SELECT cheque AS Cheque, fecha AS Fecha, nombre AS Nombre, monto AS Monto FROM ecccheques WHERE cuenta = @cuenta AND banco = @banco" &
                      If(soloVigentes, " AND anulado = '0'", "")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {banco, cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>Devuelve el cheque con su asiento, o Nothing si no existe.</summary>
        Public Async Function ObtenerAsync(banco As Integer, cuenta As String, numero As Integer, Optional ct As CancellationToken = Nothing) As Task(Of ChequeConsulta)
            Const sqlCheque As String =
                "SELECT a.cheque AS Numero, a.local AS Local, a.fecha AS Fecha, a.nombre AS Nombre, a.monto AS Monto, a.otro AS CodigoSeguridad, " &
                "a.observaciones AS Observaciones, a.anulado AS Anulado, b.usuario AS Usuario, a.id AS Id, a.codigo AS Codigo " &
                "FROM ecccheques a LEFT JOIN gatusuarios b ON b.uid = a.uidc " &
                "WHERE a.cuenta = @cuenta AND a.banco = @banco AND a.cheque = @numero"
            Const sqlConciliado As String =
                "SELECT conciliado FROM ccblibro WHERE cuenta = @cuenta AND banco = @banco AND documento = @numero AND monto = @monto AND tipo = 0"
            Const sqlAsiento As String =
                "SELECT ccontable AS Cuenta, detalle AS Detalle, md AS Debe, mh AS Haber, System AS Sistema, tipoac AS TipoAc FROM eccchequesconta " &
                "WHERE cuenta = @cuenta AND banco = @banco AND cheque = @numero ORDER BY system, pos"

            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim cheque = Await cn.QueryFirstOrDefaultAsync(Of ChequeConsulta)(New CommandDefinition(sqlCheque, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                If cheque Is Nothing Then Return Nothing
                Dim conciliado = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sqlConciliado, New With {banco, cuenta, numero, .monto = cheque.Monto}, cancellationToken:=ct)).ConfigureAwait(False)
                cheque.Conciliado = conciliado = "1"
                Dim lineas = Await cn.QueryAsync(Of LineaAsiento)(New CommandDefinition(sqlAsiento, New With {banco, cuenta, numero}, cancellationToken:=ct)).ConfigureAwait(False)
                cheque.Lineas = lineas.ToList()
                Return cheque
            End Using
        End Function

        ''' <summary>
        ''' Cheques sin retirar (FrmEntrega.LoadCheques). Igual que el original, no filtra por local ni
        ''' excluye los anulados. Los espacios del texto valen como comodines.
        ''' </summary>
        Public Async Function ListarPorEntregarAsync(filtro As FiltroEntrega, texto As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of ChequeEntrega))
            Dim columna = If(filtro = FiltroEntrega.Cheque, "cheque", "nombre")
            Dim sql = "SELECT banco AS Banco, cuenta AS Cuenta, cheque AS Cheque, fecha AS Fecha, id AS Id, codigo AS Codigo, " &
                      "nombre AS Nombre, monto AS Monto, observaciones AS Detalle FROM ecccheques " &
                      "WHERE fecharetiro = '1900-01-01' AND " & columna & " LIKE @patron ORDER BY fecha, nombre, cheque"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of ChequeEntrega)(New CommandDefinition(sql, New With {.patron = PatronFiltro(texto)}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        Public Shared Function PatronFiltro(texto As String) As String
            Return "%" & If(texto, "").Replace(" ", "%") & "%"
        End Function

        ''' <summary>
        ''' Registra quién retiró los cheques (FrmEntrega.CmdEntregar_Click). Las tablas son MyISAM, así que
        ''' si falla a la mitad los anteriores quedan entregados.
        ''' </summary>
        ''' <returns>Cheques encontrados.</returns>
        Public Async Function EntregarAsync(cheques As IEnumerable(Of ChequeEntrega), retiradoPor As String, uid As Long, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Const sql As String = "UPDATE ecccheques SET fecharetiro = @fecha, retiradopor = @retiradoPor, uidcentrega = @uid " &
                                  "WHERE banco = @banco AND cuenta = @cuenta AND cheque = @cheque"
            Dim fecha = Date.Now
            retiradoPor = If(retiradoPor, "").Replace("'", "")
            Dim total = 0
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                For Each c In cheques
                    total += Await cn.ExecuteAsync(New CommandDefinition(sql, New With {fecha, retiradoPor, uid, .banco = c.Banco, .cuenta = c.Cuenta, .cheque = c.Cheque}, cancellationToken:=ct)).ConfigureAwait(False)
                Next
            End Using
            Return total
        End Function

        ''' <summary>Bancos con cuentas de cheques en el local (FrmConfigCheque.LoadDatosCombo).</summary>
        Public Async Function ListarBancosConfigAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of BancoCheque))
            Const sql As String = "SELECT DISTINCT b.banco AS Codigo, b.descripcion AS Descripcion FROM gatbancos b " &
                                  "INNER JOIN ccbcuentas ca ON (ca.local = @local AND ca.banco = b.banco AND ca.cheques = '1') ORDER BY Codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of BancoCheque)(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        ''' <summary>
        ''' Busca primero la configuración del local y luego la general (local -1). Si no hay ninguna
        ''' devuelve los valores predeterminados, para grabarlos en el local. El original dejaba en
        ''' esos casos el local del banco visto antes, o 0, y grababa en un local ajeno.
        ''' </summary>
        Public Async Function CargarConfigAsync(local As Long, banco As Integer, Optional ct As CancellationToken = Nothing) As Task(Of ConfigCheque)
            Const sql As String = "SELECT local, banco, campo, tipo, imprime, ctop, cleft, cheight, cwidth, fuente, size, bold, italic, alineacion, formato " &
                                  "FROM eccchequesconfig WHERE local = @local AND banco = @banco ORDER BY tipo, campo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                For Each origen In {local, -1L}
                    Dim filas = (Await cn.QueryAsync(Of FilaConfigCheque)(New CommandDefinition(sql, New With {.local = origen, banco}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                    If filas.Count > 0 Then Return ConfigCheque.DesdeFilas(filas, origen)
                Next
            End Using
            Return New ConfigCheque With {.Local = local, .SinRegistro = True}
        End Function

        ''' <summary>Las tablas son MyISAM: si falla a la mitad, las filas anteriores quedan grabadas.</summary>
        Public Async Function GuardarConfigAsync(filas As IEnumerable(Of FilaConfigCheque), Optional ct As CancellationToken = Nothing) As Task
            Const sql As String = "REPLACE INTO eccchequesconfig (local, banco, campo, tipo, imprime, ctop, cleft, cheight, cwidth, fuente, size, bold, italic, alineacion, formato) " &
                                  "VALUES (@Local, @Banco, @Campo, @Tipo, @Imprime, @Ctop, @Cleft, @Cheight, @Cwidth, @Fuente, @Size, @Bold, @Italic, @Alineacion, @Formato)"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                For Each f In filas
                    Await cn.ExecuteAsync(New CommandDefinition(sql, f, cancellationToken:=ct)).ConfigureAwait(False)
                Next
            End Using
        End Function

        ''' <summary>La empresa es la de la cédula de la licencia, como VerificarEmpresa de FrmMain3.</summary>
        Public Async Function ObtenerFormatoCuentaAsync(cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of FormatoCuenta)
            Const sql As String = "SELECT Codificacion, showgands <> 0 AS MostrarGrupos FROM sgcempresas WHERE Cedula = @cedula LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim fila = Await cn.QueryFirstOrDefaultAsync(Of EmpresaFila)(New CommandDefinition(sql, New With {cedula}, cancellationToken:=ct)).ConfigureAwait(False)
                If fila Is Nothing Then Return FormatoCuenta.Predeterminado
                Return New FormatoCuenta(fila.Codificacion, fila.MostrarGrupos)
            End Using
        End Function

        Private NotInheritable Class EmpresaFila
            Public Property Codificacion As String = ""
            Public Property MostrarGrupos As Boolean
        End Class

    End Class

End Namespace
