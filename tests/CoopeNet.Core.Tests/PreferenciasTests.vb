Imports System.IO
Imports CoopeNet.Core.Configuracion

Public Class PreferenciasTests
    Implements IDisposable

    Private ReadOnly _carpeta As String = Path.Combine(Path.GetTempPath(), "coopenet-pref-" & Guid.NewGuid().ToString("N"))

    Private ReadOnly Property Ruta As String
        Get
            Return Path.Combine(_carpeta, "sub", "preferencias.ini")
        End Get
    End Property

    Public Sub Dispose() Implements IDisposable.Dispose
        If Directory.Exists(_carpeta) Then Directory.Delete(_carpeta, True)
    End Sub

    <Fact>
    Public Sub Sin_archivo_no_hay_nada_recordado()
        Dim p = Preferencias.Cargar(Ruta)
        Assert.False(p.MenuMaximizado)
        Assert.Null(p.LocalDe("servidor|3306|sdprincipal|ana"))
    End Sub

    <Fact>
    Public Sub Guarda_y_recupera_ventana_y_local_creando_la_carpeta()
        Dim p = Preferencias.Cargar(Ruta)
        p.MenuMaximizado = True
        p.RecordarLocal("servidor|3306|sdprincipal|ana", 5)
        p.RecordarLocal("servidor|3306|sdprincipal|luis", 2)
        p.Guardar()

        Dim leidas = Preferencias.Cargar(Ruta)
        Assert.True(leidas.MenuMaximizado)
        Assert.Equal(5L, leidas.LocalDe("servidor|3306|sdprincipal|ana"))
        Assert.Equal(2L, leidas.LocalDe("servidor|3306|sdprincipal|luis"))
    End Sub

    <Fact>
    Public Sub Guarda_y_recupera_la_ultima_conexion()
        Dim p = Preferencias.Cargar(Ruta)
        Assert.Equal("", p.UltimoPerfil)
        Assert.Equal("", p.UltimoUsuario)
        p.UltimoPerfil = "CP DB=PRUEBAS"
        p.UltimoUsuario = "Niño"
        p.RecordarLocal("servidor|3306|sdprincipal|ana", 5)
        p.Guardar()

        Dim leidas = Preferencias.Cargar(Ruta)
        Assert.Equal("CP DB=PRUEBAS", leidas.UltimoPerfil)
        Assert.Equal("Niño", leidas.UltimoUsuario)
        Assert.Equal(5L, leidas.LocalDe("servidor|3306|sdprincipal|ana"))
    End Sub

    <Fact>
    Public Sub El_usuario_no_distingue_mayusculas()
        Dim p = Preferencias.Cargar(Ruta)
        p.RecordarLocal("servidor|3306|sdprincipal|Ana", 5)
        Assert.Equal(5L, p.LocalDe("SERVIDOR|3306|sdprincipal|ana"))
    End Sub

    <Fact>
    Public Sub Ignora_lineas_danadas_y_conserva_las_buenas()
        Directory.CreateDirectory(Path.GetDirectoryName(Ruta))
        File.WriteAllLines(Ruta, {"basura", "=1", "LOCAL:a|1|b|c=no-es-numero", "LOCAL:a=b|1|c|d=7", "MENU.MAXIMIZADO=1"})
        Dim p = Preferencias.Cargar(Ruta)
        Assert.True(p.MenuMaximizado)
        Assert.Null(p.LocalDe("a|1|b|c"))
        Assert.Equal(7L, p.LocalDe("a=b|1|c|d"))
    End Sub

    <Fact>
    Public Sub Si_no_se_puede_escribir_no_falla()
        Directory.CreateDirectory(_carpeta)
        Dim ocupada = Path.Combine(_carpeta, "archivo")
        File.WriteAllText(ocupada, "")
        ' La "carpeta" del archivo es en realidad un archivo.
        Preferencias.Cargar(Path.Combine(ocupada, "preferencias.ini")).Guardar()
    End Sub

End Class
