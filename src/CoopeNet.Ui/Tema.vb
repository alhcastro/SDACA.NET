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

    Private Shared _paleta As Paleta = Paleta.Clara
    Private Shared _preferencia As PreferenciaTema = PreferenciaTema.Claro

    ''' <summary>
    ''' Se dispara al cambiar de modo, con la paleta anterior, para que las ventanas abiertas se vuelvan a pintar
    ''' (<see cref="Recolorear"/>). Las que se abran después ya nacen con la nueva.
    ''' </summary>
    Public Shared Event Cambio(anterior As Paleta)

    ''' <summary>True si se está usando la paleta oscura.</summary>
    Public Shared ReadOnly Property Oscuro As Boolean
        Get
            Return _paleta Is Paleta.Oscura
        End Get
    End Property

    Public Shared ReadOnly Property Preferencia As PreferenciaTema
        Get
            Return _preferencia
        End Get
    End Property

    Public Shared ReadOnly Property Marino As Color
        Get
            Return _paleta.Marino
        End Get
    End Property
    Public Shared ReadOnly Property MarinoClaro As Color
        Get
            Return _paleta.MarinoClaro
        End Get
    End Property
    Public Shared ReadOnly Property TextoSobreMarino As Color
        Get
            Return _paleta.TextoSobreMarino
        End Get
    End Property
    Public Shared ReadOnly Property Acento As Color
        Get
            Return _paleta.Acento
        End Get
    End Property
    Public Shared ReadOnly Property AcentoOscuro As Color
        Get
            Return _paleta.AcentoOscuro
        End Get
    End Property
    Public Shared ReadOnly Property AcentoSuave As Color
        Get
            Return _paleta.AcentoSuave
        End Get
    End Property
    Public Shared ReadOnly Property Peligro As Color
        Get
            Return _paleta.Peligro
        End Get
    End Property
    Public Shared ReadOnly Property PeligroSuave As Color
        Get
            Return _paleta.PeligroSuave
        End Get
    End Property
    Public Shared ReadOnly Property Fondo As Color
        Get
            Return _paleta.Fondo
        End Get
    End Property
    Public Shared ReadOnly Property Superficie As Color
        Get
            Return _paleta.Superficie
        End Get
    End Property
    Public Shared ReadOnly Property CampoLectura As Color
        Get
            Return _paleta.CampoLectura
        End Get
    End Property
    Public Shared ReadOnly Property Borde As Color
        Get
            Return _paleta.Borde
        End Get
    End Property
    Public Shared ReadOnly Property BordeFuerte As Color
        Get
            Return _paleta.BordeFuerte
        End Get
    End Property
    Public Shared ReadOnly Property Texto As Color
        Get
            Return _paleta.Texto
        End Get
    End Property
    Public Shared ReadOnly Property TextoTenue As Color
        Get
            Return _paleta.TextoTenue
        End Get
    End Property
    Public Shared ReadOnly Property Resaltado As Color
        Get
            Return _paleta.Resaltado
        End Get
    End Property
    Public Shared ReadOnly Property ExitoSuave As Color
        Get
            Return _paleta.ExitoSuave
        End Get
    End Property

    ''' <summary>Lee la preferencia guardada (CLARO, OSCURO o SISTEMA); cualquier otro valor es Claro.</summary>
    Public Shared Function InterpretarPreferencia(texto As String) As PreferenciaTema
        Select Case If(texto, "").Trim().ToUpperInvariant()
            Case "OSCURO" : Return PreferenciaTema.Oscuro
            Case "SISTEMA" : Return PreferenciaTema.Sistema
            Case Else : Return PreferenciaTema.Claro
        End Select
    End Function

    Public Shared Function TextoPreferencia(preferencia As PreferenciaTema) As String
        Return preferencia.ToString().ToUpperInvariant()
    End Function

    ''' <summary>
    ''' Elige la paleta. Con Sistema sigue a Windows (Configuración, Personalización, Colores, modo de aplicación)
    ''' y cambia solo si Windows cambia mientras el sistema está abierto.
    ''' </summary>
    Public Shared Sub Establecer(preferencia As PreferenciaTema)
        _preferencia = preferencia
        AplicarPaleta(If(DebeSerOscuro(preferencia), Paleta.Oscura, Paleta.Clara))
    End Sub

    Private Shared Function DebeSerOscuro(preferencia As PreferenciaTema) As Boolean
        Select Case preferencia
            Case PreferenciaTema.Oscuro : Return True
            Case PreferenciaTema.Sistema : Return WindowsEnOscuro()
            Case Else : Return False
        End Select
    End Function

    Private Shared Sub AplicarPaleta(nueva As Paleta)
        If nueva Is _paleta Then Return
        Dim anterior = _paleta
        _paleta = nueva
        RaiseEvent Cambio(anterior)
    End Sub

    Private Shared _escuchandoWindows As Boolean

    ''' <summary>Empieza a seguir los cambios de Windows, para la preferencia Sistema. Llamarlo una vez al arrancar.</summary>
    Public Shared Sub SeguirWindows()
        If _escuchandoWindows Then Return
        _escuchandoWindows = True
        AddHandler Microsoft.Win32.SystemEvents.UserPreferenceChanged,
            Sub(s, e)
                If e.Category = Microsoft.Win32.UserPreferenceCategory.General AndAlso _preferencia = PreferenciaTema.Sistema Then
                    AplicarPaleta(If(WindowsEnOscuro(), Paleta.Oscura, Paleta.Clara))
                End If
            End Sub
    End Sub

    ''' <summary>AppsUseLightTheme = 0 en el registro del usuario.</summary>
    Public Shared Function WindowsEnOscuro() As Boolean
        Try
            Using llave = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
                Dim valor = llave?.GetValue("AppsUseLightTheme")
                Return valor IsNot Nothing AndAlso Convert.ToInt32(valor) = 0
            End Using
        Catch ex As Exception When TypeOf ex Is Security.SecurityException OrElse TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is IO.IOException
            Return False
        End Try
    End Function

    ''' <summary>Nombre que ve el usuario. La carpeta de instalación y los .ini siguen siendo SDACA.</summary>
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
    ''' <summary>Títulos de grupo agrandados por <see cref="Proporcional"/>; los demás usan TituloGrupo.</summary>
    Private Shared ReadOnly TitulosEscalados As New Runtime.CompilerServices.ConditionalWeakTable(Of GroupBox, Font)

    ''' <summary>Agranda el título de un grupo en la proporción dada; 1 lo devuelve a su tamaño.</summary>
    Friend Shared Sub EscalarTituloGrupo(grupo As GroupBox, escala As Single)
        Dim anterior As Font = Nothing
        If TitulosEscalados.TryGetValue(grupo, anterior) Then
            TitulosEscalados.Remove(grupo)
            anterior.Dispose()
        End If
        If Math.Abs(escala - 1.0F) >= 0.01F Then TitulosEscalados.Add(grupo, New Font(TituloGrupo.FontFamily, TituloGrupo.Size * escala, TituloGrupo.Style))
        grupo.Invalidate()
    End Sub

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

        ' Los campos no heredan el color del contenedor: usan el negro de Windows, que no se lee en la oscura.
        If (TypeOf c Is TextBoxBase OrElse TypeOf c Is ComboBox OrElse TypeOf c Is ListBox OrElse TypeOf c Is NumericUpDown OrElse TypeOf c Is ListView OrElse TypeOf c Is TreeView) AndAlso
           c.ForeColor = SystemColors.WindowText Then c.ForeColor = Texto

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
                If CType(c, TextBox).ReadOnly Then
                    c.BackColor = CampoLectura
                ElseIf c.BackColor = SystemColors.Window Then
                    c.BackColor = Superficie
                End If
            Case TypeOf c Is ComboBox OrElse TypeOf c Is ListBox OrElse TypeOf c Is NumericUpDown OrElse TypeOf c Is ListView OrElse TypeOf c Is TreeView OrElse TypeOf c Is RichTextBox
                ' Solo el blanco de Windows: un color puesto a propósito (un campo resaltado) se respeta.
                If c.BackColor = SystemColors.Window Then c.BackColor = Superficie
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
            ' En la oscura AcentoOscuro es claro (para textos): el blanco no se leería encima.
            encima = If(Oscuro, Mezclar(Acento, Color.Black, 0.18F), AcentoOscuro)
            presionado = encima
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
            Dim letra As Font = Nothing
            If Not TitulosEscalados.TryGetValue(grupo, letra) Then letra = TituloGrupo
            Dim f = letra.Size / TituloGrupo.Size
            TextRenderer.DrawText(e.Graphics, grupo.Text, letra, New Point(CInt(grupo.LogicalToDeviceUnits(9) * f), CInt(grupo.LogicalToDeviceUnits(3) * f)), grupo.ForeColor, TextFormatFlags.NoPrefix)
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
        rejilla.RowHeadersDefaultCellStyle.BackColor = Fondo
        rejilla.RowHeadersDefaultCellStyle.ForeColor = TextoTenue
        rejilla.RowHeadersDefaultCellStyle.SelectionBackColor = AcentoSuave
        rejilla.RowHeadersDefaultCellStyle.SelectionForeColor = Texto
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
        ' Primero el área y después el mínimo: si el mínimo crece antes y supera el tamaño actual, Windows agranda
        ' la ventana para cumplirlo y el encabezado se sumaba dos veces.
        formulario.ClientSize = New Size(formulario.ClientSize.Width, formulario.ClientSize.Height + alto)
        If formulario.MinimumSize.Height > 0 Then
            formulario.MinimumSize = New Size(formulario.MinimumSize.Width, formulario.MinimumSize.Height + alto)
        End If
        cuerpo.Dock = DockStyle.Fill
        formulario.Controls.Add(cuerpo)
        formulario.Controls.Add(encabezado)
    End Sub

    ''' <summary>Tiñe la barra de título. Windows 10 ignora estos atributos y deja la barra normal.</summary>
    ''' <param name="ocultarTitulo">Pinta el texto del color de la barra, si el encabezado ya lo muestra.</param>
    Public Shared Sub ColorearVentana(formulario As Form, Optional ocultarTitulo As Boolean = False)
        AddHandler formulario.HandleCreated, Sub() TeñirBarra(formulario.Handle, ocultarTitulo)
        If formulario.IsHandleCreated Then TeñirBarra(formulario.Handle, ocultarTitulo)
        ' Todas las ventanas pasan por aquí, también las que no usan Aplicar (inicio de sesión y menú).
        If Oscuro Then TemaNativo(formulario)
    End Sub

    Private Shared Sub TeñirBarra(ventana As IntPtr, ocultarTitulo As Boolean)
        Const BordeVentana = 34
        Const ColorBarra = 35
        Const ColorTexto = 36
        Dim marinoNativo = ColorTranslator.ToWin32(Marino)
        Dim colorTitulo = ColorTranslator.ToWin32(If(ocultarTitulo, Marino, Color.White))
        Const ModoOscuro = 20
        Dim oscuroNativo = If(Oscuro, 1, 0)
        DwmSetWindowAttribute(ventana, ModoOscuro, oscuroNativo, 4)
        DwmSetWindowAttribute(ventana, BordeVentana, marinoNativo, 4)
        DwmSetWindowAttribute(ventana, ColorBarra, marinoNativo, 4)
        DwmSetWindowAttribute(ventana, ColorTexto, colorTitulo, 4)
    End Sub

    ' ---- Modo oscuro en controles de Windows ----

    <DllImport("uxtheme.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function SetWindowTheme(ventana As IntPtr, aplicacion As String, lista As String) As Integer
    End Function

    ''' <summary>Mezcla dos colores; <paramref name="cuanto"/> es la parte de <paramref name="b"/>.</summary>
    Public Shared Function Mezclar(a As Color, b As Color, cuanto As Single) As Color
        Return Color.FromArgb(CInt(a.R + (CInt(b.R) - a.R) * cuanto), CInt(a.G + (CInt(b.G) - a.G) * cuanto), CInt(a.B + (CInt(b.B) - a.B) * cuanto))
    End Function

    ''' <summary>
    ''' Barras de desplazamiento y listas desplegables oscuras (los temas DarkMode_Explorer y DarkMode_CFD de
    ''' Windows 10 1809 en adelante). En la clara vuelve al tema normal. El calendario de las fechas y los
    ''' cuadros de mensaje siguen claros: Windows no les da modo oscuro.
    ''' </summary>
    Public Shared Sub TemaNativo(raiz As Control)
        For Each c In Descendientes(raiz)
            Dim temaOscuro As String = Nothing
            If TypeOf c Is ComboBox OrElse TypeOf c Is TextBoxBase Then
                ' CFD también oscurece el borde y el texto de un campo desactivado, que con Explorer queda casi invisible.
                temaOscuro = "DarkMode_CFD"
            ElseIf TypeOf c Is DataGridView OrElse TypeOf c Is ScrollBar OrElse TypeOf c Is ListBox OrElse TypeOf c Is ListView OrElse TypeOf c Is TreeView OrElse
                   (TypeOf c Is ScrollableControl AndAlso CType(c, ScrollableControl).AutoScroll) Then
                temaOscuro = "DarkMode_Explorer"
            End If
            If temaOscuro Is Nothing Then Continue For
            Dim control = c
            Dim nombre = temaOscuro
            If control.IsHandleCreated Then
                SetWindowTheme(control.Handle, If(Oscuro, nombre, "Explorer"), Nothing)
            Else
                AddHandler control.HandleCreated, Sub() SetWindowTheme(control.Handle, If(Oscuro, nombre, "Explorer"), Nothing)
            End If
        Next
    End Sub

    Private Shared Iterator Function Descendientes(raiz As Control) As IEnumerable(Of Control)
        For Each c In raiz.Controls.Cast(Of Control)().ToList()
            Yield c
            For Each nieto In Descendientes(c)
                Yield nieto
            Next
        Next
    End Function

    Private Shared ReadOnly SerializaFondo As Reflection.MethodInfo = GetType(Control).GetMethod("ShouldSerializeBackColor", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
    Private Shared ReadOnly SerializaTexto As Reflection.MethodInfo = GetType(Control).GetMethod("ShouldSerializeForeColor", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)

    ''' <summary>True si el color se puso a propósito y no se hereda del contenedor.</summary>
    Private Shared Function Propio(c As Control, metodo As Reflection.MethodInfo, color As Color, delPadre As Color) As Boolean
        If metodo IsNot Nothing Then Return CBool(metodo.Invoke(c, Nothing))
        Return c.Parent Is Nothing OrElse color <> delPadre
    End Function

    Private Shared Function Traducir(color As Color, desde As Color(), hacia As Color()) As Color
        For i = 0 To desde.Length - 1
            If color.ToArgb() = desde(i).ToArgb() Then Return hacia(i)
        Next
        Return color
    End Function

    ''' <summary>
    ''' Vuelve a pintar una ventana abierta con la paleta actual: cambia cada color de la paleta anterior por el
    ''' mismo de la nueva, repinta los botones, las rejillas y la barra de título. El logo y la vista del cheque,
    ''' que imitan papel, quedan blancos.
    ''' </summary>
    Public Shared Sub Recolorear(formulario As Form, anterior As Paleta, Optional ocultarTitulo As Boolean = False)
        Dim fondosAntes = anterior.Fondos()
        Dim fondosAhora = _paleta.Fondos()
        Dim textosAntes = anterior.Textos()
        Dim textosAhora = _paleta.Textos()
        formulario.SuspendLayout()
        formulario.BackColor = Traducir(formulario.BackColor, fondosAntes, fondosAhora)
        formulario.ForeColor = Traducir(formulario.ForeColor, textosAntes, textosAhora)
        For Each c In Descendientes(formulario).ToList()
            If TypeOf c Is LogoEmpresa OrElse c.Name = ControlExcluido OrElse (c.Parent IsNot Nothing AndAlso c.Parent.Name = ControlExcluido) Then Continue For
            If Propio(c, SerializaFondo, c.BackColor, If(c.Parent?.BackColor, Color.Empty)) Then c.BackColor = Traducir(c.BackColor, fondosAntes, fondosAhora)
            If Propio(c, SerializaTexto, c.ForeColor, If(c.Parent?.ForeColor, Color.Empty)) Then c.ForeColor = Traducir(c.ForeColor, textosAntes, textosAhora)
            Dim enlace = TryCast(c, LinkLabel)
            If enlace IsNot Nothing Then
                enlace.LinkColor = Traducir(enlace.LinkColor, textosAntes, textosAhora)
                enlace.ActiveLinkColor = Traducir(enlace.ActiveLinkColor, textosAntes, textosAhora)
                enlace.VisitedLinkColor = Traducir(enlace.VisitedLinkColor, textosAntes, textosAhora)
            End If
            If c.GetType() Is GetType(Button) AndAlso CType(c, Button).FlatStyle = FlatStyle.Flat Then PintarBoton(CType(c, Button), EstiloDe(CType(c, Button)))
            Dim barra = TryCast(c, BarraComandos)
            If barra IsNot Nothing Then barra.Repintar()
            Dim rejilla = TryCast(c, DataGridView)
            If rejilla IsNot Nothing Then VestirRejilla(rejilla)
        Next
        TemaNativo(formulario)
        If formulario.IsHandleCreated Then TeñirBarra(formulario.Handle, ocultarTitulo)
        formulario.ResumeLayout(True)
        formulario.Invalidate(True)
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
