Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

Public Enum EstiloBoton
    Normal
    Primario
    Peligro
End Enum

''' <summary>
''' Apariencia común del ERP. <see cref="Aplicar"/> se llama al abrir cada pantalla, así que un
''' formulario nuevo no necesita código de diseño: basta con seguir los nombres de botones que
''' reconoce <see cref="EstiloDe"/>. Solo cambia colores y bordes, nunca la posición de los campos.
''' No fija ForeColor en los hijos: FrmVerCheque pinta de rojo un cheque anulado por herencia.
''' </summary>
Public NotInheritable Class Tema

    Private Sub New()
    End Sub

    Public Shared ReadOnly Marino As Color = ColorTranslator.FromHtml("#102A43")
    Public Shared ReadOnly MarinoClaro As Color = ColorTranslator.FromHtml("#1E3F5F")
    Public Shared ReadOnly TextoSobreMarino As Color = ColorTranslator.FromHtml("#B6C6D6")
    Public Shared ReadOnly Acento As Color = ColorTranslator.FromHtml("#0E7490")
    Public Shared ReadOnly AcentoOscuro As Color = ColorTranslator.FromHtml("#0B5C73")
    Public Shared ReadOnly AcentoSuave As Color = ColorTranslator.FromHtml("#E3F1F5")
    Public Shared ReadOnly Peligro As Color = ColorTranslator.FromHtml("#B42318")
    Public Shared ReadOnly PeligroSuave As Color = ColorTranslator.FromHtml("#FDECEA")
    Public Shared ReadOnly Fondo As Color = ColorTranslator.FromHtml("#F2F4F7")
    Public Shared ReadOnly Superficie As Color = Color.White
    Public Shared ReadOnly CampoLectura As Color = ColorTranslator.FromHtml("#F5F7FA")
    Public Shared ReadOnly Borde As Color = ColorTranslator.FromHtml("#D5DCE4")
    Public Shared ReadOnly BordeFuerte As Color = ColorTranslator.FromHtml("#B8C3CF")
    Public Shared ReadOnly Texto As Color = ColorTranslator.FromHtml("#1F2933")
    Public Shared ReadOnly TextoTenue As Color = ColorTranslator.FromHtml("#52667A")

    ''' <summary>Nombre que ve el usuario. Los proyectos, la carpeta de instalación y los .ini siguen siendo SDACA.</summary>
    Public Const Marca As String = "CoopeNet"
    Public Const Producto As String = Marca & " ERP"
    Public Const Desarrollador As String = "Desarrollo Integral CP"

    Public Const FuenteBase As String = "Segoe UI"
    Public Const FuenteSemibold As String = "Segoe UI Semibold"
    Public Const FuenteIconos As String = "Segoe MDL2 Assets"

    ''' <summary>La vista de FrmConfigCheque representa el papel impreso: no se le aplica el tema.</summary>
    Private Const ControlExcluido As String = "vista"

    Private Const AlturaMinimaTarjeta As Integer = 12

    Private Shared ReadOnly TituloGrupo As New Font(FuenteSemibold, 9.0F)

    ''' <summary>Viste una pantalla completa y le agrega el encabezado.</summary>
    ''' <param name="modulo">Nombre del módulo, que se muestra sobre el título. Opcional.</param>
    Public Shared Sub Aplicar(formulario As Form, Optional modulo As String = "")
        Escalar(formulario)
        formulario.BackColor = Fondo
        formulario.ForeColor = Texto
        formulario.ShowIcon = False
        AplicarControles(formulario)
        AgregarEncabezado(formulario, modulo)
        ColorearVentana(formulario, ocultarTitulo:=True)
    End Sub

    ''' <summary>
    ''' Las pantallas están medidas a 96 DPI y no declaran AutoScaleDimensions, así que a 125 % crecía
    ''' la letra pero no los controles. Las fuentes ya vienen escaladas: aquí solo cambian los tamaños.
    ''' </summary>
    Private Shared Sub Escalar(formulario As Form)
        Dim factor = formulario.DeviceDpi / 96.0F
        If factor = 1.0F Then Return
        formulario.AutoScaleMode = AutoScaleMode.None
        Dim minimo = formulario.MinimumSize
        formulario.Scale(New SizeF(factor, factor))
        formulario.MinimumSize = New Size(CInt(minimo.Width * factor), CInt(minimo.Height * factor))
    End Sub

    ''' <summary>Viste un control y todo lo que contiene, sin encabezado.</summary>
    Public Shared Sub AplicarControles(raiz As Control)
        For Each c In raiz.Controls.Cast(Of Control)()
            If c.Name = ControlExcluido OrElse TypeOf c Is BarraComandos Then Continue For
            Vestir(c)
            AplicarControles(c)
        Next
    End Sub

    Private Shared Sub Vestir(c As Control)
        ' Webdings y Consolas cumplen una función; solo se reemplaza la Arial heredada de VB6.
        If c.Font.Name = "Arial" Then c.Font = New Font(FuenteBase, c.Font.Size, c.Font.Style)

        Select Case True
            Case TypeOf c Is Button
                Dim boton = CType(c, Button)
                Dim estilo = EstiloDe(boton)
                PintarBoton(boton, estilo)
                AddHandler boton.EnabledChanged, Sub() PintarBoton(boton, estilo)
            Case TypeOf c Is GroupBox
                VestirGrupo(CType(c, GroupBox))
            Case TypeOf c Is TabPage
                c.BackColor = Superficie
            Case TypeOf c Is TextBox
                If CType(c, TextBox).ReadOnly Then c.BackColor = CampoLectura
            Case TypeOf c Is DataGridView
                VestirRejilla(CType(c, DataGridView))
            Case TypeOf c Is Label
                Dim etiqueta = CType(c, Label)
                If etiqueta.BorderStyle <> BorderStyle.None Then VestirEtiquetaConBorde(etiqueta)
        End Select
    End Sub

    Private Shared Function EstiloDe(boton As Button) As EstiloBoton
        Select Case boton.Name.ToLowerInvariant()
            Case "btnanular", "btnborrar"
                Return EstiloBoton.Peligro
            Case "btnguardar", "btnimprimir", "btnentregar", "btnaplicar", "btnentrar", "btncerrarmes"
                Return EstiloBoton.Primario
        End Select
        Dim formulario = boton.FindForm()
        If formulario IsNot Nothing AndAlso formulario.AcceptButton Is boton Then Return EstiloBoton.Primario
        Return EstiloBoton.Normal
    End Function

    ''' <summary>Se vuelve a llamar cuando cambia Enabled, porque un botón plano no se atenúa solo.</summary>
    Public Shared Sub PintarBoton(boton As Button, estilo As EstiloBoton)
        Dim fondoBoton = Superficie
        Dim textoBoton = Texto
        Dim bordeBoton = BordeFuerte
        Dim encima = Fondo
        Dim presionado = Borde
        If estilo = EstiloBoton.Primario Then
            fondoBoton = Acento
            textoBoton = Color.White
            bordeBoton = Acento
            encima = AcentoOscuro
            presionado = AcentoOscuro
        ElseIf estilo = EstiloBoton.Peligro Then
            textoBoton = Peligro
            bordeBoton = Peligro
            encima = PeligroSuave
            presionado = PeligroSuave
        End If
        If Not boton.Enabled Then
            fondoBoton = CampoLectura
            bordeBoton = Borde
        End If
        boton.FlatStyle = FlatStyle.Flat
        boton.UseVisualStyleBackColor = False
        boton.BackColor = fondoBoton
        boton.ForeColor = textoBoton
        boton.FlatAppearance.BorderSize = 1
        boton.FlatAppearance.BorderColor = bordeBoton
        boton.FlatAppearance.MouseOverBackColor = encima
        boton.FlatAppearance.MouseDownBackColor = presionado
    End Sub

    Private Shared Sub VestirGrupo(grupo As GroupBox)
        ' FrmConfigCheque usa un GroupBox de 2 píxeles como línea divisoria.
        If grupo.Height < AlturaMinimaTarjeta Then Return
        grupo.BackColor = Superficie
        AddHandler grupo.Paint, AddressOf Grupo_Paint
    End Sub

    Private Shared Sub Grupo_Paint(sender As Object, e As PaintEventArgs)
        Dim grupo = CType(sender, GroupBox)
        e.Graphics.Clear(If(grupo.Parent Is Nothing, Fondo, grupo.Parent.BackColor))
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Using ruta = Redondeado(New Rectangle(0, 0, grupo.Width - 1, grupo.Height - 1), 6),
              relleno As New SolidBrush(Superficie), lapiz As New Pen(Borde)
            e.Graphics.FillPath(relleno, ruta)
            e.Graphics.DrawPath(lapiz, ruta)
        End Using
        If grupo.Text.Length > 0 Then
            TextRenderer.DrawText(e.Graphics, grupo.Text, TituloGrupo, New Point(grupo.LogicalToDeviceUnits(9), grupo.LogicalToDeviceUnits(3)), grupo.ForeColor, TextFormatFlags.NoPrefix)
        End If
    End Sub

    Private Shared Sub VestirEtiquetaConBorde(etiqueta As Label)
        etiqueta.BorderStyle = BorderStyle.None
        etiqueta.BackColor = CampoLectura
        etiqueta.Padding = New Padding(3)
        AddHandler etiqueta.Paint, Sub(s, e)
                                       Using lapiz As New Pen(Borde)
                                           e.Graphics.DrawRectangle(lapiz, 0, 0, etiqueta.Width - 1, etiqueta.Height - 1)
                                       End Using
                                   End Sub
    End Sub

    Private Shared Sub VestirRejilla(rejilla As DataGridView)
        rejilla.BackgroundColor = Superficie
        rejilla.BorderStyle = BorderStyle.None
        rejilla.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        rejilla.GridColor = Borde
        rejilla.EnableHeadersVisualStyles = False
        rejilla.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        rejilla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        rejilla.ColumnHeadersHeight = rejilla.LogicalToDeviceUnits(32)
        rejilla.ColumnHeadersDefaultCellStyle.BackColor = Fondo
        rejilla.ColumnHeadersDefaultCellStyle.ForeColor = TextoTenue
        rejilla.ColumnHeadersDefaultCellStyle.SelectionBackColor = Fondo
        rejilla.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextoTenue
        rejilla.ColumnHeadersDefaultCellStyle.Font = TituloGrupo
        rejilla.DefaultCellStyle.BackColor = Superficie
        rejilla.DefaultCellStyle.ForeColor = Texto
        rejilla.DefaultCellStyle.SelectionBackColor = AcentoSuave
        rejilla.DefaultCellStyle.SelectionForeColor = Texto
        rejilla.RowTemplate.Height = rejilla.LogicalToDeviceUnits(26)
        For Each columna In rejilla.Columns.Cast(Of DataGridViewColumn)()
            If columna.AutoSizeMode <> DataGridViewAutoSizeColumnMode.Fill Then columna.Width = rejilla.LogicalToDeviceUnits(columna.Width)
        Next
    End Sub

    Private Shared Sub AgregarEncabezado(formulario As Form, modulo As String)
        ' El cuerpo nace del tamaño del área actual, para que los anclajes de cada control no se
        ' recalculen al crecer la ventana.
        Dim cuerpo As New Panel With {.Size = formulario.ClientSize, .Location = Point.Empty}
        Dim hijos = formulario.Controls.Cast(Of Control)().ToArray()
        cuerpo.Controls.AddRange(hijos)
        Dim encabezado As New EncabezadoVentana(formulario, modulo)
        Dim alto = encabezado.Height
        If formulario.MinimumSize.Height > 0 Then
            formulario.MinimumSize = New Size(formulario.MinimumSize.Width, formulario.MinimumSize.Height + alto)
        End If
        formulario.ClientSize = New Size(formulario.ClientSize.Width, formulario.ClientSize.Height + alto)
        cuerpo.Dock = DockStyle.Fill
        formulario.Controls.Add(cuerpo)
        formulario.Controls.Add(encabezado)
    End Sub

    ''' <summary>Tiñe la barra de título. Windows 10 ignora estos atributos y deja la barra normal.</summary>
    ''' <param name="ocultarTitulo">Pinta el texto del color de la barra, si el encabezado ya lo muestra.</param>
    Public Shared Sub ColorearVentana(formulario As Form, Optional ocultarTitulo As Boolean = False)
        AddHandler formulario.HandleCreated, Sub() TeñirBarra(formulario.Handle, ocultarTitulo)
        If formulario.IsHandleCreated Then TeñirBarra(formulario.Handle, ocultarTitulo)
    End Sub

    Private Shared Sub TeñirBarra(ventana As IntPtr, ocultarTitulo As Boolean)
        Const BordeVentana = 34
        Const ColorBarra = 35
        Const ColorTexto = 36
        Dim marinoNativo = ColorTranslator.ToWin32(Marino)
        Dim colorTitulo = ColorTranslator.ToWin32(If(ocultarTitulo, Marino, Color.White))
        DwmSetWindowAttribute(ventana, BordeVentana, marinoNativo, 4)
        DwmSetWindowAttribute(ventana, ColorBarra, marinoNativo, 4)
        DwmSetWindowAttribute(ventana, ColorTexto, colorTitulo, 4)
    End Sub

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(ventana As IntPtr, atributo As Integer, ByRef valor As Integer, tamano As Integer) As Integer
    End Function

    Public Shared Function Redondeado(area As Rectangle, radio As Integer) As GraphicsPath
        Dim d = radio * 2
        Dim ruta As New GraphicsPath()
        ruta.AddArc(area.Left, area.Top, d, d, 180, 90)
        ruta.AddArc(area.Right - d, area.Top, d, d, 270, 90)
        ruta.AddArc(area.Right - d, area.Bottom - d, d, d, 0, 90)
        ruta.AddArc(area.Left, area.Bottom - d, d, d, 90, 90)
        ruta.CloseFigure()
        Return ruta
    End Function

