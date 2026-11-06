''' <summary>TabControl plano: subraya la pestaña activa. El nativo no deja cambiar el fondo de su franja.</summary>
Public Class Pestanas
    Inherits TabControl

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Padding = New Point(14, 5)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.Clear(If(Parent Is Nothing, Tema.Superficie, Parent.BackColor))
        If TabCount = 0 Then Return

        Dim linea = GetTabRect(0).Bottom
        Using lapiz As New Pen(Tema.Borde)
            e.Graphics.DrawLine(lapiz, 0, linea, ClientSize.Width, linea)
        End Using

        For i = 0 To TabCount - 1
            Dim area = GetTabRect(i)
            Dim activa = i = SelectedIndex
            Dim color = If(activa, Tema.Acento, Tema.TextoTenue)
            TextRenderer.DrawText(e.Graphics, TabPages(i).Text, Font, area, color,
                                  TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPrefix)
            If activa Then
                Using relleno As New SolidBrush(Tema.Acento)
                    e.Graphics.FillRectangle(relleno, area.Left, linea - 2, area.Width, 3)
                End Using
                If Focused AndAlso ShowFocusCues Then
                    Dim foco = area
                    foco.Inflate(-3, -3)
                    ControlPaint.DrawFocusRectangle(e.Graphics, foco)
                End If
            End If
        Next
    End Sub

End Class
