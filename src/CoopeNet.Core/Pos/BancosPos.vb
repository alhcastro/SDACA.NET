Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Un banco del POS (posbancos): el banco de los cheques y transferencias con que se cobra.</summary>
    Public NotInheritable Class BancoPos
        ''' <summary>0 mientras no se ha guardado. posbancos.Codigo es char(2): de 1 a 99.</summary>
        Public Property Codigo As Integer
        Public Property Nombre As String = ""
        Public Property Telefono As String = ""
        Public Property Fax As String = ""
        ''' <summary>FC: fecha de creación.</summary>
        Public Property FC As Date?
    End Class

    ''' <summary>
    ''' Bancos del POS. Migración de SD0502\frmBancos.frm (SD_Guardar, SD_Cargar, Borrar y Buscar). Es posbancos,
    ''' distinta de gatbancos de SD0001. El modo Init, que frmPago abre para crear un banco al cobrar, queda para
    ''' cuando se migre la facturación.
    ''' </summary>
    ''' <remarks>
    ''' Guardar se replica a los locales de gatreplicadef; Borrar no, igual que en VB6. Codigo es texto: VB6 ordenaba
    ''' y calculaba el siguiente como texto, así que con el banco 10 anterior y siguiente se saltaban del 1 al 10 y un
    ''' banco nuevo tomaba el código 10 y reemplazaba al que ya lo tenía. Aquí se calcula como número. VB6 además
    ''' cambiaba FC a la fecha del guardado; aquí FC es la fecha de creación.
    ''' </remarks>
    Public NotInheritable Class BancosPosRepositorio

        Public Const Replica As String = "posbancos"
        Public Const CodigoMaximo As Integer = 99
        Public Const LargoNombre As Integer = 128
        Public Const LargoTelefono As Integer = 16

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

        ''' <summary>El REPLACE de SD_Guardar, para ejecutarlo y replicarlo igual.</summary>
        Friend Shared Function SentenciaGuardar(b As BancoPos, uid As Long, ahora As Date) As String
            Dim fc = If(b.FC, ahora)
            Return "REPLACE INTO posbancos (Codigo, Nombre, Telefono, Fax, FC, Usuario) VALUES (" &
                   String.Join(", ", {
                       Q(b.Codigo.ToString(Invariante), 2), Q(NormalizarNombre(b.Nombre), LargoNombre), Q(b.Telefono, LargoTelefono), Q(b.Fax, LargoTelefono),
                       Q(fc.ToString("yyyy-MM-dd HH:mm:ss", Invariante), 19), Q(uid.ToString(Invariante), 16)}) & ")"
        End Function

        Private Const Columnas As String = "SELECT CAST(Codigo AS UNSIGNED) AS Codigo, Nombre, Telefono, Fax, FC FROM posbancos"

        Public Async Function CargarAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task(Of BancoPos)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of BancoPos)(New CommandDefinition(
                    Columnas & " WHERE CAST(Codigo AS UNSIGNED) = @codigo LIMIT 1", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>cmdAnterior y cmdProximo: el vecino en orden numérico, o Nothing en el extremo.</summary>
        Public Async Function VecinoAsync(codigo As Integer, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of BancoPos)
            Dim sql = Columnas & If(siguiente,
                " WHERE CAST(Codigo AS UNSIGNED) > @codigo ORDER BY CAST(Codigo AS UNSIGNED) LIMIT 1",
                " WHERE CAST(Codigo AS UNSIGNED) < @codigo ORDER BY CAST(Codigo AS UNSIGNED) DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of BancoPos)(New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar de la barra. Columnas: Código y Nombre.</summary>
        Public Async Function BuscarAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT Codigo AS `Código`, Nombre FROM posbancos ORDER BY CAST(Codigo AS UNSIGNED)"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>
        ''' SD_Guardar: uno nuevo toma el siguiente código. Devuelve el código con que quedó, o 0 si ya no caben más.
        ''' </summary>
        Public Async Function GuardarAsync(b As BancoPos, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim codigo = b.Codigo
                If codigo = 0 Then
                    codigo = Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition(
                        "SELECT IFNULL(MAX(CAST(Codigo AS UNSIGNED)) + 1, 1) FROM posbancos", cancellationToken:=ct)).ConfigureAwait(False)
                    If codigo > CodigoMaximo Then Return 0
                End If
                Dim fila As New BancoPos With {.Codigo = codigo, .Nombre = b.Nombre, .Telefono = b.Telefono, .Fax = b.Fax, .FC = b.FC}
                Dim sql = SentenciaGuardar(fila, uid, Date.Now)
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, Replica, sql, local, ct).ConfigureAwait(False)
                Return codigo
            End Using
        End Function

        ''' <summary>Borrar de la barra. Como en VB6, no se replica.</summary>
        Public Async Function BorrarAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posbancos WHERE CAST(Codigo AS UNSIGNED) = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
