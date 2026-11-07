Imports System.Drawing
Imports CoopeNet.Core.Configuracion

''' <summary>
''' Pantallas con listas que se pueden maximizar o agrandar y que recuerdan su último tamaño y si quedaron
''' maximizadas (en ventanas.ini). La posición no se recuerda: siguen abriendo sobre la pantalla que las llama.
''' </summary>
Public Module Ventanas

    ''' <summary>Archivo donde se guarda; las pruebas lo cambian.</summary>
    Public Property Ruta As String = EstadosVentana.RutaPredeterminada

    ''' <summary>
    ''' Hace la ventana redimensionable, con su tamaño de diseño como mínimo, y la restaura al abrir. Llamarlo en el
    ''' constructor, con los controles ya anclados y antes de <see cref="Tema.Aplicar"/>, que escala el mínimo.
    ''' </summary>
    ''' <param name="clave">Nombre con que se recuerda; por omisión, el nombre completo de la clase.</param>
    Public Sub Recordar(formulario As Form, Optional clave As String = Nothing)
        Dim nombre = If(String.IsNullOrWhiteSpace(clave), formulario.GetType().FullName, clave)
        formulario.FormBorderStyle = FormBorderStyle.Sizable
        formulario.MaximizeBox = True
        formulario.MinimumSize = formulario.Size
        AddHandler formulario.Load, Sub() Restaurar(formulario, nombre)
        AddHandler formulario.FormClosed, Sub() Guardar(formulario, nombre)
    End Sub

    Private Function Factor(formulario As Form) As Single
        Return formulario.DeviceDpi / 96.0F
    End Function

    Private Sub Restaurar(formulario As Form, nombre As String)
        Dim estado = EstadosVentana.Cargar(Ruta).Obtener(nombre)
        If Not estado.HasValue Then Return
        Dim f = Factor(formulario)
        Dim area = Screen.FromControl(formulario).WorkingArea
        Dim ancho = Math.Min(Math.Max(CInt(estado.Value.Ancho * f), formulario.MinimumSize.Width), area.Width)
        Dim alto = Math.Min(Math.Max(CInt(estado.Value.Alto * f), formulario.MinimumSize.Height), area.Height)
        Dim centro = New Point(formulario.Left + formulario.Width \ 2, formulario.Top + formulario.Height \ 2)
        formulario.Size = New Size(ancho, alto)
        ' CenterParent se calcula antes de Load con el tamaño de diseño: se vuelve a centrar con el nuevo.
        If formulario.StartPosition = FormStartPosition.CenterParent OrElse formulario.StartPosition = FormStartPosition.CenterScreen Then
            Dim x = Math.Max(area.Left, Math.Min(centro.X - ancho \ 2, area.Right - ancho))
            Dim y = Math.Max(area.Top, Math.Min(centro.Y - alto \ 2, area.Bottom - alto))
            formulario.StartPosition = FormStartPosition.Manual
            formulario.Location = New Point(x, y)
        End If
        If estado.Value.Maximizada Then formulario.WindowState = FormWindowState.Maximized
    End Sub

    Private Sub Guardar(formulario As Form, nombre As String)
        Dim maximizada = formulario.WindowState = FormWindowState.Maximized
        Dim medida = If(formulario.WindowState = FormWindowState.Normal, formulario.Size, formulario.RestoreBounds.Size)
        Dim f = Factor(formulario)
        Dim estados = EstadosVentana.Cargar(Ruta)
        estados.Recordar(nombre, New EstadoVentana With {.Ancho = CInt(medida.Width / f), .Alto = CInt(medida.Height / f), .Maximizada = maximizada})
        estados.Guardar()
    End Sub

End Module
