Imports System.Drawing

''' <summary>
''' Reemplaza a SDSizer.ocx (ResizeControls): al agrandar la ventana, cada control crece y se mueve en
''' proporción, y su letra crece con el menor de los dos factores para no deformarse. Las medidas se toman
''' una vez, con el tamaño de diseño ya escalado por <see cref="Tema.Aplicar"/>; después siempre se calcula
''' desde ellas, así que no se acumula redondeo.
''' </summary>
''' <remarks>
''' Entra en paneles, grupos y pestañas; no en los controles de usuario (la barra de comandos acomoda sus
''' botones sola) ni en las partes internas de una rejilla. Quita los anclajes, que pelearían con la escala.
''' Las páginas de pestañas y los controles acoplados solo cambian de letra: su tamaño lo pone su padre.
''' </remarks>
Public NotInheritable Class Proporcional

    Private NotInheritable Class Medida
        Public Limites As Rectangle
        Public Letra As Font
        Public Fijo As Boolean
        ' Rejillas: Tema fija la letra de celdas y encabezados y los altos aparte de Font.
        Public LetraCeldas As Font
        Public LetraEncabezados As Font
        Public AltoFila As Integer
        Public AltoEncabezados As Integer
    End Class

    Private ReadOnly _contenedor As Control
    Private ReadOnly _base As Size
    Private ReadOnly _medidas As New Dictionary(Of Control, Medida)
    Private ReadOnly _creadas As New List(Of Font)
    Private _escala As Single = 1.0F

    Private Sub New(contenedor As Control)
        _contenedor = contenedor
        _base = contenedor.ClientSize
        Registrar(contenedor)
        AddHandler contenedor.Resize, Sub() Escalar()
    End Sub

    ''' <summary>
    ''' Empieza a escalar lo que hay dentro de <paramref name="contenedor"/> desde su tamaño actual. Llamarlo
    ''' al cargar la ventana, antes de que se restaure su último tamaño.
    ''' </summary>
    Public Shared Sub Aplicar(contenedor As Control)
        Dim p As New Proporcional(contenedor)
    End Sub

    Private Shared Function EsContenedor(c As Control) As Boolean
        Return TypeOf c Is Panel OrElse TypeOf c Is GroupBox OrElse TypeOf c Is TabControl OrElse TypeOf c Is TabPage
    End Function

    Private Sub Registrar(padre As Control)
        For Each c In padre.Controls.Cast(Of Control)()
            Dim m As New Medida With {.Limites = c.Bounds, .Letra = c.Font, .Fijo = TypeOf c Is TabPage OrElse c.Dock <> DockStyle.None}
            If Not m.Fijo Then c.Anchor = AnchorStyles.Top Or AnchorStyles.Left
            Dim rejilla = TryCast(c, DataGridView)
            If rejilla IsNot Nothing Then
                m.LetraCeldas = If(rejilla.DefaultCellStyle.Font, rejilla.Font)
                m.LetraEncabezados = If(rejilla.ColumnHeadersDefaultCellStyle.Font, rejilla.Font)
                m.AltoFila = rejilla.RowTemplate.Height
                m.AltoEncabezados = rejilla.ColumnHeadersHeight
            End If
            _medidas(c) = m
            If EsContenedor(c) AndAlso Not TypeOf c Is UserControl Then Registrar(c)
        Next
    End Sub

    Private Function Escalada(letra As Font, escala As Single) As Font
        If Math.Abs(escala - 1.0F) < 0.01F Then Return letra
        Dim nueva As New Font(letra.FontFamily, letra.Size * escala, letra.Style, letra.Unit)
        _creadas.Add(nueva)
        Return nueva
    End Function

    Private Sub Escalar()
        Dim actual = _contenedor.ClientSize
        If actual.Width = 0 OrElse actual.Height = 0 OrElse _base.Width = 0 OrElse _base.Height = 0 Then Return
        Dim fx = actual.Width / CSng(_base.Width)
        Dim fy = actual.Height / CSng(_base.Height)
        Dim escala = Math.Min(fx, fy)
        Dim cambiaLetra = Math.Abs(escala - _escala) > 0.01F
        Dim viejas = If(cambiaLetra, _creadas.ToList(), New List(Of Font))
        If cambiaLetra Then _creadas.Clear()
        _contenedor.SuspendLayout()
        Try
            For Each par In _medidas
                Dim c = par.Key
                Dim m = par.Value
                If cambiaLetra Then
                    c.Font = Escalada(m.Letra, escala)
                    Dim rejilla = TryCast(c, DataGridView)
                    If rejilla IsNot Nothing Then EscalarRejilla(rejilla, m, escala)
                    If TypeOf c Is GroupBox Then Tema.EscalarTituloGrupo(CType(c, GroupBox), escala)
                End If
                If Not m.Fijo Then
                    c.Bounds = New Rectangle(CInt(m.Limites.X * fx), CInt(m.Limites.Y * fy), CInt(m.Limites.Width * fx), CInt(m.Limites.Height * fy))
                End If
            Next
            If cambiaLetra Then _escala = escala
        Finally
            _contenedor.ResumeLayout(True)
        End Try
        For Each letra In viejas
            letra.Dispose()
        Next
    End Sub

    Private Sub EscalarRejilla(rejilla As DataGridView, m As Medida, escala As Single)
        rejilla.DefaultCellStyle.Font = Escalada(m.LetraCeldas, escala)
        rejilla.ColumnHeadersDefaultCellStyle.Font = Escalada(m.LetraEncabezados, escala)
        Dim alto = Math.Max(1, CInt(m.AltoFila * escala))
        rejilla.RowTemplate.Height = alto
        For Each fila In rejilla.Rows.Cast(Of DataGridViewRow)()
            fila.Height = alto
        Next
        If rejilla.ColumnHeadersHeightSizeMode <> DataGridViewColumnHeadersHeightSizeMode.AutoSize Then
            rejilla.ColumnHeadersHeight = Math.Max(4, CInt(m.AltoEncabezados * escala))
        End If
    End Sub

End Class
