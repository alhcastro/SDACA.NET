Imports System.Drawing.Printing
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Ui

Namespace SD1401

    ''' <summary>Comprobante de Movimiento Bancario, con las mismas secciones de REP\RPT0010.Dsr.</summary>
    Public NotInheritable Class ComprobanteImpreso
        Inherits PrintDocument

        Private Const Titulo As String = "Comprobante de Movimiento Bancario"
        Private Const AltoFila As Integer = 20

        Private Shared ReadOnly FuenteEmpresa As New Font("Arial", 13, FontStyle.Bold)
        Private Shared ReadOnly FuenteTitulo As New Font("Arial", 10.5F)
        Private Shared ReadOnly FuenteTexto As New Font("Arial", 9.5F)
        Private Shared ReadOnly FuenteRotulo As New Font("Arial", 9.5F, FontStyle.Bold)
        Private Shared ReadOnly FuentePie As New Font("Arial", 7.5F)

        Private ReadOnly _comprobante As ComprobanteMovimiento
        Private ReadOnly _empresa As String
        Private _siguiente As Integer

        Public Sub New(comprobante As ComprobanteMovimiento, empresa As String)
            _comprobante = comprobante
            _empresa = empresa
            DocumentName = Titulo
        End Sub

        Protected Overrides Sub OnBeginPrint(e As PrintEventArgs)
            MyBase.OnBeginPrint(e)
            _siguiente = 0
        End Sub

        Protected Overrides Sub OnPrintPage(e As PrintPageEventArgs)
            MyBase.OnPrintPage(e)
            Dim g = e.Graphics
            Dim area = e.MarginBounds
            Dim y = Encabezado(g, area)
            If _siguiente = 0 Then y = Datos(g, area, y)
            y = TablaAsiento(g, area, y)
            e.HasMorePages = _siguiente < _comprobante.Lineas.Count
            If Not e.HasMorePages Then Firmas(g, area, y)
            Pie(g, area)
        End Sub

        Private Function Encabezado(g As Graphics, area As Rectangle) As Integer
            LogoEmpresa.Dibujar(g, New Rectangle(area.Left, area.Top, 200, 34))
            Dim derecha As New StringFormat With {.Alignment = StringAlignment.Far}
            g.DrawString(_empresa, FuenteEmpresa, Brushes.Black, New RectangleF(area.Left + 210, area.Top, area.Width - 210, 24), derecha)
            g.DrawString(Titulo, FuenteTitulo, Brushes.Black, New RectangleF(area.Left + 210, area.Top + 22, area.Width - 210, 20), derecha)
            Dim y = area.Top + 48
            g.DrawLine(Pens.Black, area.Left, y, area.Right, y)
            Return y + 12
        End Function

        Private Function Datos(g As Graphics, area As Rectangle, inicio As Integer) As Integer
            Dim c = _comprobante
            Dim mitad = area.Left + area.Width \ 2
            Dim y = inicio
            Campo(g, "Fecha:", c.Fecha.ToString("dd/MMM/yyyy"), area.Left, y, mitad - area.Left)
            Campo(g, "Movimiento:", c.Ids.ToString(), mitad, y, area.Right - mitad)
            y += AltoFila
            Campo(g, "Tipo:", c.Tipo, area.Left, y, mitad - area.Left)
            Campo(g, "Documento:", c.Documento, mitad, y, area.Right - mitad)
            y += AltoFila
            Campo(g, "Departamento:", c.Departamento, area.Left, y, area.Width)
            y += AltoFila
            Campo(g, "Banco:", c.Banco, area.Left, y, area.Width)
            y += AltoFila
            Campo(g, "Cuenta:", c.Cuenta, area.Left, y, area.Width)
            y += AltoFila
            Campo(g, "Monto:", c.Monto.ToString("#,##0.00"), area.Left, y, area.Width)
            y += AltoFila
            y += Parrafo(g, c.MontoLetras, area.Left + AnchoRotulo, y, area.Width - AnchoRotulo)
            g.DrawString("Detalle:", FuenteRotulo, Brushes.Black, area.Left, y)
            y += Parrafo(g, c.Detalle, area.Left + AnchoRotulo, y, area.Width - AnchoRotulo)
            Return y + 10
        End Function

        Private Const AnchoRotulo As Integer = 110

        Private Shared Sub Campo(g As Graphics, rotulo As String, valor As String, x As Integer, y As Integer, ancho As Integer)
            g.DrawString(rotulo, FuenteRotulo, Brushes.Black, x, y)
            g.DrawString(valor, FuenteTexto, Brushes.Black, New RectangleF(x + AnchoRotulo, y, ancho - AnchoRotulo, AltoFila))
        End Sub

        ''' <returns>Alto ocupado.</returns>
        Private Shared Function Parrafo(g As Graphics, texto As String, x As Integer, y As Integer, ancho As Integer) As Integer
            Dim alto = Math.Max(AltoFila, CInt(Math.Ceiling(g.MeasureString(texto, FuenteTexto, ancho).Height)) + 4)
            g.DrawString(texto, FuenteTexto, Brushes.Black, New RectangleF(x, y, ancho, alto))
            Return alto
        End Function

        ''' <summary>Solo pinta las filas que caben. La página siguiente sigue en <see cref="_siguiente"/>.</summary>
        Private Function TablaAsiento(g As Graphics, area As Rectangle, inicio As Integer) As Integer
            Dim anchoMonto = 110
            Dim xCuenta = area.Left
            Dim xDescripcion = area.Left + 130
            Dim xDebe = area.Right - anchoMonto * 2
            Dim xHaber = area.Right - anchoMonto
            Dim derecha As New StringFormat With {.Alignment = StringAlignment.Far}
            Dim y = inicio
            g.DrawLine(Pens.Black, area.Left, y, area.Right, y)
            g.DrawString("# Cuenta", FuenteRotulo, Brushes.Black, xCuenta, y + 3)
            g.DrawString("Descripción", FuenteRotulo, Brushes.Black, xDescripcion, y + 3)
            g.DrawString("Debe", FuenteRotulo, Brushes.Black, New RectangleF(xDebe, y + 3, anchoMonto, AltoFila), derecha)
            g.DrawString("Haber", FuenteRotulo, Brushes.Black, New RectangleF(xHaber, y + 3, anchoMonto, AltoFila), derecha)
            y += AltoFila + 4
            g.DrawLine(Pens.Black, area.Left, y, area.Right, y)
            y += 4
            Dim limite = area.Bottom - 110
            While _siguiente < _comprobante.Lineas.Count AndAlso y + AltoFila <= limite
                Dim linea = _comprobante.Lineas(_siguiente)
                g.DrawString(linea.Cuenta, FuenteTexto, Brushes.Black, xCuenta, y)
                g.DrawString(linea.Descripcion, FuenteTexto, Brushes.Black, New RectangleF(xDescripcion, y, xDebe - xDescripcion, AltoFila))
                g.DrawString(linea.Debe.ToString("#,##0.00"), FuenteTexto, Brushes.Black, New RectangleF(xDebe, y, anchoMonto, AltoFila), derecha)
                g.DrawString(linea.Haber.ToString("#,##0.00"), FuenteTexto, Brushes.Black, New RectangleF(xHaber, y, anchoMonto, AltoFila), derecha)
                y += AltoFila
                _siguiente += 1
            End While
            Return y
        End Function

        Private Shared Sub Firmas(g As Graphics, area As Rectangle, inicio As Integer)
            Dim y = inicio + 60
            Dim ancho = area.Width \ 2 - 40
            For Each firma In {(Texto:="Hecho por", X:=area.Left), (Texto:="Revisado por", X:=area.Right - ancho)}
                g.DrawLine(Pens.Black, firma.X, y, firma.X + ancho, y)
                g.DrawString(firma.Texto, FuenteTexto, Brushes.Black, firma.X, y + 3)
            Next
        End Sub

        Private Shared Sub Pie(g As Graphics, area As Rectangle)
            Dim y = area.Bottom - 14
            g.DrawString($"{Tema.Producto} (CCB)", FuentePie, Brushes.Black, area.Left, y)
            g.DrawString(Date.Now.ToString("dd/MMM/yyyy - HH:mm:ss"), FuentePie, Brushes.Black, New RectangleF(area.Left, y, area.Width, 14), New StringFormat With {.Alignment = StringAlignment.Far})
        End Sub

    End Class

End Namespace