End Class

''' <summary>Banda superior con el módulo y el título de la ventana.</summary>
Friend NotInheritable Class EncabezadoVentana
    Inherits Panel

    Private Shared ReadOnly FuenteTitulo As New Font(Tema.FuenteSemibold, 13.0F)
    Private Shared ReadOnly FuenteModulo As New Font(Tema.FuenteBase, 8.0F)

    Private ReadOnly _ventana As Form
    Private ReadOnly _modulo As String

    Public Sub New(ventana As Form, modulo As String)
        _ventana = ventana
        _modulo = If(modulo, "").ToUpperInvariant()
        DoubleBuffered = True
        Dock = DockStyle.Top
        Height = LogicalToDeviceUnits(60)
        BackColor = Tema.Marino
        AddHandler ventana.TextChanged, Sub() Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim banderas = TextFormatFlags.NoPrefix Or TextFormatFlags.EndEllipsis Or TextFormatFlags.SingleLine
        Dim franja = LogicalToDeviceUnits(3)
        Dim izquierda = LogicalToDeviceUnits(16)
        Dim ancho = ClientSize.Width - izquierda * 2
        If _modulo.Length > 0 Then
            TextRenderer.DrawText(e.Graphics, _modulo, FuenteModulo, New Rectangle(izquierda, LogicalToDeviceUnits(9), ancho, LogicalToDeviceUnits(16)), Tema.TextoSobreMarino, banderas)
            TextRenderer.DrawText(e.Graphics, _ventana.Text, FuenteTitulo, New Rectangle(izquierda - 1, LogicalToDeviceUnits(24), ancho, LogicalToDeviceUnits(28)), Color.White, banderas)
        Else
            TextRenderer.DrawText(e.Graphics, _ventana.Text, FuenteTitulo, New Rectangle(izquierda - 1, 0, ancho, ClientSize.Height - franja),
                                  Color.White, banderas Or TextFormatFlags.VerticalCenter)
        End If
        Using relleno As New SolidBrush(Tema.Acento)
            e.Graphics.FillRectangle(relleno, 0, ClientSize.Height - franja, ClientSize.Width, franja)
        End Using
    End Sub

End Class
