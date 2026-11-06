Imports System.Threading
Imports SDACA.Core.Datos
Imports SDACA.Core.Licencias

Namespace Seguridad

    ''' <summary>Mismos códigos que devuelve <c>SD_Procesar_Entrada</c>, más los dos últimos.</summary>
    Public Enum ResultadoEntrada
        Correcto = 0
        SinConexion = 1
        UsuarioNoExiste = 2
        UsuarioInactivo = 3
        DatosInconsistentes = 5
        FueraDeHorario = 6
        ErrorConfiguracion = 7
        SesionEnOtraEstacion = 8
        Cancelado = 9
        EstacionInactiva = 10
    End Enum

    Public NotInheritable Class SolicitudEntrada
        Public Property Login As String = ""
        Public Property Local As Long
        Public Property LocalDescripcion As String = ""
        Public Property Catalogo As String = ""
        ''' <summary>Nombre del equipo.</summary>
        Public Property Equipo As String = ""
        Public Property Ip As String = ""
        ''' <summary>IDEstacion de sdconfig.ini, si existe.</summary>
        Public Property EstacionGuardada As String = ""
        Public Property Ahora As Date = Date.Now
        ''' <summary>Avisa al usuario sin detener la entrada.</summary>
        Public Property Avisar As Action(Of String) = Sub(texto) Return
        ''' <summary>Pregunta al administrador si entra aunque tenga sesión en otra estación.</summary>
        Public Property ConfirmarOtraEstacion As Func(Of String, Boolean) = Function(detalle) False
    End Class

    Public NotInheritable Class RespuestaEntrada
        Public Property Resultado As ResultadoEntrada
        Public Property Sesion As Sesion
        ''' <summary>Estación donde el usuario ya tiene sesión.</summary>
        Public Property Detalle As String = ""
    End Class

    Public NotInheritable Class EstacionEntrada
        Public Property Id As String = ""
        Public Property Nombre As String = ""
        Public Property Activa As Boolean
    End Class

    Public NotInheritable Class DatosUsuario
        Public Property Uid As Long
        Public Property Nombre As String = ""
        Public Property Apellidos As String = ""
        Public Property Genero As String = ""
    End Class

    Public NotInheritable Class HorarioUsuario
        Public Property Desde As Date
        Public Property Hasta As Date
        Public Property Mascara As Long
        Public Property Ccpi As String = ""
    End Class

    ''' <summary>Lo que la entrada necesita de la base. Separado para probar las reglas sin servidor.</summary>
    Public Interface IDatosEntrada
        Function ConectaAsync(ct As CancellationToken) As Task(Of Boolean)
        Function PrepararEstacionAsync(equipo As String, guardada As String, ip As String, ct As CancellationToken) As Task(Of EstacionEntrada)
        Function ObtenerUidAsync(login As String, ct As CancellationToken) As Task(Of Long)
        Function UsuarioActivoAsync(login As String, ct As CancellationToken) As Task(Of Boolean)
        Function EstacionDeSesionAsync(local As Long, uid As Long, ct As CancellationToken) As Task(Of String)
        Function BorrarSesionesViejasAsync(local As Long, ct As CancellationToken) As Task
        Function DetalleSesionAsync(uid As Long, ct As CancellationToken) As Task(Of String)
        Function ObtenerUsuarioAsync(login As String, ct As CancellationToken) As Task(Of DatosUsuario)
        Function ObtenerHorarioAsync(login As String, columnaDia As String, ct As CancellationToken) As Task(Of HorarioUsuario)
        Function AbrirSesionAsync(local As Long, estacionId As String, uid As Long, equipo As String, ct As CancellationToken) As Task
        Function ReabrirSesionAsync(local As Long, estacionId As String, uid As Long, ct As CancellationToken) As Task
        Function CerrarSesionAsync(local As Long, estacionId As String, uid As Long, ct As CancellationToken) As Task
        Function PrepararParametrosAsync(local As Long, uid As Long, ct As CancellationToken) As Task
        Function CargarPrivilegiosAsync(login As String, local As Long, uid As Long, cedula As String, ct As CancellationToken) As Task(Of ISet(Of String))
        Function AnotarAsync(local As Long, uid As Long, tipo As TipoBitacora, estacionId As String, detalle As String, detalleSql As String, ct As CancellationToken) As Task
    End Interface

    ''' <summary>
    ''' Inicio y cierre de sesión. Migración de <c>SD_Procesar_Entrada</c> y <c>Form_Unload</c> de
    ''' FrmMain3. La contraseña la valida MySQL al conectar, igual que en VB6.
    ''' </summary>
    ''' <remarks>
    ''' Dos diferencias con VB6, las dos a favor de la regla:
    ''' el horario y los datos del usuario se revisan antes de registrar la sesión, para no dejar
    ''' una sesión abierta de alguien a quien se le negó la entrada; y si el administrador responde
    ''' No a entrar con sesión en otra estación, no entra (VB6 lo dejaba entrar igual).
    ''' </remarks>
    Public NotInheritable Class ServicioEntrada

        Private ReadOnly _datos As IDatosEntrada
        Private ReadOnly _licencia As ILicencia

        Public Sub New(datos As IDatosEntrada, licencia As ILicencia)
            _datos = datos
            _licencia = licencia
        End Sub

        Public Async Function EntrarAsync(s As SolicitudEntrada, Optional ct As CancellationToken = Nothing) As Task(Of RespuestaEntrada)
            If Not Await _datos.ConectaAsync(ct) Then Return Falla(ResultadoEntrada.SinConexion)

            Dim estacion = Await _datos.PrepararEstacionAsync(s.Equipo, s.EstacionGuardada, s.Ip, ct)
            If estacion Is Nothing Then
                Await _datos.AnotarAsync(s.Local, 0, TipoBitacora.Error, "", $"Error al iniciar la configuración de la estacion {s.Equipo} [{s.Ip}]", "", ct)
                Return Falla(ResultadoEntrada.ErrorConfiguracion)
            End If
            If Not estacion.Activa Then Return Falla(ResultadoEntrada.EstacionInactiva)

            Dim uid = If(Privilegios.EsAdministrador(s.Login), 0L, Await _datos.ObtenerUidAsync(s.Login, ct))
            If uid = -1 Then
                Await _datos.AnotarAsync(s.Local, uid, TipoBitacora.Error, estacion.Id, $"Error al iniciar con el usuario '{s.Login}' en la estación {estacion.Nombre}[{s.Ip}]", "", ct)
                Return Falla(ResultadoEntrada.UsuarioNoExiste)
            End If
            If uid <> 0 AndAlso Not Await _datos.UsuarioActivoAsync(s.Login, ct) Then
                Await _datos.AnotarAsync(s.Local, uid, TipoBitacora.Error, estacion.Id, $"Error al iniciar con el usuario '{s.Login}' en la estación {estacion.Nombre} [{s.Ip}] debido a que esta bloqueado.", "gatusuarios", ct)
                Return Falla(ResultadoEntrada.UsuarioInactivo)
            End If

            Dim reabrir = False
            Dim estacionSesion = Await _datos.EstacionDeSesionAsync(s.Local, uid, ct)
            If estacionSesion.Length > 0 Then
                ' Las sesiones de más de 24 horas se dan por abandonadas.
                Await _datos.BorrarSesionesViejasAsync(s.Local, ct)
                Dim detalle = Await _datos.DetalleSesionAsync(uid, ct)
                If detalle.Length > 0 Then
                    If detalle.Contains(estacion.Nombre, StringComparison.Ordinal) Then
                        s.Avisar.Invoke($"El usuario {{{s.Login}}} ya esta trabajando en esta estación.{vbCrLf}La sesión no ha sido cerrada correctamente.")
                        Await _datos.AnotarAsync(s.Local, uid, TipoBitacora.Error, estacionSesion, $"Sesión previa para el usuario ""{s.Login}"" no se ha cerrado correctamente en la estación {estacion.Nombre}", "", ct)
                        reabrir = True
                    ElseIf uid = 0 Then
                        If Not s.ConfirmarOtraEstacion(detalle) Then Return Falla(ResultadoEntrada.Cancelado)
                    Else
                        Await _datos.AnotarAsync(s.Local, uid, TipoBitacora.Suceso, estacion.Id, $"El usuario esta intentando ingresar a la estación {estacion.Nombre} [IP:{s.Ip}] pero ya tiene sesión iniciada en {detalle}", "", ct)
                        Return New RespuestaEntrada With {.Resultado = ResultadoEntrada.SesionEnOtraEstacion, .Detalle = detalle}
                    End If
                End If
            End If

            Dim usuario = Await _datos.ObtenerUsuarioAsync(s.Login, ct)
            If usuario Is Nothing Then Return Falla(ResultadoEntrada.DatosInconsistentes)

            ' Solo "Administrador" se salta el horario. root no: VB6 lo busca en gatusuarios y no está.
            If Not String.Equals(s.Login, "administrador", StringComparison.OrdinalIgnoreCase) Then
                Dim horario = Await _datos.ObtenerHorarioAsync(s.Login, PoliticaHorario.ColumnaDia(s.Ahora), ct)
                If horario Is Nothing OrElse
                   Not PoliticaHorario.VigenciaPermite(horario.Desde, horario.Hasta, s.Ahora) OrElse
                   Not PoliticaHorario.HoraPermitida(horario.Mascara, s.Ahora.Hour) Then
                    Return Falla(ResultadoEntrada.FueraDeHorario)
                End If
            End If

            If reabrir Then
                Await _datos.ReabrirSesionAsync(s.Local, estacion.Id, uid, ct)
            Else
                Await _datos.AbrirSesionAsync(s.Local, estacion.Id, uid, s.Equipo, ct)
            End If
            Await _datos.PrepararParametrosAsync(s.Local, uid, ct)
            Dim permisos = Await _datos.CargarPrivilegiosAsync(s.Login, s.Local, uid, _licencia.Registro.Cedula, ct)
            Await _datos.AnotarAsync(s.Local, uid, TipoBitacora.Suceso, estacion.Id, $"El usuario {s.Login} ha iniciado sesión en la estación {estacion.Nombre}.", "", ct)

            Return New RespuestaEntrada With {
                .Resultado = ResultadoEntrada.Correcto,
                .Sesion = New Sesion With {
                    .Login = s.Login, .Uid = uid, .Nombre = usuario.Nombre, .Apellidos = usuario.Apellidos, .Genero = usuario.Genero,
                    .Local = s.Local, .LocalDescripcion = s.LocalDescripcion, .EstacionId = estacion.Id, .Estacion = estacion.Nombre,
                    .Catalogo = s.Catalogo, .Cedula = _licencia.Registro.CedulaSinGuiones, .Empresa = _licencia.Registro.Nombre, .Permisos = permisos
                }
            }
        End Function

        Public Async Function SalirAsync(sesion As Sesion, Optional ct As CancellationToken = Nothing) As Task
            Await _datos.CerrarSesionAsync(sesion.Local, sesion.EstacionId, sesion.Uid, ct)
            Await _datos.AnotarAsync(sesion.Local, sesion.Uid, TipoBitacora.Suceso, sesion.EstacionId, $"El usuario {sesion.Login} ha cerrado la sesión en la estación {sesion.Estacion}.", "", ct)
        End Function

        Private Shared Function Falla(resultado As ResultadoEntrada) As RespuestaEntrada
            Return New RespuestaEntrada With {.Resultado = resultado}
        End Function

    End Class

End Namespace
