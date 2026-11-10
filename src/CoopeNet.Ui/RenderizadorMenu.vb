''' <summary>
''' Menús contextuales y desplegables con los colores del tema, claro u oscuro. El de Windows siempre es claro.
''' Lee <see cref="Tema"/> al pintar, así que sirve aunque el modo cambie con el menú creado.
''' </summary>
Public NotInheritable Class RenderizadorMenu
    Inherits ToolStripProfessionalRenderer

    Private Shared ReadOnly FuenteMarca As New Font(Tema.FuenteIconos, 9.0F)

    Public Sub New()
        MyBase.New(New Colores())
        RoundedEdges = False
    End Sub

    Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
        e.TextColor = If(e.Item.Enabled, Tema.Texto, Tema.TextoTenue)
        MyBase.OnRenderItemText(e)
    End Sub

    Protected Overrides Sub OnRenderItemCheck(e As ToolStripItemImageRenderEventArgs)
        Dim area = e.ImageRectangle
        area.Inflate(2, 2)
        Using relleno As New SolidBrush(Tema.AcentoSuave)
            e.Graphics.FillRectangle(relleno, area)
        End Using
        TextRenderer.DrawText(e.Graphics, ChrW(&HE73E).ToString(), FuenteMarca, area, Tema.Acento,
                              TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
    End Sub

    Private NotInheritable Class Colores
        Inherits ProfessionalColorTable

        Public Sub New()
            UseSystemColors = False
        End Sub

        Public Overrides ReadOnly Property ToolStripDropDownBackground As Color
            Get
                Return Tema.Superficie
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientBegin As Color
            Get
                Return Tema.Superficie
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientMiddle As Color
            Get
                Return Tema.Superficie
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientEnd As Color
            Get
                Return Tema.Superficie
            End Get
        End Property
        Public Overrides ReadOnly Property MenuBorder As Color
            Get
                Return Tema.BordeFuerte
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemBorder As Color
            Get
                Return Tema.AcentoSuave
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelected As Color
            Get
                Return Tema.AcentoSuave
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelectedGradientBegin As Color
            Get
                Return Tema.AcentoSuave
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelectedGradientEnd As Color
            Get
                Return Tema.AcentoSuave
            End Get
        End Property
        Public Overrides ReadOnly Property SeparatorDark As Color
            Get
                Return Tema.Borde
            End Get
        End Property
        Public Overrides ReadOnly Property SeparatorLight As Color
            Get
                Return Tema.Superficie
            End Get
        End Property
        Public Overrides ReadOnly Property CheckBackground As Color
            Get
                Return Tema.AcentoSuave
            End Get
        End Property
        Public Overrides ReadOnly Property CheckSelectedBackground As Color
            Get
                Return Tema.AcentoSuave
            End Get
        End Property
    End Class

End Class
