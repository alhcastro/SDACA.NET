Imports System.IO
Imports System.Runtime.InteropServices

''' <summary>Avance de una descarga: archivos hechos, total y lo que se está haciendo.</summary>
Public Structure Avance
    Public Hechos As Integer
    Public Total As Integer
    Public Bytes As Long
    Public Texto As String
End Structure

''' <summary>
''' Las versiones bajadas en este equipo, cada una en su carpeta: <c>versiones\1.0.2\</c>. Una carpeta solo
''' cuenta si tiene la marca <see cref="Marca"/>, que se escribe al final; una descarga cortada se arma en
''' <c>1.0.2.descarga</c> y no estorba. <c>actual.txt</c> dice cuál se usa, para arrancar sin conexión.
''' </summary>
Public NotInheritable Class InstalacionLocal

    Public Const Marca As String = ".completa"
    Private Const Sufijo As String = ".descarga"
    Private Const ArchivoActual As String = "actual.txt"

    Public ReadOnly Property Raiz As String

    Public Sub New(rutaRaiz As String)
        Raiz = Path.GetFullPath(rutaRaiz)
    End Sub

    ''' <summary>C:\ProgramData\CoopeNet\versiones, junto a los .ini (CarpetaDatos en CoopeNet.Core).</summary>
    Public Shared ReadOnly Property Predeterminada As String
        Get
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CoopeNet", "versiones")
        End Get
    End Property

    Public Function Carpeta(numero As String) As String
        Return Path.Combine(Raiz, numero)
    End Function

    Public Function EstaCompleta(numero As String) As Boolean
        Return Not String.IsNullOrEmpty(numero) AndAlso File.Exists(Path.Combine(Carpeta(numero), Marca))
    End Function

    ''' <summary>La versión elegida la última vez, si su carpeta sigue completa.</summary>
    Public Function Actual() As String
        Try
            Dim texto = File.ReadAllText(Path.Combine(Raiz, ArchivoActual)).Trim()
            Return If(EstaCompleta(texto), texto, Nothing)
        Catch ex As IOException
            Return Nothing
        Catch ex As UnauthorizedAccessException
            Return Nothing
        End Try
    End Function

    Public Sub Activar(numero As String)
        Directory.CreateDirectory(Raiz)
        Dim temporal = Path.Combine(Raiz, ArchivoActual & ".tmp")
        File.WriteAllText(temporal, numero)
        File.Move(temporal, Path.Combine(Raiz, ArchivoActual), overwrite:=True)
    End Sub

    ''' <summary>Sin versión elegida: sin conexión se arranca la instalada.</summary>
    Public Sub Olvidar()
        Try
            File.Delete(Path.Combine(Raiz, ArchivoActual))
        Catch ex As IOException
        Catch ex As UnauthorizedAccessException
        End Try
    End Sub

    ''' <summary>
    ''' Deja lista la carpeta de la versión. Verifica la firma antes de escribir nada y el hash de cada archivo.
    ''' Lo que ya está igual en <paramref name="semillas"/> (la versión anterior, la instalada) se enlaza o se
    ''' copia; solo lo distinto se baja del origen.
    ''' </summary>
    Public Async Function PrepararAsync(publicada As VersionPublicada, clavePublica As String, origen As IOrigenVersiones,
                                        semillas As IEnumerable(Of String), progreso As IProgress(Of Avance),
                                        ct As Threading.CancellationToken) As Task(Of Manifiesto)
        If Not Firma.Verificar(publicada.Manifiesto, publicada.Firma, clavePublica) Then
            Throw New InvalidDataException($"La versión {publicada.Version} no tiene una firma válida. No se instala.")
        End If
        Dim m = Manifiesto.DesdeTexto(publicada.Manifiesto)
        If m.Version <> publicada.Version Then Throw New InvalidDataException($"El manifiesto dice {m.Version} y la versión es {publicada.Version}.")
        ' La versión es el nombre de la carpeta: solo números y puntos.
        If Not m.Version.All(Function(c) Char.IsAsciiDigit(c) OrElse c = "."c) Then Throw New InvalidDataException($"Versión inválida: {m.Version}")
        Canales.Interpretar(m.Version)
        If EstaCompleta(m.Version) Then Return m

        Dim final = Carpeta(m.Version)
        Dim armado = final & Sufijo
        If Directory.Exists(armado) Then Directory.Delete(armado, recursive:=True)
        Directory.CreateDirectory(armado)

        Dim origenes = semillas.Where(Function(s) Not String.IsNullOrEmpty(s) AndAlso Directory.Exists(s)).ToList()
        Dim hechos = 0
        Dim bajados = 0L
        For Each a In m.Archivos
            ct.ThrowIfCancellationRequested()
            Dim destino = Path.Combine(armado, a.Ruta.Replace("/"c, Path.DirectorySeparatorChar))
            Directory.CreateDirectory(Path.GetDirectoryName(destino))
            If Not Reusar(a, origenes, destino) Then
                progreso?.Report(New Avance With {.Hechos = hechos, .Total = m.Archivos.Count, .Bytes = bajados, .Texto = $"Descargando {a.Ruta}"})
                Dim datos = Await origen.LeerArchivoAsync(a.Hash, ct).ConfigureAwait(False)
                If datos.LongLength <> a.Tamano OrElse Huella.DeBytes(datos) <> a.Hash Then
                    Throw New InvalidDataException($"{a.Ruta} llegó dañado.")
                End If
                Await File.WriteAllBytesAsync(destino, datos, ct).ConfigureAwait(False)
                bajados += datos.LongLength
            End If
            hechos += 1
            progreso?.Report(New Avance With {.Hechos = hechos, .Total = m.Archivos.Count, .Bytes = bajados, .Texto = $"Preparando la versión {m.Version}"})
        Next
        File.WriteAllText(Path.Combine(armado, Marca), m.Version)

        If Directory.Exists(final) Then Directory.Delete(final, recursive:=True)
        Directory.Move(armado, final)
        Return m
    End Function

    ''' <summary>
    ''' Busca el archivo idéntico, por ruta, en las carpetas semilla. Solo se enlaza lo que está en otra versión
    ''' bajada, que nadie modifica; lo de la instalada se copia, porque el instalador podría reescribir ese archivo
    ''' en su lugar y con un enlace cambiaría también aquí.
    ''' </summary>
    Private Function Reusar(a As ArchivoManifiesto, origenes As List(Of String), destino As String) As Boolean
        For Each carpetaOrigen In origenes
            Dim candidato = Path.Combine(carpetaOrigen, a.Ruta.Replace("/"c, Path.DirectorySeparatorChar))
            Try
                If Not File.Exists(candidato) OrElse New FileInfo(candidato).Length <> a.Tamano Then Continue For
                If Huella.DeArchivo(candidato) <> a.Hash Then Continue For
                Dim propia = Path.GetFullPath(carpetaOrigen).StartsWith(Raiz & Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                If Not (propia AndAlso Enlazar(candidato, destino)) Then File.Copy(candidato, destino, overwrite:=True)
                Return True
            Catch ex As IOException
            Catch ex As UnauthorizedAccessException
            End Try
        Next
        Return False
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True, EntryPoint:="CreateHardLinkW")>
    Private Shared Function CreateHardLink(nuevo As String, existente As String, reservado As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    ''' <summary>Un enlace duro no ocupa espacio. Si Windows no lo permite (otro disco, permisos), se copia.</summary>
    Private Shared Function Enlazar(existente As String, nuevo As String) As Boolean
        If Not OperatingSystem.IsWindows() Then Return False
        Return CreateHardLink(nuevo, existente, IntPtr.Zero)
    End Function

    ''' <summary>Borra las versiones que no están en <paramref name="conservar"/>. Lo que esté en uso se queda.</summary>
    Public Function Limpiar(conservar As IEnumerable(Of String)) As Integer
        If Not Directory.Exists(Raiz) Then Return 0
        Dim quedan = New HashSet(Of String)(conservar.Where(Function(c) c IsNot Nothing), StringComparer.OrdinalIgnoreCase)
        Dim borradas = 0
        For Each carpetaVersion In Directory.GetDirectories(Raiz)
            Dim nombre = Path.GetFileName(carpetaVersion)
            If quedan.Contains(nombre) Then Continue For
            Try
                ' Primero se renombra: si CoopeNet corre desde ahí, Windows no deja y la carpeta queda entera.
                ' Borrar directo podría dejarla a medias pero con la marca de completa.
                Dim aparte = carpetaVersion
                If Not nombre.EndsWith(".borrar", StringComparison.OrdinalIgnoreCase) Then
                    aparte = carpetaVersion & "." & Guid.NewGuid().ToString("N").Substring(0, 8) & ".borrar"
                    Directory.Move(carpetaVersion, aparte)
                End If
                Directory.Delete(aparte, recursive:=True)
                borradas += 1
            Catch ex As IOException
            Catch ex As UnauthorizedAccessException
            End Try
        Next
        Return borradas
    End Function

    ''' <summary>Las versiones completas en este equipo, de la más nueva a la más vieja.</summary>
    Public Function Completas() As IReadOnlyList(Of String)
        If Not Directory.Exists(Raiz) Then Return Array.Empty(Of String)()
        Return Directory.GetDirectories(Raiz).Select(Function(d) Path.GetFileName(d)).
            Where(Function(n) EstaCompleta(n) AndAlso System.Version.TryParse(n, Nothing)).
            OrderByDescending(Function(n) System.Version.Parse(n)).ToList()
    End Function

End Class
