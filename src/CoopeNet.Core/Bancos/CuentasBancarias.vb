Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Core.Datos

Namespace Bancos

    ''' <summary>Elemento de una lista: departamento, banco o moneda.</summary>
    Public NotInheritable Class OpcionCodigo
        Public Property Codigo As Long
        Public Property Descripcion As String = ""

        Public Overrides Function ToString() As String
            Return Descripcion
        End Function
    End Class

    ''' <summary>Fila de <c>ccbcuentas</c>, con las 20 columnas de la base.</summary>
    Public NotInheritable Class CuentaBancaria
        Public Property Local As Long
        Public Property Banco As Long
        Public Property Cuenta As String = ""
        Public Property Descripcion As String = ""
        Public Property Moneda As Long
        Public Property MontoInicial As Double
        Public Property ChequesTransito As Double
        Public Property DepositosTransito As Double
        ''' <summary>0 ahorros, 1 corriente, 2 caja chica.</summary>
        Public Property Tipo As String = "0"
        Public Property Cheques As String = "0"
        Public Property NumCheque As Long
        Public Property Conta As String = "0"
        Public Property Contable As String = ""
        Public Property DetalleContable As String = ""
        Public Property Conversion As String = ""
        Public Property DetalleConversion As String = ""
        Public Property CuentaBanco As String = ""
        Public Property NumeroCliente As String = ""

        Public Shared Function SaldoInicial(montoInicial As Double, chequesTransito As Double, depositosTransito As Double) As Double
            Return montoInicial - chequesTransito + depositosTransito
        End Function
    End Class

    Public NotInheritable Class UsuarioCuenta
        Public Property Uid As Long
        Public Property Usuario As String = ""
        Public Property Id As String = ""
        Public Property Nombre As String = ""
    End Class

    ''' <summary>Fila de <c>ccbcuentasconta</c>: la cuenta contable que usa otro departamento.</summary>
    Public NotInheritable Class ContaDepartamento
        Public Property Local As Long
        Public Property Departamento As String = ""
        Public Property Contable As String = ""
        Public Property Descripcion As String = ""
        Public Property Conversion As String = ""
    End Class

    ''' <summary>
    ''' Migración de SD1401\FrmCuentasBancarias.frm, FrmCuentasUsuarios.frm y FrmCuentasxDepartamento.frm.
    ''' Fuentes\SD1401 espera las columnas cconvh, cconvdd y cconvhd del Init de Clase.cls. La base no
    ''' las tiene: conserva cconv (código) y cconvd (descripción), que es lo que se usa aquí.
    ''' </summary>
    Public NotInheritable Class CuentasBancariasRepositorio

        Private Const IdentificadorReplica As String = "BANCOS"
        ''' <summary>VB6 espera un segundo entre el DELETE y los INSERT. Se conserva por la réplica.</summary>
        Private Const EsperaReplica As Integer = 1000

        Private Const ColumnasCuenta As String =
            "SELECT local AS Local, banco AS Banco, cuenta AS Cuenta, Descripcion, moneda AS Moneda, montoinicial AS MontoInicial, " &
            "montoinicialTransito AS ChequesTransito, montoinicialdeptransito AS DepositosTransito, Tipo, Cheques, NumCheque, conta AS Conta, " &
            "ccontable AS Contable, cdetalle AS DetalleContable, cconv AS Conversion, cconvd AS DetalleConversion, cuentab AS CuentaBanco, " &
            "numerocliente AS NumeroCliente FROM ccbcuentas"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Async Function ListaAsync(sql As String, ct As CancellationToken) As Task(Of List(Of OpcionCodigo))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of OpcionCodigo)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        Public Function ListarLocalesAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionCodigo))
            Return ListaAsync("SELECT local AS Codigo, descripcion AS Descripcion FROM gatlocales ORDER BY local", ct)
        End Function

        Public Function ListarBancosAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionCodigo))
            Return ListaAsync("SELECT DISTINCT banco AS Codigo, descripcion AS Descripcion FROM gatbancos ORDER BY banco", ct)
        End Function

        Public Function ListarMonedasAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionCodigo))
            Return ListaAsync("SELECT moneda AS Codigo, descripcion AS Descripcion FROM gatmonedas ORDER BY moneda", ct)
        End Function

        Public Async Function ObtenerAsync(local As Long, banco As Long, cuenta As String, Optional ct As CancellationToken = Nothing) As Task(Of CuentaBancaria)
            Const sql As String = ColumnasCuenta & " WHERE cuenta = @cuenta AND banco = @banco AND local = @local"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of CuentaBancaria)(New CommandDefinition(sql, New With {local, banco, cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>La llave es banco y cuenta: REPLACE con otro local sobrescribiría esa cuenta.</summary>
        Public Async Function LocalDeCuentaAsync(banco As Long, cuenta As String, Optional ct As CancellationToken = Nothing) As Task(Of Long?)
            Const sql As String = "SELECT local FROM ccbcuentas WHERE banco = @banco AND cuenta = @cuenta"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of Long?)(New CommandDefinition(sql, New With {banco, cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Function BuscarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT c.cuenta AS Cuenta, c.Descripcion AS 'Descripción', b.descripcion AS Banco, b.banco AS CodBanc " &
                                  "FROM ccbcuentas c INNER JOIN gatbancos b ON (c.banco = b.banco) WHERE local = @local"
            Return TablaAsync(sql, New With {local}, ct)
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

        ''' <summary>La sentencia va como texto porque el mismo texto se replica a los otros locales.</summary>
        Public Async Function GuardarAsync(c As CuentaBancaria, uid As Long, localSesion As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim sql = SentenciaGuardar(c, uid, Date.Now)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarAsync(cn, sql, localSesion, ct).ConfigureAwait(False)
            End Using
        End Function

        Friend Shared Function SentenciaGuardar(c As CuentaBancaria, uid As Long, ahora As Date) As String
            Dim fecha = ahora.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            Return "REPLACE INTO ccbcuentas (local, banco, cuenta, Descripcion, moneda, montoinicial, montoinicialTransito, montoinicialdeptransito, " &
                   "Tipo, Cheques, NumCheque, conta, ccontable, cdetalle, cconv, cconvd, cuentab, numerocliente, uidc, fc) VALUES (" &
                   $"{c.Local},{c.Banco},{Texto(c.Cuenta)},{Texto(c.Descripcion)},{c.Moneda},{Numero(c.MontoInicial)},{Numero(c.ChequesTransito)},{Numero(c.DepositosTransito)}," &
                   $"{Texto(c.Tipo)},{Texto(c.Cheques)},{c.NumCheque},{Texto(c.Conta)},{Texto(c.Contable)},{Texto(c.DetalleContable)},{Texto(c.Conversion)},{Texto(c.DetalleConversion)}," &
                   $"{Texto(c.CuentaBanco)},{Texto(c.NumeroCliente)},{uid},'{fecha}')"
        End Function

        Private Shared Function Texto(valor As String) As String
            Return "'" & LibroBancos.Literal(If(valor, "")) & "'"
        End Function

        Private Shared Function Numero(valor As Double) As String
            Return valor.ToString("R", CultureInfo.InvariantCulture)
        End Function

        Private Shared Async Function EjecutarAsync(cn As MySqlConnection, sql As String, local As Long, ct As CancellationToken) As Task
            Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
            Await Replicador.ReplicarAsync(cn, IdentificadorReplica, sql, local, ct).ConfigureAwait(False)
        End Function

        Public Async Function BuscarCatalogoAsync(formato As FormatoCuenta, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim catalogo = Await EmisionCheques.CatalogoAsync(cn, cedula, ct).ConfigureAwait(False)
                Dim sql = $"SELECT Descripcion AS 'Descripción', {formato.ExpresionSql("codigo")} AS Cuenta, Codigo FROM {catalogo}sgccatalogo WHERE Auxiliar = '1' ORDER BY descripcion"
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <param name="cuenta">Como la ve el usuario, sin los niveles ocultos.</param>
        Public Async Function ObtenerCuentaCatalogoAsync(cuenta As String, formato As FormatoCuenta, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of CuentaCatalogo)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim catalogo = Await EmisionCheques.CatalogoAsync(cn, cedula, ct).ConfigureAwait(False)
                Dim sql = $"SELECT Descripcion, auxiliar AS Auxiliar, codigo AS Codigo FROM {catalogo}sgccatalogo WHERE {formato.ExpresionSql("codigo")} = @cuenta LIMIT 1"
                Return Await cn.QueryFirstOrDefaultAsync(Of CuentaCatalogo)(New CommandDefinition(sql, New With {cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function ListarUsuariosAsync(banco As Long, cuenta As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of UsuarioCuenta))
            Const sql As String = "SELECT a.usuario AS Uid, IFNULL(c.usuario,'') AS Usuario, IFNULL(c.id,'') AS Id, " &
                                  "IFNULL(CONCAT(b.nombre,' ',b.apellido1,' ',b.apellido2),'Nombre no Registrado') AS Nombre " &
                                  "FROM ccbusuarioscuentas a LEFT JOIN gatusuarios c ON (c.uid = a.usuario) LEFT JOIN gacdp b ON (b.id = c.id) " &
                                  "WHERE a.banco = @banco AND a.cuenta = @cuenta"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of UsuarioCuenta)(New CommandDefinition(sql, New With {banco, cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        Public Function BuscarUsuariosAsync(banco As Long, cuenta As String, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT a.uid, a.usuario, a.id, IFNULL(CONCAT(b.nombre,' ',b.apellido1,' ',b.apellido2),'Nombre no Registrado') AS Nombre " &
                                  "FROM gatusuarios a LEFT JOIN gacdp b ON (b.id = a.id) " &
                                  "WHERE a.uid NOT IN (SELECT usuario FROM ccbusuarioscuentas WHERE banco = @banco AND cuenta = @cuenta)"
            Return TablaAsync(sql, New With {banco, cuenta}, ct)
        End Function

        ''' <summary>Igual que VB6: borra todos y agrega los de la lista. MyISAM no deshace si falla a medias.</summary>
        Public Async Function GuardarUsuariosAsync(banco As Long, cuenta As String, usuarios As IEnumerable(Of Long), localSesion As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim cuentaSql = Texto(cuenta)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarAsync(cn, $"DELETE FROM ccbusuarioscuentas WHERE banco = {banco} AND cuenta = {cuentaSql}", localSesion, ct).ConfigureAwait(False)
                Await Task.Delay(EsperaReplica, ct).ConfigureAwait(False)
                For Each uid In usuarios
                    Await EjecutarAsync(cn, $"INSERT INTO ccbusuarioscuentas (banco, cuenta, usuario) VALUES ({banco}, {cuentaSql}, {uid})", localSesion, ct).ConfigureAwait(False)
                Next
            End Using
        End Function

        Public Async Function ListarContaAsync(banco As Long, cuenta As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of ContaDepartamento))
            Const sql As String = "SELECT a.local AS Local, IFNULL(b.descripcion,'') AS Departamento, a.cc AS Contable, a.descripcion AS Descripcion, a.ccconv AS Conversion " &
                                  "FROM ccbcuentasconta a LEFT JOIN gatlocales b ON (b.local = a.local) WHERE a.banco = @banco AND a.cuenta = @cuenta"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of ContaDepartamento)(New CommandDefinition(sql, New With {banco, cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        ''' <summary>Igual que VB6: borra todos y agrega los de la lista. MyISAM no deshace si falla a medias.</summary>
        Public Async Function GuardarContaAsync(banco As Long, cuenta As String, filas As IEnumerable(Of ContaDepartamento), uid As Long, localSesion As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim cuentaSql = Texto(cuenta)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarAsync(cn, $"DELETE FROM ccbcuentasconta WHERE banco = {banco} AND cuenta = {cuentaSql}", localSesion, ct).ConfigureAwait(False)
                Await Task.Delay(EsperaReplica, ct).ConfigureAwait(False)
                For Each f In filas
                    Dim sql = "INSERT INTO ccbcuentasconta (local, banco, cuenta, cc, descripcion, ccconv, uidc, fc, fm) VALUES " &
                              $"({f.Local}, {banco}, {cuentaSql}, {Texto(f.Contable)}, {Texto(f.Descripcion)}, {Texto(f.Conversion)}, {uid}, NOW(), NULL)"
                    Await EjecutarAsync(cn, sql, localSesion, ct).ConfigureAwait(False)
                Next
            End Using
        End Function

    End Class

End Namespace
