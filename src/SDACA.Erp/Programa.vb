Imports System.Threading
Imports SDACA.Core.Licencias
Imports SDACA.Ui

''' <summary>Arranque del ERP. Reemplaza a Sub Main de SDACAERP\Module01.bas.</summary>
Public Module Programa

    ''' <summary>Módulos migrados. El menú muestra los que estén en la licencia y el usuario pueda abrir.</summary>
    Public Function Modulos() As IReadOnlyList(Of IModulo)
        Return New IModulo() {New SD1101.Modulo(), New SD1501.Modulo()}
    End Function

    <STAThread>
    Public Sub Main(argumentos As String())
        Using unico As New Mutex(True, "SDACA.Erp", Nothing)
            If Not unico.WaitOne(0, False) Then
                MessageBox.Show("El sistema ya se encuentra en ejecución en este equipo." & vbCrLf &
                                "Por favor cierre la sesión abierta y vuelva a intentar la conexión...",
                                Tema.Producto, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Application.SetHighDpiMode(HighDpiMode.SystemAware)
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            Dim licencia As New LicenciaCoopepalmares()
            Using entrada As New FrmEntrada(licencia, DirectorioIni(argumentos))
                If entrada.ShowDialog() <> DialogResult.OK Then Return
                Dim sesion = entrada.Sesion
                Dim servicio = entrada.Servicio
                Try
                    Application.Run(New FrmMenu(sesion, Modulos(), licencia))
                Finally
                    Try
                        Task.Run(Function() servicio.SalirAsync(sesion)).Wait()
                    Catch ex As AggregateException
                        MessageBox.Show($"Error al cerrar la sesión del usuario ""{sesion.Login}"" en la estación {sesion.Estacion}.",
                                        Tema.Producto, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End Try
                End Try
            End Using
        End Using
    End Sub

    ''' <summary>Carpeta de los .ini de VB6. Se cambia con -inipath, igual que en SDACAERP.</summary>
    Private Function DirectorioIni(argumentos As String()) As String
        Dim i = Array.FindIndex(argumentos, Function(a) String.Equals(a, "-inipath", StringComparison.OrdinalIgnoreCase))
        If i >= 0 AndAlso i < argumentos.Length - 1 Then Return argumentos(i + 1)
        Return "C:\Program Files (x86)\SDACA\Bin"
    End Function

End Module
