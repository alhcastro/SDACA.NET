Imports System.Drawing.Printing
Imports SDACA.Core.Bancos
Imports SDACA.Ui

Namespace SD1401

    ''' <summary>
    ''' Conciliación Bancaria: REP\RPT0004.Dsr con sus dos subreportes RPT0005. Las líneas se arman una vez y
    ''' se reparten por páginas. Como allá, Débitos son los movimientos de tipo 0 (cheques y salidas).
    ''' </summary>
    Public NotInheritable Class InformeConciliacionImpreso
        Inherits PrintDocument

        Private Const Titulo As String = "Conciliación Bancaria"
        Private Const AltoFila As Integer = 16

        Private Shared ReadOnly FuenteEmpresa As New Font("Arial", 12, FontStyle.Bold)
        Private Shared ReadOnly FuenteTitulo As New Font("Arial", 9)
        Private Shared ReadOnly FuenteTexto As New Font("Arial", 8)
        Private Shared ReadOnly FuenteRotulo As New Font("Arial", 8, FontStyle.Bold)
        Private Shared ReadOnly FuenteSeccion As New Font("Arial", 9.5F, FontStyle.Bold)

        Private ReadOnly _informe As InformeConciliacion
        Private ReadOnly _empresa As String
        Private ReadOnly _subtitulo As String
        Private ReadOnly _conTransito As Boolean
        Private ReadOnly _lineas As New List(Of Action(Of Graphics, Rectangle, Integer))
        Private _siguiente As Integer
        Private _pagina As Integer

        Public Sub New(informe As InformeConciliacion, empresa As String, subtitulo As String, conTransito As Boolean)
            _informe = informe
            _empresa = empresa
            _subtitulo = subtitulo
            _conTransito = conTransito
            DocumentName = Titulo
            DefaultPageSettings.Landscape = True
            DefaultPageSettings.Margins = New Margins(45, 45, 45, 45)
            Armar()
        End Sub

        Private Shared Function Monto(valor As Double) As String
            Return valor.ToString("#,##0.00")
        End Function

        Private Shared Function Fecha(valor As Date) As String
            Return If(valor.Year <= 1900, "", valor.ToString("dd/MMM/yyyy"))
        End Function

        Private Sub Armar()
            Dim d = _informe.Datos
            _lineas.Add(Sub(g, a, y) Texto(g, "Saldo Inicial:", FuenteRotulo, a.Left, y, 120, False, Monto(d.MontoInicial)))
            _lineas.Add(AddressOf EncabezadoTabla)
            For Each fila In d.Filas
                Dim f = fila
                _lineas.Add(Sub(g, a, y) FilaTabla(g, a, y, f))
            Next
            _lineas.Add(AddressOf TotalesTabla)
            _lineas.Add(Sub(g, a, y) Nada())
            AgregarResumen()
            If _conTransito Then
                AgregarTransito($"Cheques en Tránsito al {d.Fin:dd - MMMM - yyyy}", _informe.ChequesTransito)
                AgregarTransito($"Depósitos en Tránsito al {d.Fin:dd - MMMM - yyyy}", _informe.DepositosTransito)
            End If
        End Sub

        Private Shared Sub Nada()
        End Sub

        Private Sub AgregarResumen()
            Dim i = _informe
            Dim d = i.Datos
            Dim columnas = {
                ("Resumen Movimientos", New(String, Double)() {("Monto Inicial:", d.MontoInicial), ("Créditos Conciliados:", i.CreditosConciliados), ("Débitos Conciliados:", i.DebitosConciliados),
                                                                 ("Créditos No Conc.:", d.DepositosActuales), ("Débitos No Conc.:", d.ChequesActuales), ("Saldo en Libros:", i.SaldoLibros)}),
                ("Saldo en Bancos", New(String, Double)() {("Saldo en Libros:", i.SaldoLibros), ("Cré. en Tránsito:", i.ChequesTotal), ("Déb. en Tránsito:", -i.DepositosTotal), ("Saldo en Banco:", i.SaldoBanco)}),
                ("Resumen Cheques en Tránsito", New(String, Double)() {("Anteriores:", d.ChequesAnteriores), ("Aplicados:", d.ChequesAplicados), ("Actuales:", d.ChequesActuales), ("Total:", i.ChequesTotal)}),
                ("Resumen Depósitos en Tránsito", New(String, Double)() {("Anteriores:", d.DepositosAnteriores), ("Aplicados:", d.DepositosAplicados), ("Actuales:", d.DepositosActuales), ("Total:", i.DepositosTotal)})
            }
            _lineas.Add(Sub(g, a, y)
                            For c = 0 To columnas.Length - 1
                                g.DrawString(columnas(c).Item1, FuenteRotulo, Brushes.Black, a.Left + c * (a.Width \ 4), y)
                            Next
                        End Sub)
            For n = 0 To 5
                Dim fila = n
                _lineas.Add(Sub(g, a, y)
                                For c = 0 To columnas.Length - 1
                                    If fila >= columnas(c).Item2.Length Then Continue For
                                    Dim par = columnas(c).Item2(fila)
                                    Texto(g, par.Item1, FuenteTexto, a.Left + c * (a.Width \ 4), y, a.Width \ 4 - 14, True, Monto(par.Item2))
                                Next
                            End Sub)
            Next
        End Sub

        Private Sub AgregarTransito(subtitulo As String, filas As List(Of FilaConciliacion))
            _lineas.Add(Sub(g, a, y) Nada())
            _lineas.Add(Sub(g, a, y) g.DrawString(subtitulo, FuenteSeccion, Brushes.Black, a.Left, y))
            _lineas.Add(Sub(g, a, y) FilaTransito(g, a, y, "Fecha", "# Documento", "Detalle", "Monto", FuenteRotulo))
            For Each fila In filas
                Dim f = fila
                _lineas.Add(Sub(g, a, y) FilaTransito(g, a, y, Fecha(f.Fecha), f.Documento, f.Detalle, Monto(f.Monto), FuenteTexto))
            Next
            Dim total = filas.Sum(Function(f) f.Monto)
            _lineas.Add(Sub(g, a, y) FilaTransito(g, a, y, "", "", $"Total ({filas.Count})", Monto(total), FuenteRotulo))
        End Sub

        Private Shared Sub FilaTransito(g As Graphics, a As Rectangle, y As Integer, fechaTexto As String, documento As String, detalle As String, valor As String, fuente As Font)
            g.DrawString(fechaTexto, fuente, Brushes.Black, a.Left, y)
            g.DrawString(documento, fuente, Brushes.Black, a.Left + 90, y)
            Recorte(g, detalle, fuente, a.Left + 200, y, a.Width - 330)
            Derecha(g, valor, fuente, a.Right - 120, y, 120)
        End Sub

        ''' <summary>Posiciones de las columnas de la tabla principal.</summary>
        Private Shared Function Columnas(a As Rectangle) As Integer()
            Return {a.Left, a.Left + 70, a.Left + 300, a.Left + 370, a.Right - 320, a.Right - 240, a.Right - 145, a.Right - 50}
        End Function

        Private Shared Sub EncabezadoTabla(g As Graphics, a As Rectangle, y As Integer)
            Dim c = Columnas(a)
            g.DrawString("Fecha", FuenteRotulo, Brushes.Black, c(0), y)
            g.DrawString("Detalle en Libros", FuenteRotulo, Brushes.Black, c(1), y)
            g.DrawString("Fecha", FuenteRotulo, Brushes.Black, c(2), y)
            g.DrawString("Detalle del Banco", FuenteRotulo, Brushes.Black, c(3), y)
            g.DrawString("# Documento", FuenteRotulo, Brushes.Black, c(4), y)
            Derecha(g, "Débitos", FuenteRotulo, c(5), y, 90)
            Derecha(g, "Créditos", FuenteRotulo, c(6), y, 90)
            g.DrawString("Conc.", FuenteRotulo, Brushes.Black, c(7), y)
            g.DrawLine(Pens.Black, a.Left, y + AltoFila - 1, a.Right, y + AltoFila - 1)
        End Sub

        Private Shared Sub FilaTabla(g As Graphics, a As Rectangle, y As Integer, f As FilaConciliacion)
            Dim c = Columnas(a)
            Dim soloBanco = f.Detalle.Trim().Length = 0 AndAlso f.BancoDetalle.Trim().Length > 0
            Dim soloLibro = f.Detalle.Trim().Length > 0 AndAlso f.BancoDetalle.Trim().Length = 0
            If Not soloBanco Then g.DrawString(Fecha(f.Fecha), FuenteTexto, Brushes.Black, c(0), y)
            Recorte(g, f.Detalle, FuenteTexto, c(1), y, c(2) - c(1) - 6)
            If Not soloLibro Then g.DrawString(Fecha(f.BancoFecha), FuenteTexto, Brushes.Black, c(2), y)
            Recorte(g, f.BancoDetalle, FuenteTexto, c(3), y, c(4) - c(3) - 6)
            Recorte(g, f.Documento, FuenteTexto, c(4), y, c(5) - c(4))
            Derecha(g, If(f.Tipo = 0, Monto(f.Monto), ""), FuenteTexto, c(5), y, 90)
            Derecha(g, If(f.Tipo = 0, "", Monto(f.Monto)), FuenteTexto, c(6), y, 90)
            If f.Conciliado Then g.DrawString("SI", FuenteTexto, Brushes.Black, c(7), y)
        End Sub

        Private Sub TotalesTabla(g As Graphics, a As Rectangle, y As Integer)
            Dim c = Columnas(a)
            g.DrawLine(Pens.Black, c(4), y, a.Right, y)
            g.DrawString("Totales", FuenteRotulo, Brushes.Black, c(4), y + 2)
            Derecha(g, Monto(_informe.TotalDebitos), FuenteRotulo, c(5), y + 2, 90)
            Derecha(g, Monto(_informe.TotalCreditos), FuenteRotulo, c(6), y + 2, 90)
        End Sub

        Private Shared Sub Texto(g As Graphics, rotulo As String, fuente As Font, x As Integer, y As Integer, ancho As Integer, valorAlFinal As Boolean, valor As String)
            g.DrawString(rotulo, fuente, Brushes.Black, x, y)
            If valorAlFinal Then
                Derecha(g, valor, FuenteTexto, x, y, ancho)
            Else
                g.DrawString(valor, FuenteTexto, Brushes.Black, x + ancho, y)
            End If
        End Sub

        Private Shared Sub Derecha(g As Graphics, texto As String, fuente As Font, x As Integer, y As Integer, ancho As Integer)
            g.DrawString(texto, fuente, Brushes.Black, New RectangleF(x, y, ancho, AltoFila), New StringFormat With {.Alignment = StringAlignment.Far, .FormatFlags = StringFormatFlags.NoWrap})
        End Sub

        Private Shared Sub Recorte(g As Graphics, texto As String, fuente As Font, x As Integer, y As Integer, ancho As Integer)
            g.DrawString(UnaLinea(texto), fuente, Brushes.Black, New RectangleF(x, y, ancho, AltoFila), New StringFormat With {.Trimming = StringTrimming.EllipsisCharacter, .FormatFlags = StringFormatFlags.NoWrap})
        End Sub

        ''' <summary>El detalle del libro suele traer nombre y detalle en dos líneas.</summary>
        Friend Shared Function UnaLinea(texto As String) As String
            Return String.Join(" ", If(texto, "").Split({ControlChars.Cr, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries))
        End Function

        Protected Overrides Sub OnBeginPrint(e As PrintEventArgs)
            MyBase.OnBeginPrint(e)
            _siguiente = 0
            _pagina = 0
        End Sub

        Protected Overrides Sub OnPrintPage(e As PrintPageEventArgs)
            MyBase.OnPrintPage(e)
            _pagina += 1
            Dim g = e.Graphics
            Dim a = e.MarginBounds
            LogoEmpresa.Dibujar(g, New Rectangle(a.Left, a.Top, 170, 28))
            Dim derecha As New StringFormat With {.Alignment = StringAlignment.Far}
            g.DrawString(_empresa, FuenteEmpresa, Brushes.Black, New RectangleF(a.Left + 180, a.Top, a.Width - 180, 20), derecha)
            g.DrawString(_subtitulo, FuenteTitulo, Brushes.Black, New RectangleF(a.Left + 180, a.Top + 19, a.Width - 180, 18), derecha)
            Dim y = a.Top + 40
            g.DrawLine(Pens.Black, a.Left, y, a.Right, y)
            y += 6
            Dim limite = a.Bottom - 20
            While _siguiente < _lineas.Count AndAlso y + AltoFila <= limite
                _lineas(_siguiente)(g, a, y)
                y += AltoFila
                _siguiente += 1
            End While
            g.DrawString($"{Tema.Producto} (CCB) - {Date.Now:dd/MMM/yyyy HH:mm}", FuenteTexto, Brushes.Black, a.Left, a.Bottom - 14)
            g.DrawString($"Pág. {_pagina}", FuenteTexto, Brushes.Black, New RectangleF(a.Left, a.Bottom - 14, a.Width, 14), derecha)
            e.HasMorePages = _siguiente < _lineas.Count
        End Sub

        ''' <summary>Abre la vista previa, como FrmReporte.</summary>
        Public Shared Sub Mostrar(dueno As IWin32Window, documento As PrintDocument)
            Try
                Using vista As New PrintPreviewDialog With {.Document = documento, .UseAntiAlias = True, .WindowState = FormWindowState.Maximized}
                    vista.ShowDialog(dueno)
                End Using
            Catch ex As InvalidPrinterException
                MessageBox.Show(dueno, "No hay una impresora disponible para mostrar el informe.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
        End Sub

    End Class

End Namespace
