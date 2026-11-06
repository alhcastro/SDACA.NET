Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Datos

Namespace Seguridad

    Public NotInheritable Class LocalEntrada
        Public Property Local As Long
        Public Property Descripcion As String = ""
    End Class

    ''' <summary>
    ''' Consultas de Module01.bas y FrmEntrada.frm sobre gatestaciones, gatusuarios, gatsysusuarios,
    ''' gatlocales y gatparametros. Son las mismas tablas que usa VB6, así que los dos sistemas ven
    ''' las mismas sesiones y la misma bitácora.
    ''' </summary>
    Public NotInheritable Class DatosEntradaMySql
        Implements IDatosEntrada

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Async Function ConectaAsync(ct As CancellationToken) As Task(Of Boolean) Implements IDatosEntrada.ConectaAsync
            Try
                Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                    Return True
                End Using
            Catch ex As MySqlException
                Return False
            End Try
        End Function

        ''' <summary>Locales que FrmEntrada ofrece al usuario.</summary>
        Public Async Function ListarLocalesAsync(login As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of LocalEntrada))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim sql As String
                Dim uid As Long
                If String.Equals(login, "administrador", StringComparison.OrdinalIgnoreCase) Then
                    sql = "SELECT DISTINCT A.Local, A.Descripcion FROM gatlocales A " &
                          "INNER JOIN gatmoduloslocal B ON A.Local = B.Local WHERE A.activo = '1' ORDER BY A.Local;"
                Else
                    uid = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition("SELECT UID FROM gatusuarios WHERE usuario = @login;", New With {login}, cancellationToken:=ct)).ConfigureAwait(False)
                    sql = "SELECT L.Local, L.Descripcion FROM gatlocales L " &
                          "INNER JOIN gatusuariospriv2 P ON P.uid = MD5(CONCAT(CAST(L.Local AS CHAR), ',', @uid)) " &
                          "WHERE L.Activo = '1' GROUP BY L.Local;"
                End If
                Dim filas = Await cn.QueryAsync(Of LocalEntrada)(New CommandDefinition(sql, New With {uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        ''' <summary>
        ''' Parte de InicializarConfiguracion. La estación se identifica por el nombre del equipo; si
        ''' no está registrada, se reutiliza el IDEstacion de sdconfig.ini o se crea uno nuevo.
        ''' </summary>
        Public Async Function PrepararEstacionAsync(equipo As String, guardada As String, ip As String, ct As CancellationToken) As Task(Of EstacionEntrada) Implements IDatosEntrada.PrepararEstacionAsync
            Const consulta As String = "SELECT Estacion AS Id, Descripcion AS Nombre, Activo = '1' AS Activa FROM gatestaciones WHERE Descripcion = @equipo LIMIT 1;"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim estacion = Await cn.QueryFirstOrDefaultAsync(Of EstacionEntrada)(New CommandDefinition(consulta, New With {equipo}, cancellationToken:=ct)).ConfigureAwait(False)
                If estacion IsNot Nothing Then Return estacion

                Dim id = If(guardada?.Length = 32, guardada, Md5Nuevo())
                Dim renombrada = Await cn.ExecuteAsync(New CommandDefinition("UPDATE gatestaciones SET Descripcion = @equipo, IP = @ip WHERE Estacion = @id;", New With {equipo, ip, id}, cancellationToken:=ct)).ConfigureAwait(False)
                If renombrada = 0 Then
                    Const alta As String =
                        "INSERT INTO gatestaciones (Estacion, Descripcion, Activo, IP, Ruta, ActReg, Apagado, UFI, UIDC, FC) " &
                        "VALUES (@id, @nombre, '1', @ip, '', '0', '', NOW(), 0, NOW());"
                    Await cn.ExecuteAsync(New CommandDefinition(alta, New With {id, .nombre = equipo.ToUpperInvariant(), ip}, cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Return Await cn.QueryFirstOrDefaultAsync(Of EstacionEntrada)(New CommandDefinition(consulta, New With {equipo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Private Shared Function Md5Nuevo() As String
            Return Md5Vb6.Calcular(Guid.NewGuid().ToString("B").ToUpperInvariant())
        End Function

        Public Async Function ObtenerUidAsync(login As String, ct As CancellationToken) As Task(Of Long) Implements IDatosEntrada.ObtenerUidAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim uid = Await cn.ExecuteScalarAsync(Of Long?)(New CommandDefinition("SELECT Uid FROM gatusuarios WHERE usuario = @login;", New With {login}, cancellationToken:=ct)).ConfigureAwait(False)
                Return If(uid, -1L)
            End Using
        End Function

        ''' <summary>Bloquear a un usuario en SDACA es quitarle select_priv en mysql.user.</summary>
        Public Async Function UsuarioActivoAsync(login As String, ct As CancellationToken) As Task(Of Boolean) Implements IDatosEntrada.UsuarioActivoAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sql As String = "SELECT COUNT(user) FROM mysql.user WHERE user = @login AND select_priv = 'Y';"
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(sql, New With {login}, cancellationToken:=ct)).ConfigureAwait(False) > 0
            End Using
        End Function

        Public Async Function EstacionDeSesionAsync(local As Long, uid As Long, ct As CancellationToken) As Task(Of String) Implements IDatosEntrada.EstacionDeSesionAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sql As String = "SELECT Estacion FROM gatsysusuarios WHERE Local = @local AND UID = @uid;"
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {local, uid}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        Public Async Function BorrarSesionesViejasAsync(local As Long, ct As CancellationToken) As Task Implements IDatosEntrada.BorrarSesionesViejasAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sql As String = "DELETE FROM gatsysusuarios WHERE Local = @local AND DATE_SUB(NOW(), INTERVAL 1 DAY) > FI;"
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function DetalleSesionAsync(uid As Long, ct As CancellationToken) As Task(Of String) Implements IDatosEntrada.DetalleSesionAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sql As String =
                    "SELECT CONCAT(' ', E.Descripcion, ' [IP:', E.IP, ']') FROM gatsysusuarios U " &
                    "INNER JOIN gatestaciones E ON E.Estacion = U.Estacion WHERE U.UID = @uid;"
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {uid}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        Public Async Function ObtenerUsuarioAsync(login As String, ct As CancellationToken) As Task(Of DatosUsuario) Implements IDatosEntrada.ObtenerUsuarioAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sql As String =
                    "SELECT A.UID AS Uid, B.nombre AS Nombre, CONCAT(B.apellido1, ' ', B.apellido2) AS Apellidos, B.genero AS Genero " &
                    "FROM gatusuarios A, gacdp B WHERE B.id = A.ID AND A.Usuario = @login AND Fdef = '1900-01-01';"
                Dim usuario = Await cn.QueryFirstOrDefaultAsync(Of DatosUsuario)(New CommandDefinition(sql, New With {login}, cancellationToken:=ct)).ConfigureAwait(False)
                If usuario Is Nothing AndAlso Privilegios.EsAdministrador(login) Then
                    Return New DatosUsuario With {.Uid = 0, .Nombre = "Administrador", .Genero = "0"}
                End If
                Return usuario
            End Using
        End Function

        Public Async Function ObtenerHorarioAsync(login As String, columnaDia As String, ct As CancellationToken) As Task(Of HorarioUsuario) Implements IDatosEntrada.ObtenerHorarioAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                ' columnaDia sale de PoliticaHorario.ColumnaDia: D1 a D7, nunca de la pantalla.
                Dim sql = $"SELECT FDesde AS Desde, FHasta AS Hasta, {columnaDia} AS Mascara, CCPI AS Ccpi FROM gatusuarios WHERE usuario = @login;"
                Return Await cn.QueryFirstOrDefaultAsync(Of HorarioUsuario)(New CommandDefinition(sql, New With {login}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function AbrirSesionAsync(local As Long, estacionId As String, uid As Long, equipo As String, ct As CancellationToken) As Task Implements IDatosEntrada.AbrirSesionAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sesion As String = "REPLACE INTO gatsysusuarios (Local, Estacion, UID, FI, FE) VALUES (@local, @estacionId, @uid, NOW(), NULL);"
                Await cn.ExecuteAsync(New CommandDefinition(sesion, New With {local, estacionId, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("UPDATE gatestaciones SET UFI = NOW() WHERE Descripcion = @equipo;", New With {equipo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function ReabrirSesionAsync(local As Long, estacionId As String, uid As Long, ct As CancellationToken) As Task Implements IDatosEntrada.ReabrirSesionAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sql As String = "UPDATE gatsysusuarios SET FE = NULL WHERE Local = @local AND Estacion = @estacionId AND UID = @uid;"
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {local, estacionId, uid}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function CerrarSesionAsync(local As Long, estacionId As String, uid As Long, ct As CancellationToken) As Task Implements IDatosEntrada.CerrarSesionAsync
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sql As String = "DELETE FROM gatsysusuarios WHERE Local = @local AND Estacion = @estacionId AND UID = @uid;"
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {local, estacionId, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("UPDATE gatestaciones SET Apagado = '' WHERE Estacion = @estacionId;", New With {estacionId}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>GATDAE y los parámetros del local que faltan, copiados del local 1000 (SD_INITParametros).</summary>
        Public Async Function PrepararParametrosAsync(local As Long, uid As Long, ct As CancellationToken) As Task Implements IDatosEntrada.PrepararParametrosAsync
            Dim parametros As New Parametros(_fabrica)
            If String.IsNullOrEmpty(Await parametros.ObtenerAsync(local, "GATDAE", ct).ConfigureAwait(False)) Then
                Await parametros.GuardarAsync(local, "GATDAE", "0", uid, ct).ConfigureAwait(False)
            End If
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim existentes = (Await cn.QueryAsync(Of String)(New CommandDefinition("SELECT Parametro FROM gatparametros WHERE Local = @local;", New With {local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                If existentes.Count = 0 Then existentes.Add("")
                Const sql As String =
                    "INSERT INTO gatparametros (SELECT @local AS 'Local', Parametro, Valor, @uid AS 'UIDC', NULL " &
                    "FROM gatparametros WHERE Parametro NOT IN @existentes AND Local = 1000);"
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {local, uid, existentes}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Function CargarPrivilegiosAsync(login As String, local As Long, uid As Long, cedula As String, ct As CancellationToken) As Task(Of ISet(Of String)) Implements IDatosEntrada.CargarPrivilegiosAsync
            Return New Privilegios(_fabrica).CargarAsync(login, local, local, uid, cedula, ct:=ct)
        End Function

        Public Function AnotarAsync(local As Long, uid As Long, tipo As TipoBitacora, estacionId As String, detalle As String, detalleSql As String, ct As CancellationToken) As Task Implements IDatosEntrada.AnotarAsync
            Return New Bitacora(_fabrica).AgregarAsync(local, 0, uid, tipo, estacionId, detalle, detalleSql, ct)
        End Function

    End Class

End Namespace
