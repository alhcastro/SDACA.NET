Imports System.IO
Imports System.Security.Cryptography
Imports System.Text.Json
Imports System.Text.Json.Serialization

''' <summary>Un archivo de una versión: su ruta dentro de la carpeta, su SHA-256 y su tamaño.</summary>
Public NotInheritable Class ArchivoManifiesto
    <JsonPropertyName("ruta")> Public Property Ruta As String = ""
    <JsonPropertyName("hash")> Public Property Hash As String = ""
    <JsonPropertyName("tamano")> Public Property Tamano As Long
End Class

''' <summary>
''' Lo que compone una versión publicada. Se guarda como texto JSON y se firma ese texto tal cual: el lanzador
''' verifica la firma sobre los mismos bytes que bajó, sin volver a serializar.
''' </summary>
Public NotInheritable Class Manifiesto

    <JsonPropertyName("version")> Public Property Version As String = ""
    <JsonPropertyName("publicada")> Public Property Publicada As DateTime
    <JsonPropertyName("ejecutable")> Public Property Ejecutable As String = "CoopeNet.exe"
    <JsonPropertyName("archivos")> Public Property Archivos As List(Of ArchivoManifiesto) = New List(Of ArchivoManifiesto)()

    ''' <summary>Lo que no viaja: símbolos y documentación, igual que el instalador.</summary>
    Public Shared ReadOnly Excluidos As String() = {".pdb", ".xml"}

    Private Shared ReadOnly Opciones As New JsonSerializerOptions With {.WriteIndented = False}

    Public Function ATexto() As String
        Return JsonSerializer.Serialize(Me, Opciones)
    End Function

    Public Shared Function DesdeTexto(texto As String) As Manifiesto
        Dim m = JsonSerializer.Deserialize(Of Manifiesto)(texto, Opciones)
        If m Is Nothing OrElse m.Archivos Is Nothing Then Throw New InvalidDataException("El manifiesto está vacío.")
        For Each a In m.Archivos
            Validar(a)
        Next
        Return m
    End Function

    ''' <summary>Arma el manifiesto de una carpeta publicada.</summary>
    Public Shared Function DeCarpeta(carpeta As String, numero As String, Optional nombreEjecutable As String = "CoopeNet.exe") As Manifiesto
        Dim m As New Manifiesto With {.Version = numero, .Publicada = DateTime.UtcNow, .Ejecutable = nombreEjecutable}
        Dim raiz = Path.GetFullPath(carpeta)
        For Each ruta In Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories).OrderBy(Function(r) r, StringComparer.OrdinalIgnoreCase)
            If Excluidos.Contains(Path.GetExtension(ruta), StringComparer.OrdinalIgnoreCase) Then Continue For
            m.Archivos.Add(New ArchivoManifiesto With {
                .Ruta = Path.GetRelativePath(raiz, ruta).Replace("\"c, "/"c),
                .Hash = Huella.DeArchivo(ruta),
                .Tamano = New FileInfo(ruta).Length
            })
        Next
        If Not m.Archivos.Any(Function(a) String.Equals(a.Ruta, nombreEjecutable, StringComparison.OrdinalIgnoreCase)) Then
            Throw New FileNotFoundException($"La carpeta {carpeta} no tiene {nombreEjecutable}.")
        End If
        Return m
    End Function

    ''' <summary>
    ''' Una ruta del manifiesto no puede salir de la carpeta de la versión: un manifiesto con "..\" escribiría
    ''' en cualquier parte del equipo.
    ''' </summary>
    Private Shared Sub Validar(a As ArchivoManifiesto)
        If a Is Nothing OrElse String.IsNullOrWhiteSpace(a.Ruta) OrElse a.Tamano < 0 Then Throw New InvalidDataException("El manifiesto tiene un archivo sin datos.")
        If Not Huella.EsValida(a.Hash) Then Throw New InvalidDataException($"Hash inválido en {a.Ruta}.")
        Dim partes = a.Ruta.Split("/"c)
        If Path.IsPathRooted(a.Ruta) OrElse a.Ruta.Contains("\"c) OrElse a.Ruta.Contains(":"c) OrElse
           partes.Any(Function(p) p.Length = 0 OrElse p = "." OrElse p = "..") Then
            Throw New InvalidDataException($"Ruta no permitida en el manifiesto: {a.Ruta}")
        End If
    End Sub

End Class

''' <summary>SHA-256 en hexadecimal minúscula.</summary>
Public NotInheritable Class Huella

    Private Sub New()
    End Sub

    Public Shared Function DeArchivo(ruta As String) As String
        Using f = File.OpenRead(ruta)
            Return Convert.ToHexString(SHA256.HashData(f)).ToLowerInvariant()
        End Using
    End Function

    Public Shared Function DeBytes(datos As Byte()) As String
        Return Convert.ToHexString(SHA256.HashData(datos)).ToLowerInvariant()
    End Function

    Public Shared Function EsValida(hash As String) As Boolean
        Return hash IsNot Nothing AndAlso hash.Length = 64 AndAlso hash.All(Function(c) (c >= "0"c AndAlso c <= "9"c) OrElse (c >= "a"c AndAlso c <= "f"c))
    End Function

End Class
