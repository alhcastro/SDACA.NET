Imports System.Drawing.Drawing2D
Imports SDACA.Ui

''' <summary>Tarjeta de una opción del menú. Hereda de Button por el teclado y la accesibilidad.</summary>
Friend NotInheritable Class TarjetaOpcion
    Inherits Button

    Private Shared ReadOnly FuenteTitulo As New Font(Tema.FuenteSemibold, 10.0F)
    Private Shared ReadOnly FuenteEstado As New Font(Tema.FuenteBase, 8.25F)
    Private Shared ReadOnly FuenteIcono As New Font(Tema.FuenteIconos, 14.0F)

    Public ReadOnly Property Opcion As OpcionMenu
    ''' <summary>La búsqueda mezcla módulos, así que cada tarjeta recuerda el suyo.</summary>
    Public ReadOnly Property Modulo As IModulo
    Private _encima As Boolean

    Public Sub New(opcion As OpcionMenu, modulo As IModulo)
        Me.Opcion = opcion
        Me.Modulo = modulo
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Text = opcion.Etiqueta
        UseMnemonic = False
        AccessibleName = If(EstaPendiente, opcion.Etiqueta & ", pendiente de migrar", opcion.Etiqueta)
        Margin = New Padding(0, 0, 12, 12)
    End Sub

    Private ReadOnly Property EstaPendiente As Boolean
        Get
            Return Opcion.Crear Is Nothing
        End Get
    End Property

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        _encima = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _encima = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnGotFocus(e As EventArgs)
        MyBase.OnGotFocus(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnLostFocus(e As EventArgs)
        MyBase.OnLostFocus(e)
        Invalidate()
    End Sub

    ''' <summary>El árbol anterior abría con Enter. Button solo responde a la barra espaciadora.</summary>
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            PerformClick()
        End If
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(If(Parent Is Nothing, Tema.Fondo, Parent.BackColor))
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim resaltada = _encima OrElse Focused
        Dim area = New Rectangle(0, 0, Width - 1, Height - 1)
        Using ruta = Tema.Redondeado(area, 8), relleno As New SolidBrush(Tema.Superficie),
              lapiz As New Pen(If(resaltada, Tema.Acento, Tema.Borde), If(Focused, 2.0F, 1.0F))
            g.FillPath(relleno, ruta)
            g.DrawPath(lapiz, ruta)
        End Using

        Dim margen = LogicalToDeviceUnits(14)
        Dim lado = Height - margen * 2
        Dim insignia = New Rectangle(margen, margen, lado, lado)
        Using ruta = Tema.Redondeado(insignia, 6), relleno As New SolidBrush(If(EstaPendiente, Tema.CampoLectura, Tema.AcentoSuave))
            g.FillPath(relleno, ruta)
        End Using
        Dim glifo = If(EstaPendiente, IconoMenu.Pendiente, Opcion.Icono)
        TextRenderer.DrawText(g, glifo.ToString(), FuenteIcono, insignia, If(EstaPendiente, Tema.TextoTenue, Tema.Acento),
                              TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)

        Dim izquierda = insignia.Right + LogicalToDeviceUnits(12)
        Dim ancho = Width - izquierda - LogicalToDeviceUnits(10)
        Dim banderas = TextFormatFlags.NoPrefix Or TextFormatFlags.WordBreak Or TextFormatFlags.EndEllipsis Or TextFormatFlags.Left
        If EstaPendiente Then
            TextRenderer.DrawText(g, Opcion.Etiqueta, FuenteTitulo, New Rectangle(izquierda, LogicalToDeviceUnits(13), ancho, LogicalToDeviceUnits(22)), Tema.TextoTenue,
                                  TextFormatFlags.NoPrefix Or TextFormatFlags.EndEllipsis Or TextFormatFlags.SingleLine)
            TextRenderer.DrawText(g, "Pendiente de migrar", FuenteEstado, New Rectangle(izquierda, LogicalToDeviceUnits(35), ancho, LogicalToDeviceUnits(18)), Tema.TextoTenue, TextFormatFlags.NoPrefix)
        Else
            TextRenderer.DrawText(g, Opcion.Etiqueta, FuenteTitulo, New Rectangle(izquierda, 0, ancho, Height), Tema.Texto,
                                  banderas Or TextFormatFlags.VerticalCenter)
        End If
    End Sub

End Class

''' <summary>Módulo en la barra lateral. Se marca con una franja de acento.</summary>
Friend NotInheritable Class BotonLateral
    Inherits Button

    Private Shared ReadOnly Fuente As New Font(Tema.FuenteBase, 9.75F)
    Private Shared ReadOnly FuenteActiva As New Font(Tema.FuenteSemibold, 9.75F)

    Public ReadOnly Property Modulo As IModulo
    Private _activo As Boolean
    Private _encima As Boolean

    Public Sub New(modulo As IModulo)
        Me.Modulo = modulo
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Text = modulo.Descripcion
        UseMnemonic = False
        Margin = Padding.Empty
    End Sub

    Public Property Activo As Boolean
        Get
            Return _activo
        End Get
        Set(value As Boolean)
            _activo = value
            AccessibleDescription = If(value, "Módulo seleccionado", "")
            Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        _encima = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _encima = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnGotFocus(e As EventArgs)
        MyBase.OnGotFocus(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnLostFocus(e As EventArgs)
        MyBase.OnLostFocus(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(If(_activo OrElse _encima, Tema.MarinoClaro, Tema.Marino))
        If _activo Then
            Using relleno As New SolidBrush(Tema.Acento)
                g.FillRectangle(relleno, 0, 0, LogicalToDeviceUnits(4), Height)
            End Using
        End If
        TextRenderer.DrawText(g, Text, If(_activo, FuenteActiva, Fuente), New Rectangle(LogicalToDeviceUnits(20), 0, Width - LogicalToDeviceUnits(32), Height),
                              If(_activo, Color.White, Tema.TextoSobreMarino),
                              TextFormatFlags.NoPrefix Or TextFormatFlags.WordBreak Or TextFormatFlags.VerticalCenter Or TextFormatFlags.Left)
        If Focused AndAlso ShowFocusCues Then
            Dim foco = ClientRectangle
            foco.Inflate(-3, -3)
            ControlPaint.DrawFocusRectangle(g, foco, Color.White, Tema.Marino)
        End If
    End Sub

End Class
