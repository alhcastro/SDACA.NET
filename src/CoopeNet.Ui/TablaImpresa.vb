Imports System.Drawing.Printing

''' <summary>Una columna de <see cref="TablaImpresa"/>.</summary>
Public NotInheritable Class ColumnaImpresa
    Public Sub New(titulo As String, peso As Single, Optional derecha As Boolean = True)
        Me.Titulo = titulo
        Me.Peso = peso
        Me.Derecha = derecha
    End Sub

    Public ReadOnly Property Titulo As String
    ''' <summary>El ancho relativo frente a las demás columnas.</summary>
    Public ReadOnly Property Peso As Single
    Public ReadOnly Property Derecha As Boolean
End Class

''' <summary>
''' Informe de una tabla, para reemplazar los ActiveReports de los REP de VB6: logo y empresa, título, líneas de
''' encabezado, filas alternadas en gris, una fila de totales al final y el pie con la empresa, el nombre del
''' informe, la fecha y "Pagina # n de m".
''' </summary>
Public NotInheritable Class TablaImpresa
    Inherits PrintDocument

    Private Const AltoFila As Single = 16
    ''' <summary>El título puede ocupar dos líneas.</summary>
    Private Const AltoTitulo As Single = 32

    Private Shared ReadOnly FuenteTitulo As New Font("Arial", 10, FontStyle.Bold)
    Private Shared ReadOnly FuenteTexto As New Font("Arial", 8)
    Private Shared ReadOnly FuenteRotulo As New Font("Arial", 8, FontStyle.Bold)
    Private Shared ReadOnly FuentePie As New Font("Arial", 7)
    Private Shared ReadOnly Gris As New SolidBrush(Color.FromArgb(&HE0, &HE0, &HE0))

    Private ReadOnly _titulo As String
    Private ReadOnly _empresa As String
    Private ReadOnly _informe As String
    Private ReadOnly _encabezado As IReadOnlyList(Of String)
    Private ReadOnly _columnas As IReadOnlyList(Of ColumnaImpresa)
    Private ReadOnly _filas As IReadOnlyList(Of String())
    Private ReadOnly _totales As String()
    Private ReadOnly _impreso As Date = Date.Now
    Private _siguiente As Integer
    Private _pagina As Integer
    Private _paginas As Integer

    ''' <param name="informe">El nombre que VB6 ponía en el pie, por ejemplo "SD0502-RPT0013".</param>
    ''' <param name="encabezado">Líneas que van bajo el título en cada página (artículo, inventario inicial).</param>
    ''' <param name="totales">La fila final; Nothing si no lleva.</param>
    Public Sub New(titulo As String, empresa As String, informe As String, encabezado As IReadOnlyList(Of String),
                   columnas As IReadOnlyList(Of ColumnaImpresa), filas As IReadOnlyList(Of String()), totales As String())
        _titulo = titulo
        _empresa = empresa
        _informe = informe
        _encabezado = encabezado
        _columnas = columnas
        _filas = filas
        _totales = totales
        DocumentName = titulo
        ' Márgenes de los REP: 700 y 400 twips a los lados, 1000 arriba y abajo.
        DefaultPageSettings.Margins = New Margins(49, 28, 69, 69)
    End Sub

    Private Function FilasPorPagina(area As Rectangle) As Integer
        Dim cabecera = 34 + AltoTitulo + 18 * _encabezado.Count + 20 + AltoFila + 2
        Return Math.Max(1, CInt(Math.Floor((area.Height - cabecera - 24) / AltoFila)))
    End Function

    Protected Overrides Sub OnBeginPrint(e As PrintEventArgs)
        MyBase.OnBeginPrint(e)
        _siguiente = 0
        _pagina = 0
        Dim porPagina = FilasPorPagina(DefaultPageSettings.Bounds.Size - New Size(DefaultPageSettings.Margins.Left + DefaultPageSettings.Margins.Right, DefaultPageSettings.Margins.Top + DefaultPageSettings.Margins.Bottom))
        Dim filas = _filas.Count + If(_totales Is Nothing, 0, 1)
        _paginas = Math.Max(1, CInt(Math.Ceiling(filas / porPagina)))
    End Sub

    Private Function FilasPorPagina(tamano As Size) As Integer
        Return FilasPorPagina(New Rectangle(Point.Empty, tamano))
    End Function

    Protected Overrides Sub OnPrintPage(e As PrintPageEventArgs)
        MyBase.OnPrintPage(e)
        _pagina += 1
        Dim g = e.Graphics
        Dim a = e.MarginBounds
        LogoEmpresa.Dibujar(g, New Rectangle(a.Left, a.Top, 170, 28))
        Dim derecha As New StringFormat With {.Alignment = StringAlignment.Far}
        g.DrawString(_empresa, FuenteTitulo, Brushes.Black, New RectangleF(a.Left + 180, a.Top, a.Width - 180, 18), derecha)
        Dim y As Single = a.Top + 34
        g.DrawString(_titulo, FuenteRotulo, Brushes.Black, New RectangleF(a.Left, y, a.Width, AltoTitulo))
        y += AltoTitulo - 18
        For Each linea In _encabezado
            y += 18
            g.DrawString(linea, FuenteTexto, Brushes.Black, New RectangleF(a.Left, y, a.Width, 18))
        Next
        y += 20
        Dim xs = Posiciones(a)
        Fila(g, xs, y, _columnas.Select(Function(c) c.Titulo).ToArray(), FuenteRotulo, Nothing)
        y += AltoFila
        g.DrawLine(Pens.Black, a.Left, y, a.Right, y)
        y += 2
        Dim porPagina = FilasPorPagina(a)
        Dim enPagina = 0
        While _siguiente < _filas.Count AndAlso enPagina < porPagina
            Fila(g, xs, y, _filas(_siguiente), FuenteTexto, If(_siguiente Mod 2 = 0, Gris, Nothing))
            y += AltoFila
            _siguiente += 1
            enPagina += 1
        End While
        Dim faltaTotal = _totales IsNot Nothing AndAlso _siguiente >= _filas.Count
        If faltaTotal AndAlso enPagina < porPagina Then
            g.DrawLine(Pens.Black, a.Left, y + 1, a.Right, y + 1)
            Fila(g, xs, y + 3, _totales, FuenteRotulo, Nothing)
            faltaTotal = False
            _siguiente += 1
        End If
        e.HasMorePages = _siguiente < _filas.Count OrElse faltaTotal
        ' Pie, como PageFooter_Format de los REP.
        Dim pie = a.Bottom + 8
        g.DrawLine(Pens.Gray, a.Left, pie - 2, a.Right, pie - 2)
        g.DrawString($"{_empresa}    {_informe}    {_impreso:dd/MMM/yyyy HH:mm:ss}", FuentePie, Brushes.Black, a.Left, pie)
        g.DrawString($"Pagina # {_pagina} de {_paginas}", FuentePie, Brushes.Black, New RectangleF(a.Left, pie, a.Width, 14), derecha)
    End Sub

    Private Function Posiciones(a As Rectangle) As Single()
        Dim total = _columnas.Sum(Function(c) c.Peso)
        Dim xs(_columnas.Count) As Single
        xs(0) = a.Left
        For i = 0 To _columnas.Count - 1
            xs(i + 1) = xs(i) + a.Width * _columnas(i).Peso / total
        Next
        Return xs
    End Function

    Private Sub Fila(g As Graphics, xs As Single(), y As Single, valores As String(), fuente As Font, fondo As Brush)
        If fondo IsNot Nothing Then g.FillRectangle(fondo, xs(0), y, xs(xs.Length - 1) - xs(0), AltoFila)
        For i = 0 To Math.Min(valores.Length, _columnas.Count) - 1
            Dim formato As New StringFormat With {.Alignment = If(_columnas(i).Derecha, StringAlignment.Far, StringAlignment.Near), .Trimming = StringTrimming.EllipsisCharacter, .FormatFlags = StringFormatFlags.NoWrap}
            g.DrawString(If(valores(i), ""), fuente, Brushes.Black, New RectangleF(xs(i) + 2, y + 1, xs(i + 1) - xs(i) - 4, AltoFila), formato)
        Next
    End Sub

    ''' <summary>La vista previa maximizada, como FrmReporte de los REP.</summary>
    Public Shared Sub Mostrar(dueno As IWin32Window, documento As PrintDocument)
        Using vista As New PrintPreviewDialog With {.Document = documento, .UseAntiAlias = True, .WindowState = FormWindowState.Maximized}
            vista.ShowDialog(dueno)
        End Using
    End Sub

End Class
