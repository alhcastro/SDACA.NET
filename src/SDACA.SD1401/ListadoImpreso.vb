Imports System.Drawing.Printing
Imports SDACA.Core.Bancos
Imports SDACA.Ui

Namespace SD1401

    ''' <summary>REP\RPT0009.Dsr: listado de movimientos del banco que no están en libros.</summary>
    Public NotInheritable Class ListadoImpreso
        Inherits PrintDocument

        Private Const AltoFila As Integer = 17

        Private Shared ReadOnly FuenteTitulo As New Font("Arial", 10, FontStyle.Bold)
        Private Shared ReadOnly FuenteTexto As New Font("Arial", 8.5F)
        Private Shared ReadOnly FuenteRotulo As New Font("Arial", 8.5F, FontStyle.Bold)

        Private ReadOnly _titulo As String
        Private ReadOnly _empresa As String
        Private ReadOnly _filas As IReadOnlyList(Of FilaConciliacion)
        Private _siguiente As Integer

        Public Sub New(titulo As String, empresa As String, filas As IReadOnlyList(Of FilaConciliacion))
            _titulo = titulo
            _empresa = empresa
            _filas = filas
            DocumentName = titulo
        End Sub

        Protected Overrides Sub OnBeginPrint(e As PrintEventArgs)
            MyBase.OnBeginPrint(e)
            _siguiente = 0
        End Sub

        Protected Overrides Sub OnPrintPage(e As PrintPageEventArgs)
            MyBase.OnPrintPage(e)
            Dim g = e.Graphics
            Dim a = e.MarginBounds
            LogoEmpresa.Dibujar(g, New Rectangle(a.Left, a.Top, 170, 28))
            Dim derecha As New StringFormat With {.Alignment = StringAlignment.Far}
            g.DrawString(_empresa, FuenteTitulo, Brushes.Black, New RectangleF(a.Left + 180, a.Top, a.Width - 180, 18), derecha)
            g.DrawString(_titulo, FuenteTexto, Brushes.Black, New RectangleF(a.Left, a.Top + 34, a.Width, 18))
            Dim y = a.Top + 54
            g.DrawLine(Pens.Black, a.Left, y, a.Right, y)
            y += 4
            Fila(g, a, y, "Fecha", "Documento", "Detalle", "Crédito", "Debito", FuenteRotulo)
            y += AltoFila
            While _siguiente < _filas.Count AndAlso y + AltoFila <= a.Bottom - AltoFila
                Dim f = _filas(_siguiente)
                Fila(g, a, y, f.Fecha.ToString("dd/MMM/yyyy"), f.Documento, f.Detalle, If(f.Tipo = 0, f.Monto.ToString("#,##0.00"), ""), If(f.Tipo = 0, "", f.Monto.ToString("#,##0.00")), FuenteTexto)
                y += AltoFila
                _siguiente += 1
            End While
            e.HasMorePages = _siguiente < _filas.Count
            If Not e.HasMorePages Then
                Fila(g, a, y, "", "", $"Total ({_filas.Count})",
                     _filas.Where(Function(f) f.Tipo = 0).Sum(Function(f) f.Monto).ToString("#,##0.00"),
                     _filas.Where(Function(f) f.Tipo <> 0).Sum(Function(f) f.Monto).ToString("#,##0.00"), FuenteRotulo)
            End If
        End Sub

        Private Shared Sub Fila(g As Graphics, a As Rectangle, y As Integer, fecha As String, documento As String, detalle As String, credito As String, debito As String, fuente As Font)
            Dim derecha As New StringFormat With {.Alignment = StringAlignment.Far}
            g.DrawString(fecha, fuente, Brushes.Black, a.Left, y)
            g.DrawString(documento, fuente, Brushes.Black, a.Left + 85, y)
            g.DrawString(InformeConciliacionImpreso.UnaLinea(detalle), fuente, Brushes.Black, New RectangleF(a.Left + 185, y, a.Width - 385, AltoFila), New StringFormat With {.Trimming = StringTrimming.EllipsisCharacter, .FormatFlags = StringFormatFlags.NoWrap})
            g.DrawString(credito, fuente, Brushes.Black, New RectangleF(a.Right - 200, y, 100, AltoFila), derecha)
            g.DrawString(debito, fuente, Brushes.Black, New RectangleF(a.Right - 100, y, 100, AltoFila), derecha)
        End Sub

    End Class

End Namespace
