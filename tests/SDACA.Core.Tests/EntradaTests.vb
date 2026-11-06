Imports System.Threading
Imports SDACA.Core.Datos
Imports SDACA.Core.Licencias
Imports SDACA.Core.Seguridad

Public Class EntradaTests

    Private Const Ahora As String = "2026-11-04 10:30:00" ' miércoles

    Private ReadOnly _datos As New DatosFalsos()

    Private Function Entrar(login As String, Optional confirmar As Boolean = False) As RespuestaEntrada
        Dim solicitud As New SolicitudEntrada With {
            .Login = login, .Local = 1, .Equipo = "PC1", .Ip = "1.1.1.1", .Ahora = Date.Parse(Ahora),
            .ConfirmarOtraEstacion = Function(detalle) confirmar
        }
        Return New ServicioEntrada(_datos, New LicenciaCoopepalmares()).EntrarAsync(solicitud).GetAwaiter().GetResult()
    End Function

    <Fact>
    Public Sub Usuario_valido_entra_abre_sesion_y_anota_en_bitacora()
        Dim r = Entrar("ana")
        Assert.Equal(ResultadoEntrada.Correcto, r.Resultado)
        Assert.Equal(25, r.Sesion.Uid)
        Assert.Equal("EST1", r.Sesion.EstacionId)
        Assert.Contains("abrir", _datos.Hechos)
        Assert.Contains(TipoBitacora.Suceso, _datos.Tipos)
    End Sub

    <Fact>
    Public Sub Sin_conexion_no_consulta_nada_mas()
        _datos.Conecta = False
        Assert.Equal(ResultadoEntrada.SinConexion, Entrar("ana").Resultado)
        Assert.Empty(_datos.Hechos)
    End Sub

    <Fact>
    Public Sub Estacion_deshabilitada_no_entra()
        _datos.EstacionActiva = False
        Assert.Equal(ResultadoEntrada.EstacionInactiva, Entrar("ana").Resultado)
    End Sub

    <Fact>
    Public Sub Usuario_inexistente_queda_en_bitacora_como_error()
        _datos.Uid = -1
        Assert.Equal(ResultadoEntrada.UsuarioNoExiste, Entrar("ana").Resultado)
        Assert.Contains(TipoBitacora.Error, _datos.Tipos)
    End Sub

    <Fact>
    Public Sub Usuario_bloqueado_no_entra()
        _datos.Activo = False
        Assert.Equal(ResultadoEntrada.UsuarioInactivo, Entrar("ana").Resultado)
    End Sub

    <Fact>
    Public Sub Administrador_no_se_valida_como_usuario_activo_ni_por_horario()
        _datos.Activo = False
        _datos.Horario = Nothing
        Assert.Equal(ResultadoEntrada.Correcto, Entrar("Administrador").Resultado)
    End Sub

    <Fact>
    Public Sub Root_si_pasa_por_horario_como_en_vb6()
        _datos.Horario = Nothing
        Assert.Equal(ResultadoEntrada.FueraDeHorario, Entrar("root").Resultado)
    End Sub

    <Fact>
    Public Sub Fuera_de_horario_no_deja_sesion_abierta()
        _datos.Horario.Mascara = 1L << 9 ' solo de 9:00 a 9:59
        Assert.Equal(ResultadoEntrada.FueraDeHorario, Entrar("ana").Resultado)
        Assert.DoesNotContain("abrir", _datos.Hechos)
    End Sub

    <Fact>
    Public Sub Sesion_en_otra_estacion_bloquea_a_un_usuario_normal()
        _datos.EstacionSesion = "EST2"
        _datos.Detalle = " PC2 [IP:2.2.2.2]"
        Dim r = Entrar("ana")
        Assert.Equal(ResultadoEntrada.SesionEnOtraEstacion, r.Resultado)
        Assert.Equal(" PC2 [IP:2.2.2.2]", r.Detalle)
        Assert.DoesNotContain("abrir", _datos.Hechos)
    End Sub

    <Fact>
    Public Sub Sesion_sin_cerrar_en_la_misma_estacion_se_reabre()
        _datos.EstacionSesion = "EST1"
        _datos.Detalle = " PC1 [IP:1.1.1.1]"
        Assert.Equal(ResultadoEntrada.Correcto, Entrar("ana").Resultado)
        Assert.Contains("reabrir", _datos.Hechos)
        Assert.DoesNotContain("abrir", _datos.Hechos)
    End Sub

    <Fact>
    Public Sub Administrador_con_sesion_en_otra_estacion_decide_si_entra()
        _datos.EstacionSesion = "EST2"
        _datos.Detalle = " PC2 [IP:2.2.2.2]"
        Assert.Equal(ResultadoEntrada.Cancelado, Entrar("Administrador", confirmar:=False).Resultado)
        Assert.Equal(ResultadoEntrada.Correcto, Entrar("Administrador", confirmar:=True).Resultado)
    End Sub

    <Fact>
    Public Sub Si_ya_no_hay_detalle_la_sesion_vieja_no_estorba()
        _datos.EstacionSesion = "EST2"
        _datos.Detalle = ""
        Assert.Equal(ResultadoEntrada.Correcto, Entrar("ana").Resultado)
        Assert.Contains("borrarViejas", _datos.Hechos)
    End Sub

    <Fact>
    Public Sub Salir_cierra_la_sesion_y_anota_en_bitacora()
        Dim sesion As New Sesion With {.Login = "ana", .Uid = 25, .Local = 1, .EstacionId = "EST1", .Estacion = "PC1"}
        Dim servicio As New ServicioEntrada(_datos, New LicenciaCoopepalmares())
        servicio.SalirAsync(sesion).GetAwaiter().GetResult()
        Assert.Contains("cerrar", _datos.Hechos)
        Assert.Contains(TipoBitacora.Suceso, _datos.Tipos)
    End Sub

    <Theory>
    <InlineData(0, 0, False)>
    <InlineData(1, 0, True)>
    <InlineData(1, 1, False)>
    <InlineData(8388608, 23, True)>
    <InlineData(16777215, 12, True)>
    <InlineData(5, 1, False)>
    <InlineData(5, 2, True)>
    Public Sub Hora_permitida_lee_la_mascara_como_sd_horario(mascara As Long, hora As Integer, esperado As Boolean)
        Assert.Equal(esperado, PoliticaHorario.HoraPermitida(mascara, hora))
    End Sub

    <Theory>
    <InlineData("1900-01-01", "1900-01-01", True)>
    <InlineData("2026-01-01", "2026-12-31", True)>
    <InlineData("2026-11-05", "2026-12-31", False)>
    <InlineData("2026-01-01", "2026-11-03", False)>
    <InlineData("2026-01-01", "1900-01-01", False)>
    Public Sub Vigencia_compara_contra_hoy(desde As String, hasta As String, esperado As Boolean)
        Assert.Equal(esperado, PoliticaHorario.VigenciaPermite(Date.Parse(desde), Date.Parse(hasta), Date.Parse(Ahora)))
    End Sub

    <Fact>
    Public Sub Columna_del_dia_empieza_en_domingo()
        Assert.Equal("D1", PoliticaHorario.ColumnaDia(New Date(2026, 11, 1)))
        Assert.Equal("D7", PoliticaHorario.ColumnaDia(New Date(2026, 11, 7)))
    End Sub

    Private NotInheritable Class DatosFalsos
        Implements IDatosEntrada

        Public Conecta As Boolean = True
        Public EstacionActiva As Boolean = True
        Public Uid As Long = 25
        Public Activo As Boolean = True
        Public EstacionSesion As String = ""
        Public Detalle As String = ""
        Public Horario As New HorarioUsuario With {.Desde = New Date(1900, 1, 1), .Hasta = New Date(1900, 1, 1), .Mascara = 16777215}
        Public ReadOnly Hechos As New List(Of String)
        Public ReadOnly Tipos As New List(Of TipoBitacora)

        Private Function Hecho(texto As String) As Task
            Hechos.Add(texto)
            Return Task.CompletedTask
        End Function

        Public Function ConectaAsync(ct As CancellationToken) As Task(Of Boolean) Implements IDatosEntrada.ConectaAsync
            Return Task.FromResult(Conecta)
        End Function
        Public Function PrepararEstacionAsync(equipo As String, guardada As String, ip As String, ct As CancellationToken) As Task(Of EstacionEntrada) Implements IDatosEntrada.PrepararEstacionAsync
            Return Task.FromResult(New EstacionEntrada With {.Id = "EST1", .Nombre = equipo, .Activa = EstacionActiva})
        End Function
        Public Function ObtenerUidAsync(login As String, ct As CancellationToken) As Task(Of Long) Implements IDatosEntrada.ObtenerUidAsync
            Return Task.FromResult(Uid)
        End Function
        Public Function UsuarioActivoAsync(login As String, ct As CancellationToken) As Task(Of Boolean) Implements IDatosEntrada.UsuarioActivoAsync
            Return Task.FromResult(Activo)
        End Function
        Public Function EstacionDeSesionAsync(local As Long, uid As Long, ct As CancellationToken) As Task(Of String) Implements IDatosEntrada.EstacionDeSesionAsync
            Return Task.FromResult(EstacionSesion)
        End Function
        Public Function BorrarSesionesViejasAsync(local As Long, ct As CancellationToken) As Task Implements IDatosEntrada.BorrarSesionesViejasAsync
            Return Hecho("borrarViejas")
        End Function
        Public Function DetalleSesionAsync(uid As Long, ct As CancellationToken) As Task(Of String) Implements IDatosEntrada.DetalleSesionAsync
            Return Task.FromResult(Detalle)
        End Function
        Public Function ObtenerUsuarioAsync(login As String, ct As CancellationToken) As Task(Of DatosUsuario) Implements IDatosEntrada.ObtenerUsuarioAsync
            Return Task.FromResult(New DatosUsuario With {.Uid = Uid, .Nombre = "Ana"})
        End Function
        Public Function ObtenerHorarioAsync(login As String, columnaDia As String, ct As CancellationToken) As Task(Of HorarioUsuario) Implements IDatosEntrada.ObtenerHorarioAsync
            Return Task.FromResult(Horario)
        End Function
        Public Function AbrirSesionAsync(local As Long, estacionId As String, uid As Long, equipo As String, ct As CancellationToken) As Task Implements IDatosEntrada.AbrirSesionAsync
            Return Hecho("abrir")
        End Function
        Public Function ReabrirSesionAsync(local As Long, estacionId As String, uid As Long, ct As CancellationToken) As Task Implements IDatosEntrada.ReabrirSesionAsync
            Return Hecho("reabrir")
        End Function
        Public Function CerrarSesionAsync(local As Long, estacionId As String, uid As Long, ct As CancellationToken) As Task Implements IDatosEntrada.CerrarSesionAsync
            Return Hecho("cerrar")
        End Function
        Public Function PrepararParametrosAsync(local As Long, uid As Long, ct As CancellationToken) As Task Implements IDatosEntrada.PrepararParametrosAsync
            Return Hecho("parametros")
        End Function
        Public Function CargarPrivilegiosAsync(login As String, local As Long, uid As Long, cedula As String, ct As CancellationToken) As Task(Of ISet(Of String)) Implements IDatosEntrada.CargarPrivilegiosAsync
            Return Task.FromResult(Of ISet(Of String))(New HashSet(Of String)())
        End Function
        Public Function AnotarAsync(local As Long, uid As Long, tipo As TipoBitacora, estacionId As String, detalle As String, detalleSql As String, ct As CancellationToken) As Task Implements IDatosEntrada.AnotarAsync
            Tipos.Add(tipo)
            Return Task.CompletedTask
        End Function
    End Class

End Class
