Imports System.Drawing
Imports System.Drawing.Printing
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Etiqueta pequeña del artículo (botón Etiqueta de frmMantArticulos, que imprime frmEtiqPeq4 con PrintForm):
    ''' descripción, código y el código en Code 39. Las medidas son las del formulario de VB6, con su corrimiento
    ''' de 300 twips, pasadas a centésimas de pulgada. VB6 armaba las barras con la fuente "Código de Barras 3/9";
    ''' aquí se dibujan, así que no hace falta la fuente en el equipo.
    ''' </summary>
    Public NotInheritable Class EtiquetaArticulo
        Inherits PrintDocument

        ''' <summary>Twips a centésimas de pulgada.</summary>
        Private Shared Function T(twips As Integer) As Single
            Return twips / 14.4F
        End Function

        Private Shared ReadOnly FuenteDescripcion As New Font("Calibri", 12.0F, FontStyle.Bold, GraphicsUnit.Point)
        Private Shared ReadOnly FuenteCodigo As New Font("Calibri", 18.0F, FontStyle.Bold, GraphicsUnit.Point)

        Private ReadOnly _codigo As String
        Private ReadOnly _descripcion As String

        Public Sub New(codigo As String, descripcion As String)
            _codigo = If(codigo, "").Trim()
            _descripcion = If(descripcion, "").Trim()
            DocumentName = $"Etiqueta {_codigo}"
            OriginAtMargins = False
        End Sub

        ''' <summary>
        ''' Ancho de las barras, en centésimas de pulgada: la fuente "Código de Barras 3/9" a 21.75 puntos ocupa
        ''' 252 twips por carácter, asteriscos incluidos. lblPUMNormal medía 4200 twips y cortaba lo que no cabía;
        ''' aquí un código largo se angosta para que entre completo y siga leyéndose.
        ''' </summary>
        Public Shared Function AnchoBarras(codigo As String) As Single
            Return Math.Min(T(252 * (If(codigo, "").Trim().Length + 2)), T(4200))
        End Function

        ''' <summary>INI_Read(SDACADIR\CONFIG.INI, "POS", "PRINTER_ETIQ", la predeterminada), con el CONFIG.INI de CoopeNet.</summary>
        Public Shared Function Impresora(directorioDatos As String) As String
            Dim ruta = IO.Path.Combine(If(directorioDatos, ""), "CONFIG.INI")
            Return ConexionesIni.LeerValor(ruta, "POS", "PRINTER_ETIQ").Trim()
        End Function

        ''' <summary>Usa la impresora de etiquetas si está instalada; si no, la predeterminada, como VB6.</summary>
        Public Sub Elegir(impresoraEtiquetas As String)
            If impresoraEtiquetas.Length > 0 AndAlso PrinterSettings.InstalledPrinters.Cast(Of String)().Any(Function(p) String.Equals(p, impresoraEtiquetas, StringComparison.OrdinalIgnoreCase)) Then
                PrinterSettings.PrinterName = impresoraEtiquetas
            End If
        End Sub

        Protected Overrides Sub OnPrintPage(e As PrintPageEventArgs)
            MyBase.OnPrintPage(e)
            Dim g = e.Graphics
            ' El origen queda en la esquina del área imprimible, donde PrintForm ponía el formulario.
            g.PageUnit = GraphicsUnit.Display
            Dim centrado As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Near, .Trimming = StringTrimming.EllipsisWord}
            g.DrawString(_descripcion, FuenteDescripcion, Brushes.Black, New RectangleF(T(45), T(180), T(4275), T(555)), centrado)
            g.DrawString(_codigo, FuenteCodigo, Brushes.Black, New RectangleF(0, T(630), T(4335), T(435)), centrado)
            Dim ancho = AnchoBarras(_codigo)
            Dim barras = New RectangleF(T(45) + (T(4200) - ancho) / 2, T(1020), ancho, T(450))
            If Not DibujoBarras.Dibujar39(g, barras, _codigo) Then
                g.DrawString(_codigo, FuenteDescripcion, Brushes.Black, New RectangleF(T(45), T(1020), T(4200), T(510)), centrado)
            End If
            e.HasMorePages = False
        End Sub

    End Class

End Namespace
