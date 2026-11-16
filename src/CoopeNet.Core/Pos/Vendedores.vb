Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Un usuario que se puede asignar como vendedor (cmbVendedores): uid, login y nombre.</summary>
    Public NotInheritable Class UsuarioVendedor
        Public Property Uid As Long
        Public Property Usuario As String = ""
        Public Property Nombre As String = ""

        Public Overrides Function ToString() As String
            Return $"{Uid} - {Usuario}"
        End Function
    End Class

    ''' <summary>
    ''' Vendedores asignados al local (posvendedores). Migración de SD0502\frmVendedores.frm (SD_ComboVend, SD_Cargar,
    ''' SD_Guardar, Borrar y Buscar). Id es el uid del vendedor y Uid quien lo asignó. No se replica.
    ''' </summary>
    ''' <remarks>
    ''' FC es int: VB6 guardaba NOW() y MySQL lo dejaba en el año o en 2147483647. Aquí se guarda la fecha como
    ''' número aaaammdd, que sí cabe.
    ''' </remarks>
    Public NotInheritable Class VendedoresRepositorio

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Shared Function FechaEntera(fecha As Date) As Integer
            Return Integer.Parse(fecha.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
        End Function

        ''' <summary>
        ''' SD_ComboVend: los usuarios con cuenta en MySQL. Se agregan los vendedores del local que ya no la tienen, para
        ''' que se puedan ver y borrar (en VB6 el combo quedaba vacío).
        ''' </summary>
        Public Async Function UsuariosAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of UsuarioVendedor))
            Const sql As String =
                "SELECT u.uid AS Uid, CAST(u.usuario AS CHAR) AS Usuario, TRIM(CONCAT_WS(' ', D.nombre, D.apellido1, D.apellido2)) AS Nombre " &
                "FROM gatusuarios u LEFT JOIN gacdp D ON D.id = u.id " &
                "WHERE CAST(u.usuario AS CHAR) IN (SELECT CAST(User AS CHAR) FROM mysql.user) " &
                "OR u.uid IN (SELECT Id FROM posvendedores WHERE Local = @local) ORDER BY u.uid"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of UsuarioVendedor)(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>SD_Cargar con Consulta: ¿está asignado en el local?</summary>
        Public Async Function ExisteAsync(id As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(
                    "SELECT COUNT(*) FROM posvendedores WHERE Local = @local AND Id = @id", New With {id, local}, cancellationToken:=ct)).ConfigureAwait(False) > 0
            End Using
        End Function

        ''' <summary>cmdAnterior y cmdProximo: el uid vecino asignado en el local, o Nothing en el extremo.</summary>
        Public Async Function VecinoAsync(id As Long, local As Long, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of Long?)
            Dim sql = "SELECT Id FROM posvendedores WHERE Local = @local" &
                If(siguiente, " AND Id > @id ORDER BY Id LIMIT 1", " AND Id < @id ORDER BY Id DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long?)(New CommandDefinition(sql, New With {id, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar de la barra. Columnas: Código, Usuario y Nombre.</summary>
        Public Async Function BuscarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String =
                "SELECT V.Id AS `Código`, CAST(G.usuario AS CHAR) AS Usuario, TRIM(CONCAT_WS(' ', D.nombre, D.apellido1, D.apellido2)) AS Nombre " &
                "FROM posvendedores V INNER JOIN gatusuarios G ON G.uid = V.Id LEFT JOIN gacdp D ON D.id = G.id WHERE V.Local = @local ORDER BY V.Id"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>SD_Guardar: el REPLACE de VB6, con FC como aaaammdd.</summary>
        Public Async Function GuardarAsync(id As Long, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(
                    "REPLACE INTO posvendedores (Id, Uid, FC, Local) VALUES (@id, @uid, @fc, @local)",
                    New With {id, uid, .fc = FechaEntera(Date.Today), local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Borrar de la barra.</summary>
        Public Async Function BorrarAsync(id As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posvendedores WHERE Id = @id AND Local = @local", New With {id, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
