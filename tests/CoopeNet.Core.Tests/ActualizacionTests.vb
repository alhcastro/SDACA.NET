Imports System.IO
Imports System.Text
Imports System.Threading
Imports CoopeNet.Actualizacion

Public Class ActualizacionTests
    Implements IDisposable

    Private ReadOnly _raiz As String = Path.Combine(Path.GetTempPath(), "coopenet-act-" & Guid.NewGuid().ToString("N"))
    Private Shared ReadOnly Claves As (Privada As String, Publica As String) = Firma.CrearClaves()

    ''' <summary>Un servidor en memoria: versiones firmadas y archivos por hash, y cuántos se pidieron.</summary>
    Private NotInheritable Class OrigenMemoria
        Implements IOrigenVersiones
        Public ReadOnly Versiones As New List(Of VersionPublicada)()
        Public ReadOnly Archivos As New Dictionary(Of String, Byte())()
        Public Pedidos As Integer
        Public Caido As Boolean

        Public Function VersionesAsync(ct As CancellationToken) As Task(Of IReadOnlyList(Of VersionPublicada)) Implements IOrigenVersiones.VersionesAsync
            If Caido Then Throw New IOException("sin red")
            Return Task.FromResult(Of IReadOnlyList(Of VersionPublicada))(Versiones.ToList())
        End Function

        Public Function ObtenerAsync(version As String, ct As CancellationToken) As Task(Of VersionPublicada) Implements IOrigenVersiones.ObtenerAsync
            Return Task.FromResult(Versiones.FirstOrDefault(Function(v) v.Version = version))
        End Function

        Public Function LeerArchivoAsync(hash As String, ct As CancellationToken) As Task(Of Byte()) Implements IOrigenVersiones.LeerArchivoAsync
            Pedidos += 1
            Return Task.FromResult(Archivos(hash))
        End Function
    End Class

    Private Function Carpeta(nombre As String, ParamArray contenido As (Ruta As String, Texto As String)()) As String
        Dim ruta = Path.Combine(_raiz, nombre)
        For Each c In contenido
            Dim destino = Path.Combine(ruta, c.Ruta)
            Directory.CreateDirectory(Path.GetDirectoryName(destino))
            File.WriteAllText(destino, c.Texto)
        Next
        Return ruta
    End Function

    ''' <summary>Publica una carpeta en el origen en memoria, como lo haría herramientas\Publicar.</summary>
    Private Shared Function Publicar(origen As OrigenMemoria, carpetaVersion As String, numero As String, Optional canal As String = Canales.General, Optional privada As String = Nothing) As VersionPublicada
        Dim m = Manifiesto.DeCarpeta(carpetaVersion, numero)
        For Each a In m.Archivos
            origen.Archivos(a.Hash) = File.ReadAllBytes(Path.Combine(carpetaVersion, a.Ruta))
        Next
        Dim texto = m.ATexto()
        Dim v As New VersionPublicada With {.Version = numero, .Canal = canal, .Activa = True, .Manifiesto = texto, .Firma = Firma.Firmar(texto, If(privada, Claves.Privada))}
        origen.Versiones.Add(v)
        Return v
    End Function

    Private Function Preparar() As (Origen As OrigenMemoria, Instalacion As InstalacionLocal, Lanz As Lanzamiento, Base As String)
        Dim base = Carpeta("base", ("CoopeNet.exe", "erp 1.0.1"), ("CoopeNet.Core.dll", "core 1"), ("runtime/System.dll", "runtime"))
        Dim instalacion As New InstalacionLocal(Path.Combine(_raiz, "versiones"))
        Return (New OrigenMemoria(), instalacion, New Lanzamiento(instalacion, base, "1.0.1"), base)
    End Function

    Private Shared Function Resolver(l As Lanzamiento, origen As IOrigenVersiones, Optional canal As String = Canales.General) As Eleccion
        Return l.ResolverAsync(origen, canal, Claves.Publica, Nothing, CancellationToken.None).GetAwaiter().GetResult()
    End Function

    <Fact>
    Public Sub Solo_baja_lo_que_cambio_y_arranca_la_nueva()
        Dim p = Preparar()
        Dim nueva = Carpeta("pub102", ("CoopeNet.exe", "erp 1.0.2"), ("CoopeNet.Core.dll", "core 1"), ("runtime/System.dll", "runtime"))
        Publicar(p.Origen, nueva, "1.0.2")

        Dim e = Resolver(p.Lanz, p.Origen)

        Assert.Null(e.Fallo)
        Assert.Equal("1.0.2", e.Version)
        Assert.Equal(1, p.Origen.Pedidos) ' solo CoopeNet.exe; el resto salió de la instalada
        Assert.Equal("erp 1.0.2", File.ReadAllText(e.Ruta))
        Assert.Equal("runtime", File.ReadAllText(Path.Combine(e.Carpeta, "runtime", "System.dll")))
        Assert.Equal("1.0.2", p.Instalacion.Actual())

        ' La segunda vez ya está: no baja nada.
        Dim otra = Resolver(p.Lanz, p.Origen)
        Assert.Equal("al día", otra.Resultado)
        Assert.Equal(1, p.Origen.Pedidos)
    End Sub

    <Fact>
    Public Sub Sin_servidor_arranca_la_ultima_que_uso_y_si_no_hay_la_instalada()
        Dim p = Preparar()
        p.Origen.Caido = True
        Dim e = Resolver(p.Lanz, p.Origen)
        Assert.Equal(p.Base, e.Carpeta)
        Assert.NotNull(e.Fallo)

        p.Origen.Caido = False
        Publicar(p.Origen, Carpeta("pub102", ("CoopeNet.exe", "erp 1.0.2")), "1.0.2")
        Resolver(p.Lanz, p.Origen)
        p.Origen.Caido = True
        Assert.Equal("1.0.2", Resolver(p.Lanz, p.Origen).Version)
    End Sub

    <Fact>
    Public Sub Retirar_la_nueva_vuelve_a_la_anterior_sin_bajar_nada()
        Dim p = Preparar()
        Publicar(p.Origen, Carpeta("pub102", ("CoopeNet.exe", "erp 1.0.2")), "1.0.2")
        Dim v103 = Publicar(p.Origen, Carpeta("pub103", ("CoopeNet.exe", "erp 1.0.3")), "1.0.3")
        p.Origen.Versiones.Remove(v103)
        Resolver(p.Lanz, p.Origen)
        p.Origen.Versiones.Add(v103)
        Assert.Equal("1.0.3", Resolver(p.Lanz, p.Origen).Version)
        Dim pedidos = p.Origen.Pedidos

        v103.Activa = False
        Dim e = Resolver(p.Lanz, p.Origen)
        Assert.Equal("1.0.2", e.Version)
        Assert.Equal(pedidos, p.Origen.Pedidos)
    End Sub

    <Fact>
    Public Sub Retirar_todo_vuelve_a_la_instalada()
        Dim p = Preparar()
        Dim v = Publicar(p.Origen, Carpeta("pub102", ("CoopeNet.exe", "erp 1.0.2")), "1.0.2")
        Resolver(p.Lanz, p.Origen)
        v.Activa = False
        Dim e = Resolver(p.Lanz, p.Origen)
        Assert.Equal(p.Base, e.Carpeta)
        Assert.Null(p.Instalacion.Actual())
    End Sub

    <Fact>
    Public Sub El_canal_piloto_recibe_antes()
        Dim p = Preparar()
        Publicar(p.Origen, Carpeta("pub102", ("CoopeNet.exe", "erp 1.0.2")), "1.0.2", Canales.Piloto)
        Assert.Equal(p.Base, Resolver(p.Lanz, p.Origen, Canales.General).Carpeta)
        Assert.Equal("1.0.2", Resolver(p.Lanz, p.Origen, Canales.Piloto).Version)
    End Sub

    <Fact>
    Public Sub Una_version_firmada_con_otra_clave_no_se_instala()
        Dim p = Preparar()
        Publicar(p.Origen, Carpeta("pub102", ("CoopeNet.exe", "erp 1.0.2")), "1.0.2", privada:=Firma.CrearClaves().Privada)
        Dim e = Resolver(p.Lanz, p.Origen)
        Assert.Equal(p.Base, e.Carpeta)
        Assert.StartsWith("no se pudo instalar 1.0.2", e.Resultado)
        Assert.Equal(0, p.Origen.Pedidos)
        Assert.False(Directory.Exists(p.Instalacion.Carpeta("1.0.2")))
    End Sub

    <Fact>
    Public Sub Un_manifiesto_alterado_despues_de_firmar_no_se_instala()
        Dim p = Preparar()
        Dim v = Publicar(p.Origen, Carpeta("pub102", ("CoopeNet.exe", "erp 1.0.2")), "1.0.2")
        v.Manifiesto = v.Manifiesto.Replace("CoopeNet.exe", "CoopeNet.ex_")
        Assert.Equal(p.Base, Resolver(p.Lanz, p.Origen).Carpeta)
    End Sub

    <Fact>
    Public Sub Un_archivo_danado_no_deja_la_version_completa()
        Dim p = Preparar()
        Dim v = Publicar(p.Origen, Carpeta("pub102", ("CoopeNet.exe", "erp 1.0.2")), "1.0.2")
        Dim hash = Manifiesto.DesdeTexto(v.Manifiesto).Archivos(0).Hash
        p.Origen.Archivos(hash) = Encoding.UTF8.GetBytes("erp 1.0.X")
        Dim e = Resolver(p.Lanz, p.Origen)
        Assert.Equal(p.Base, e.Carpeta)
        Assert.False(p.Instalacion.EstaCompleta("1.0.2"))
    End Sub

    <Theory>
    <InlineData("../fuera.dll")>
    <InlineData("sub/../../fuera.dll")>
    <InlineData("C:/Windows/fuera.dll")>
    <InlineData("sub\fuera.dll")>
    Public Sub Un_manifiesto_no_puede_escribir_fuera_de_su_carpeta(ruta As String)
        Dim m As New Manifiesto With {.Version = "1.0.2"}
        m.Archivos.Add(New ArchivoManifiesto With {.Ruta = ruta, .Hash = New String("a"c, 64), .Tamano = 1})
        Assert.Throws(Of InvalidDataException)(Function() Manifiesto.DesdeTexto(m.ATexto()))
    End Sub

    <Fact>
    Public Sub El_manifiesto_deja_fuera_simbolos_y_documentacion()
        Dim c = Carpeta("pub", ("CoopeNet.exe", "x"), ("CoopeNet.pdb", "x"), ("CoopeNet.Core.xml", "x"), ("es/CoopeNet.resources.dll", "x"))
        Dim rutas = Manifiesto.DeCarpeta(c, "1.0.2").Archivos.Select(Function(a) a.Ruta).ToList()
        Assert.Equal({"CoopeNet.exe", "es/CoopeNet.resources.dll"}, rutas)
    End Sub

    <Theory>
    <InlineData("1.0.1", "1.0.1.0", True)>
    <InlineData("1.0.1", "1.0.2", False)>
    <InlineData("1.0.10", "1.0.9", False)>
    Public Sub Versiones_iguales(a As String, b As String, esperado As Boolean)
        Assert.Equal(esperado, Lanzamiento.Iguales(a, b))
    End Sub

    <Fact>
    Public Sub Elige_por_numero_y_no_por_texto()
        Dim lista = {"1.0.9", "1.0.10", "1.0.2"}.Select(Function(n) New VersionPublicada With {.Version = n, .Activa = True}).ToList()
        Assert.Equal("1.0.10", Canales.Elegir(lista, Canales.General).Version)
    End Sub

    <Fact>
    Public Sub La_configuracion_del_equipo_tiene_valores_predeterminados()
        Dim ruta = Path.Combine(_raiz, "actualizacion.ini")
        Directory.CreateDirectory(_raiz)
        Assert.Equal(3312UI, ConfiguracionEquipo.Cargar(ruta).Puerto)
        File.WriteAllText(ruta, "[ACTUALIZACION]" & vbCrLf & "CANAL=PILOTO" & vbCrLf & "PUERTO=3315" & vbCrLf & "ACTIVA=0")
        Dim c = ConfiguracionEquipo.Cargar(ruta)
        Assert.Equal(Canales.Piloto, c.Canal)
        Assert.Equal(3315UI, c.Puerto)
        Assert.False(c.Activa)
        Assert.Equal(ConfiguracionEquipo.ServidorPredeterminado, c.Servidor)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            Directory.Delete(_raiz, recursive:=True)
        Catch ex As IOException
        End Try
    End Sub

End Class
