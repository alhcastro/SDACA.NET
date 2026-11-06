Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Catalogos

    ''' <summary>Fila de <c>gatbancos</c>. Numloc está vacío en todos y Finport lo maneja la conciliación.</summary>
    Public NotInheritable Class BancoCatalogo
        Public Property Codigo As Integer
        Public Property Descripcion As String = ""
    End Class

    ''' <summary>Fila de <c>gatmonedas</c>.</summary>
    Public NotInheritable Class MonedaCatalogo
        Public Property Codigo As Integer
        Public Property Descripcion As String = ""
        Public Property Simbolo As String = ""
        Public Property Iniciales As String = ""

        Public Overrides Function ToString() As String
            Return $"{Codigo} - {Descripcion}"
        End Function
    End Class

    ''' <summary>Fila de <c>gattipocambio</c>.</summary>
    Public NotInheritable Class TipoCambio
        Public Property Moneda As Integer
        Public Property Fecha As Date
        Public Property Compra As Double
        Public Property Venta As Double
    End Class

    ''' <summary>
    ''' Bancos, monedas y tipos de cambio. Migración de SD0001\FrmMantBancos.frm, FrmMantMonedas.frm y
    ''' FrmTipoCambio.frm, con el esquema real: gatbancos no tiene siglas ni web services, gatmonedas
    ''' tiene Iniciales y gattipocambio es por moneda y día, sin banco ni hora. Se replica como GATMANT.
    ''' </summary>
    Public NotInheritable Class CatalogosMonetarios

        Private Const IdentificadorReplica As String = "GATMANT"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Async Function EjecutarAsync(cn As MySqlConnection, sql As String, local As Long, ct As CancellationToken) As Task
            Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
            Await Replicador.ReplicarAsync(cn, IdentificadorReplica, sql, local, ct).ConfigureAwait(False)
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

        Private Async Function SiguienteAsync(sql As String, ct As CancellationToken) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Function SiguienteBancoAsync(Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Return SiguienteAsync("SELECT IFNULL(MAX(Banco),-1) + 1 FROM gatbancos", ct)
        End Function

        Public Function BuscarBancosAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Return TablaAsync("SELECT Banco AS 'Código', Descripcion AS 'Descripción' FROM gatbancos ORDER BY Banco", Nothing, ct)
        End Function

        Public Async Function ObtenerBancoAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task(Of BancoCatalogo)
            Const sql As String = "SELECT Banco AS Codigo, Descripcion FROM gatbancos WHERE Banco = @codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of BancoCatalogo)(New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function GuardarBancoAsync(banco As BancoCatalogo, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarAsync(cn, SentenciaBanco(banco, uid), local, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Conserva Finport, que es la configuración de columnas del CSV de la conciliación.</summary>
        Friend Shared Function SentenciaBanco(b As BancoCatalogo, uid As Long) As String
            Dim descripcion = LibroBancos.Literal(b.Descripcion)
            Return $"INSERT INTO gatbancos (Banco, Descripcion, uidc, FC) VALUES ({b.Codigo}, '{descripcion}', {uid}, NOW()) ON DUPLICATE KEY UPDATE Descripcion = '{descripcion}'"
        End Function

        Public Function SiguienteMonedaAsync(Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Return SiguienteAsync("SELECT IFNULL(MAX(Moneda),-1) + 1 FROM gatmonedas", ct)
        End Function

        Public Function BuscarMonedasAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Return TablaAsync("SELECT Moneda AS 'Código', Descripcion AS 'Descripción' FROM gatmonedas ORDER BY Moneda", Nothing, ct)
        End Function

        Public Async Function ListarMonedasExtranjerasAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of MonedaCatalogo))
            Const sql As String = "SELECT Moneda AS Codigo, Descripcion, Simbolo, Iniciales FROM gatmonedas WHERE Moneda <> 0 ORDER BY Moneda"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of MonedaCatalogo)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        Public Async Function ObtenerMonedaAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task(Of MonedaCatalogo)
            Const sql As String = "SELECT Moneda AS Codigo, Descripcion, Simbolo, Iniciales FROM gatmonedas WHERE Moneda = @codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of MonedaCatalogo)(New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function GuardarMonedaAsync(moneda As MonedaCatalogo, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarAsync(cn, SentenciaMoneda(moneda, uid), local, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>VB6 usa REPLACE con 6 valores en una tabla de 7 columnas; aquí además se conserva FC.</summary>
        Friend Shared Function SentenciaMoneda(m As MonedaCatalogo, uid As Long) As String
            Dim descripcion = LibroBancos.Literal(m.Descripcion)
            Dim simbolo = LibroBancos.Literal(m.Simbolo)
            Dim iniciales = LibroBancos.Literal(m.Iniciales)
            Return $"INSERT INTO gatmonedas (Moneda, Descripcion, Simbolo, Iniciales, uidc, FC) VALUES ({m.Codigo}, '{descripcion}', '{simbolo}', '{iniciales}', {uid}, NOW()) " &
                   $"ON DUPLICATE KEY UPDATE Descripcion = '{descripcion}', Simbolo = '{simbolo}', Iniciales = '{iniciales}'"
        End Function

        ''' <summary>El tipo de cambio de ese día, o el último si no se indica fecha.</summary>
        Public Async Function ObtenerTipoCambioAsync(moneda As Integer, Optional fecha As Date? = Nothing, Optional ct As CancellationToken = Nothing) As Task(Of TipoCambio)
            Dim sql = "SELECT moneda AS Moneda, fecha AS Fecha, compra AS Compra, venta AS Venta FROM gattipocambio WHERE moneda = @moneda" &
                      If(fecha.HasValue, " AND fecha = @fecha", "") & " ORDER BY fecha DESC LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of TipoCambio)(New CommandDefinition(sql, New With {moneda, fecha}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Function BuscarTiposCambioAsync(moneda As Integer, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT DATE_FORMAT(a.fecha,'%d/%m/%Y') AS Fecha, a.compra AS Compra, a.venta AS Venta, b.Usuario AS Creadopor, c.Usuario AS Editadopor FROM gattipocambio a " &
                                  "LEFT JOIN gatusuarios b ON (b.UID = a.uidc) LEFT JOIN gatusuarios c ON (c.UID = a.uide) WHERE a.moneda = @moneda ORDER BY a.fecha DESC"
            Return TablaAsync(sql, New With {moneda}, ct)
        End Function

        Public Async Function GuardarTipoCambioAsync(t As TipoCambio, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Const existe As String = "SELECT COUNT(*) FROM gattipocambio WHERE moneda = @Moneda AND fecha = @Fecha"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim hay = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(existe, New With {t.Moneda, t.Fecha}, cancellationToken:=ct)).ConfigureAwait(False) > 0
                Await EjecutarAsync(cn, SentenciaTipoCambio(t, uid, hay), local, ct).ConfigureAwait(False)
            End Using
        End Function

        Friend Shared Function SentenciaTipoCambio(t As TipoCambio, uid As Long, existe As Boolean) As String
            Dim fecha = t.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            Dim compra = t.Compra.ToString("R", CultureInfo.InvariantCulture)
            Dim venta = t.Venta.ToString("R", CultureInfo.InvariantCulture)
            If existe Then Return $"UPDATE gattipocambio SET compra = {compra}, venta = {venta}, uide = {uid}, fe = NOW() WHERE moneda = {t.Moneda} AND fecha = '{fecha}'"
            Return $"INSERT INTO gattipocambio (moneda, fecha, compra, venta, uidc, fc) VALUES ({t.Moneda}, '{fecha}', {compra}, {venta}, {uid}, NOW())"
        End Function

    End Class

End Namespace
