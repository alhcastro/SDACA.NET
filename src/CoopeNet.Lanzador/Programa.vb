Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports CoopeNet.Actualizacion

''' <summary>
''' Arranque de CoopeNet con actualización. Pregunta al servidor qué versión le toca a este equipo, la baja si
''' falta (solo los archivos que cambiaron) y abre CoopeNet.exe desde su carpeta. Sin servidor abre la última que
''' tenga, sin demora: una sucursal sin internet sigue trabajando.
''' </summary>
Public Module Programa

    ''' <summary>Si en este tiempo no se decidió, se muestra la ventana de avance.</summary>
    Private Const EsperaVentana As Integer = 1200

    Private ReadOnly RutaRegistro As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CoopeNet", "lanzador.log")

    <STAThread>
    Public Sub Main(argumentos As String())
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        Dim base = AppContext.BaseDirectory
        Dim versionBase = VersionDe(Path.Combine(base, "CoopeNet.exe"))
        Dim config = ConfiguracionEquipo.Cargar(ConfiguracionEquipo.RutaPredeterminada)
        Dim lanzamiento As New Lanzamiento(New InstalacionLocal(InstalacionLocal.Predeterminada), base, versionBase)

        Dim eleccion As Eleccion
        Using unico As New Mutex(True, "CoopeNet.Lanzador", Nothing)
            If Not unico.WaitOne(0, False) Then Return ' Doble clic repetido: ya hay uno trabajando.
            If ErpAbierto() Then
                ' No se cambian archivos con CoopeNet abierto; CoopeNet mismo avisa que ya está en ejecución.
                eleccion = lanzamiento.SinServidor("CoopeNet ya estaba abierto")
            ElseIf Not config.Activa Then
                eleccion = lanzamiento.SinServidor("actualizaciones desactivadas en actualizacion.ini")
            Else
                eleccion = Resolver(lanzamiento, config)
            End If
        End Using

        Registrar($"{eleccion.Version}: {eleccion.Resultado}" & If(eleccion.Fallo Is Nothing, "", $" [{eleccion.Fallo.GetType().Name}: {eleccion.Fallo.Message}]"))
        If eleccion.Fallo IsNot Nothing AndAlso eleccion.Resultado.StartsWith("no se pudo", StringComparison.Ordinal) Then
            MessageBox.Show($"No se pudo instalar la versión nueva de CoopeNet. Se abre la versión {eleccion.Version}." & vbCrLf & vbCrLf &
                            eleccion.Fallo.Message, "CoopeNet ERP", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If

        If Not File.Exists(eleccion.Ruta) Then
            Registrar($"No existe {eleccion.Ruta}; se abre la instalada.")
            eleccion = New Eleccion With {.Carpeta = base, .Version = versionBase}
        End If
        Try
            Dim inicio As New ProcessStartInfo(eleccion.Ruta) With {.WorkingDirectory = eleccion.Carpeta, .UseShellExecute = False}
            For Each a In argumentos
                inicio.ArgumentList.Add(a)
            Next
            Process.Start(inicio)?.Dispose()
        Catch ex As Exception
            Registrar($"No se pudo abrir {eleccion.Ruta}: {ex.Message}")
            MessageBox.Show($"No se pudo abrir CoopeNet: {ex.Message}", "CoopeNet ERP", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>Consulta y, si hace falta, baja. La ventana sale solo si tarda.</summary>
    Private Function Resolver(lanzamiento As Lanzamiento, config As ConfiguracionEquipo) As Eleccion
        Dim almacen As New AlmacenVersiones(config.Servidor, config.Puerto, esperaSegundos:=5UI)
        Using ventana As New FrmActualizando()
            Dim progreso As New Progress(Of Avance)(AddressOf ventana.Mostrar)
            Dim tarea = Task.Run(Function() lanzamiento.ResolverAsync(almacen, config.Canal, ClavePublicaPublicacion.Pem, progreso, ventana.Cancelacion))
            If Not tarea.Wait(EsperaVentana) Then
                AddHandler ventana.Shown, Async Sub()
                                              Try
                                                  Await tarea
                                              Catch
                                              End Try
                                              ventana.Close()
                                          End Sub
                If Not tarea.IsCompleted Then ventana.ShowDialog()
            End If
            Dim eleccion As Eleccion
            Try
                eleccion = tarea.GetAwaiter().GetResult()
            Catch ex As OperationCanceledException
                eleccion = lanzamiento.SinServidor("actualización cancelada por el usuario")
            Catch ex As Exception
                eleccion = lanzamiento.SinServidor("error inesperado", ex)
            End Try
            Reportar(almacen, config, eleccion)
            Return eleccion
        End Using
    End Function

    ''' <summary>La tabla equipos dice qué versión tiene cada equipo. Si no responde en 3 s, se sigue igual.</summary>
    Private Sub Reportar(almacen As AlmacenVersiones, config As ConfiguracionEquipo, eleccion As Eleccion)
        Try
            Using limite As New CancellationTokenSource(3000)
                almacen.ReportarAsync(Environment.MachineName, Environment.UserName, config.Canal, eleccion.Version, eleccion.Resultado, limite.Token).Wait(3000)
            End Using
        Catch ex As Exception
        End Try
    End Sub

    Private Function ErpAbierto() As Boolean
        Dim existente As Mutex = Nothing
        If Mutex.TryOpenExisting("CoopeNet.Erp", existente) Then
            existente.Dispose()
            Return True
        End If
        Return False
    End Function

    Private Function VersionDe(ruta As String) As String
        If Not File.Exists(ruta) Then Return "0.0.0"
        Dim info = FileVersionInfo.GetVersionInfo(ruta)
        Return $"{info.FileMajorPart}.{info.FileMinorPart}.{info.FileBuildPart}"
    End Function

    ''' <summary>C:\ProgramData\CoopeNet\lanzador.log, con las últimas líneas: para saber qué pasó en un equipo.</summary>
    Private Sub Registrar(texto As String)
        Try
            Dim lineas = If(File.Exists(RutaRegistro), File.ReadAllLines(RutaRegistro).TakeLast(499).ToList(), New List(Of String)())
            lineas.Add($"{Date.Now:yyyy-MM-dd HH:mm:ss} {Environment.UserName}: {texto}")
            File.WriteAllLines(RutaRegistro, lineas)
        Catch ex As Exception
        End Try
    End Sub

End Module
