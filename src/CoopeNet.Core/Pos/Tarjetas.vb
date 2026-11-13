Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Un tipo de tarjeta (postarjetas): el emisor con que se cobra con tarjeta en el POS.</summary>
    Public NotInheritable Class TarjetaPos
        ''' <summary>0 mientras no se ha guardado. postarjetas.Codigo es char(2): de 1 a 99.</summary>
        Public Property Codigo As Integer
        Public Property Nombre As String = ""
        Public Property Telefono As String = ""
        Public Property Fax As String = ""
        Public Property TelefonoConexion As String = ""
        ''' <summary>No está en la pantalla; se conserva al guardar.</summary>
        Public Property TelefonoConsulta As String = ""
        Public Property PComision As Double
        ''' <summary>FC: fecha de creación. Nothing en las que VB6 guardó sin fecha.</summary>
        Public Property FC As Date?
    End Class

    ''' <summary>
    ''' Tipos de tarjeta del POS. Migración de SD0502\frmTarjetas.frm (SD_Guardar, SD_Cargar, Borrar y Buscar). El
    ''' modo Init, que frmPago abre para crear una tarjeta al cobrar, queda para cuando se migre la facturación.
    ''' </summary>
    ''' <remarks>
    ''' Guardar se replica a los locales de gatreplicadef; Borrar no, igual que en VB6. Codigo es texto, así que el
    ''' orden y el siguiente código se calculan como número (VB6 los calculaba como texto: después de la 9 venía la
    ''' 10 antes que la 2). VB6 dejaba FC en NULL y TelefonoConsulta vacío en cada guardado; aquí FC es la fecha de
    ''' creación y TelefonoConsulta se conserva.
    ''' </remarks>
    Public NotInheritable Class TarjetasRepositorio

        Public Const Replica As String = "postarjetas"
        Public Const CodigoMaximo As Integer = 99
        Public Const LargoNombre As Integer = 32
        Public Const LargoTelefono As Integer = 32
        Public Const LargoFax As Integer = 16
        Public Const LargoConexion As Integer = 16

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String, largo As Integer) As String
            Dim t = If(texto, "").Trim()
            If t.Length > largo Then t = t.Substring(0, largo)
            Return $"'{LibroBancos.Literal(t)}'"
        End Function

        ''' <summary>SDTDescri_LostFocus: el nombre va en mayúsculas.</summary>
        Public Shared Function NormalizarNombre(nombre As String) As String
            Return If(nombre, "").Trim().ToUpper(CultureInfo.CurrentCulture)
        End Function

        ''' <summary>El REPLACE de SD_Guardar con todas las columnas, para ejecutarlo y replicarlo igual.</summary>
        Friend Shared Function SentenciaGuardar(t As TarjetaPos, uid As Long, ahora As Date) As String
            Dim fc = If(t.FC, ahora)
            Return "REPLACE INTO postarjetas (Codigo, Nombre, Telefono, Fax, TelefonoConexion, TelefonoConsulta, PComision, FC, Usuario) VALUES (" &
                   String.Join(", ", {
                       Q(t.Codigo.ToString(Invariante), 2), Q(NormalizarNombre(t.Nombre), LargoNombre), Q(t.Telefono, LargoTelefono), Q(t.Fax, LargoFax),
                       Q(t.TelefonoConexion, LargoConexion), Q(t.TelefonoConsulta, LargoConexion), t.PComision.ToString("R", Invariante),
                       Q(fc.ToString("yyyy-MM-dd HH:mm:ss", Invariante), 19), Q(uid.ToString(Invariante), 16)}) & ")"
        End Function

        Private Const Columnas As String =
            "SELECT CAST(Codigo AS UNSIGNED) AS Codigo, Nombre, Telefono, Fax, TelefonoConexion, TelefonoConsulta, IFNULL(PComision, 0) AS PComision, FC FROM postarjetas"

        Public Async Function CargarAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task(Of TarjetaPos)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of TarjetaPos)(New CommandDefinition(
                    Columnas & " WHERE CAST(Codigo AS UNSIGNED) = @codigo LIMIT 1", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>cmdAnterior y cmdProximo: la vecina en orden numérico, o Nothing en el extremo.</summary>
        Public Async Function VecinaAsync(codigo As Integer, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of TarjetaPos)
            Dim sql = Columnas & If(siguiente,
                " WHERE CAST(Codigo AS UNSIGNED) > @codigo ORDER BY CAST(Codigo AS UNSIGNED) LIMIT 1",
                " WHERE CAST(Codigo AS UNSIGNED) < @codigo ORDER BY CAST(Codigo AS UNSIGNED) DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of TarjetaPos)(New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar de la barra. Columnas: Código y Nombre.</summary>
        Public Async Function BuscarAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT Codigo AS `Código`, Nombre FROM postarjetas ORDER BY CAST(Codigo AS UNSIGNED)"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>
        ''' SD_Guardar: una nueva toma el siguiente código. Devuelve el código con que quedó, o 0 si ya no caben
        ''' más (el código tiene dos dígitos).
        ''' </summary>
        Public Async Function GuardarAsync(t As TarjetaPos, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim codigo = t.Codigo
                If codigo = 0 Then
                    codigo = Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition(
                        "SELECT IFNULL(MAX(CAST(Codigo AS UNSIGNED)) + 1, 1) FROM postarjetas", cancellationToken:=ct)).ConfigureAwait(False)
                    If codigo > CodigoMaximo Then Return 0
                End If
                Dim fila As New TarjetaPos With {
                    .Codigo = codigo, .Nombre = t.Nombre, .Telefono = t.Telefono, .Fax = t.Fax, .TelefonoConexion = t.TelefonoConexion,
                    .TelefonoConsulta = t.TelefonoConsulta, .PComision = t.PComision, .FC = t.FC}
                Dim sql = SentenciaGuardar(fila, uid, Date.Now)
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, Replica, sql, local, ct).ConfigureAwait(False)
                Return codigo
            End Using
        End Function

        ''' <summary>Borrar de la barra. Como en VB6, no se replica.</summary>
        Public Async Function BorrarAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM postarjetas WHERE CAST(Codigo AS UNSIGNED) = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>sdtComision: porcentaje con punto o coma decimal; vacío es 0. Nothing si no es válido.</summary>
        Public Shared Function LeerComision(texto As String) As Double?
            Dim t = If(texto, "").Trim().Replace(","c, "."c)
            If t.Length = 0 Then Return 0
            Dim valor As Double
            If Not Double.TryParse(t, NumberStyles.AllowDecimalPoint, Invariante, valor) OrElse valor > 100 Then Return Nothing
            Return valor
        End Function

    End Class

End Namespace
