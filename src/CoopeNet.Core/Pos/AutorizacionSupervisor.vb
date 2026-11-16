Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Datos
Imports CoopeNet.Core.Seguridad

Namespace Pos

    ''' <summary>
    ''' cmdValidar de frmModConsec y frmGenerador: un supervisor entra a la base con su usuario y contraseña de MySQL y
    ''' debe tener el 5000 y el 5030 en el local. root y Administrador no necesitan privilegios.
    ''' </summary>
    Public NotInheritable Class AutorizacionSupervisor

        Private Sub New()
        End Sub

        ''' <summary>El resultado y el uid del supervisor en gatusuarios (0 si no está).</summary>
        Public Shared Async Function AutorizarAsync(fabrica As IFabricaConexiones, login As String, clave As String, local As Long,
                                                    Optional ct As CancellationToken = Nothing) As Task(Of (Resultado As ResultadoAutorizacion, Uid As Long))
            Using cn = Await fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim cadena As New MySqlConnectionStringBuilder(cn.ConnectionString)
                Dim credenciales = CadenaConexion.ResolverCredenciales(login, clave)
                cadena.UserID = credenciales.UsuarioMySql
                cadena.Password = credenciales.Contrasena
                Try
                    Using prueba As New MySqlConnection(cadena.ConnectionString)
                        Await prueba.OpenAsync(ct).ConfigureAwait(False)
                    End Using
                Catch ex As MySqlException
                    Return (ResultadoAutorizacion.NoAutorizado, 0)
                End Try
                Dim uid = CLng(Vb6.Val(Convert.ToString(Await cn.ExecuteScalarAsync(Of Object)(New CommandDefinition(
                    "SELECT UID FROM gatusuarios WHERE USUARIO = @login", New With {login}, cancellationToken:=ct)).ConfigureAwait(False), CultureInfo.InvariantCulture)))
                If Privilegios.EsAdministrador(login) Then Return (ResultadoAutorizacion.Autorizado, uid)
                Dim total = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(
                    "SELECT COUNT(uid) FROM gatusuariospriv2 WHERE uid = @hash AND pri IN (@p5000, @p5030)",
                    New With {.hash = Privilegios.HashUsuario(local, uid), .p5000 = Privilegios.HashPrivilegio(local, uid, 5000), .p5030 = Privilegios.HashPrivilegio(local, uid, 5030)},
                    cancellationToken:=ct)).ConfigureAwait(False)
                Return (If(total >= 2, ResultadoAutorizacion.Autorizado, ResultadoAutorizacion.SinPrivilegio), uid)
            End Using
        End Function

    End Class

End Namespace
