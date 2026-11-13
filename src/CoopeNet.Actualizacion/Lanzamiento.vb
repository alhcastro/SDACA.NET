Imports System.IO

''' <summary>Qué arrancar: la carpeta, su versión y lo que pasó para llegar ahí.</summary>
Public NotInheritable Class Eleccion
    Public Property Carpeta As String = ""
    Public Property Version As String = ""
    Public Property Ejecutable As String = "CoopeNet.exe"
    ''' <summary>Para la tabla equipos y el registro del lanzador.</summary>
    Public Property Resultado As String = ""
    ''' <summary>Si no se pudo actualizar: se arranca lo que había y se avisa.</summary>
    Public Property Fallo As Exception

    Public ReadOnly Property Ruta As String
        Get
            Return Path.Combine(Carpeta, Ejecutable)
        End Get
    End Property
End Class

''' <summary>
''' La decisión del lanzador. El servidor manda: se arranca la versión que <see cref="Canales.Elegir"/> escoge
''' para el canal del equipo, aunque sea más vieja que la instalada (así se devuelve una versión con fallas).
''' Sin servidor, o si algo falla, se arranca la última que se usó; y si no hay ninguna, la instalada.
''' </summary>
Public NotInheritable Class Lanzamiento

    Private ReadOnly _instalacion As InstalacionLocal
    Private ReadOnly _base As String
    Private ReadOnly _versionBase As String

    ''' <param name="carpetaBase">La carpeta del instalador (Program Files\CoopeNet), con su CoopeNet.exe.</param>
    ''' <param name="versionBase">La versión de ese CoopeNet.exe.</param>
    Public Sub New(instalacion As InstalacionLocal, carpetaBase As String, versionBase As String)
        _instalacion = instalacion
        _base = carpetaBase
        _versionBase = versionBase
    End Sub

    Public Shared Function Iguales(a As String, b As String) As Boolean
        Dim va As System.Version = Nothing, vb As System.Version = Nothing
        If Not System.Version.TryParse(If(a, ""), va) OrElse Not System.Version.TryParse(If(b, ""), vb) Then Return False
        Return Completa(va) = Completa(vb)
    End Function

    ''' <summary>1.0.1 y 1.0.1.0 son la misma versión; System.Version no lo cree así.</summary>
    Private Shared Function Completa(v As System.Version) As System.Version
        Return New System.Version(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0))
    End Function

    Private Function Instalada(resultado As String, Optional fallo As Exception = Nothing) As Eleccion
        Return New Eleccion With {.Carpeta = _base, .Version = _versionBase, .Resultado = resultado, .Fallo = fallo}
    End Function

    ''' <summary>La que se usó la última vez, o la instalada.</summary>
    Public Function SinServidor(resultado As String, Optional fallo As Exception = Nothing) As Eleccion
        Dim ultima = _instalacion.Actual()
        If ultima Is Nothing Then Return Instalada(resultado, fallo)
        Return New Eleccion With {.Carpeta = _instalacion.Carpeta(ultima), .Version = ultima, .Resultado = resultado, .Fallo = fallo}
    End Function

    Public Async Function ResolverAsync(origen As IOrigenVersiones, canal As String, clavePublica As String,
                                        progreso As IProgress(Of Avance), ct As Threading.CancellationToken) As Task(Of Eleccion)
        Dim anterior = _instalacion.Actual()
        Dim objetivo As VersionPublicada
        Try
            objetivo = Canales.Elegir(Await origen.VersionesAsync(ct).ConfigureAwait(False), canal)
        Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
            Return SinServidor("sin conexión con el servidor de actualizaciones", ex)
        End Try

        If objetivo Is Nothing OrElse Iguales(objetivo.Version, _versionBase) Then
            ' Nada publicado (o todo retirado), o lo publicado es lo instalado: se usa la instalación.
            _instalacion.Olvidar()
            _instalacion.Limpiar({anterior})
            Return Instalada(If(objetivo Is Nothing, "sin versiones publicadas: la instalada", "al día con la instalada"))
        End If

        Try
            Dim resultado As String
            If _instalacion.EstaCompleta(objetivo.Version) Then
                resultado = If(String.Equals(objetivo.Version, anterior, StringComparison.Ordinal), "al día", $"cambió de {If(anterior, _versionBase)} a {objetivo.Version} (ya estaba bajada)")
            Else
                Dim completa = Await origen.ObtenerAsync(objetivo.Version, ct).ConfigureAwait(False)
                If completa Is Nothing Then Throw New InvalidDataException($"La versión {objetivo.Version} desapareció del servidor.")
                Dim semillas = {If(anterior Is Nothing, Nothing, _instalacion.Carpeta(anterior)), _base}
                Await _instalacion.PrepararAsync(completa, clavePublica, origen, semillas, progreso, ct).ConfigureAwait(False)
                resultado = $"actualizado de {If(anterior, _versionBase)} a {objetivo.Version}"
            End If
            _instalacion.Activar(objetivo.Version)
            ' Se conserva la anterior para volver sin bajar nada si se retira la nueva.
            _instalacion.Limpiar({objetivo.Version, anterior})
            Return New Eleccion With {.Carpeta = _instalacion.Carpeta(objetivo.Version), .Version = objetivo.Version, .Resultado = resultado}
        Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
            Return SinServidor($"no se pudo instalar {objetivo.Version}: {ex.Message}", ex)
        End Try
    End Function

End Class
