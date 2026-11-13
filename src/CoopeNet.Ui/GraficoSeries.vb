Imports System.Drawing.Drawing2D
Imports System.Globalization

''' <summary>Las formas de MSChart que usaban las pantallas de VB6, en dos dimensiones.</summary>
Public Enum TipoGrafico
    Barras
    Lineas
    Area
    Gradas
    ''' <summary>La primera serie en barras y la segunda en línea.</summary>
    Combinacion
End Enum

''' <summary>
''' Gráfico de dos series por categoría (por ejemplo, compras y ventas por mes). Reemplaza al MSChart de VB6
''' para lo que usaban las pantallas: barras, líneas, área, gradas y combinación, con leyenda y escala.
''' </summary>
Public Class GraficoSeries
    Inherits Control

    Private _tipo As TipoGrafico = TipoGrafico.Barras
    Private _etiquetas As IReadOnlyList(Of String) = Array.Empty(Of String)()
    Private _series As IReadOnlyList(Of IReadOnlyList(Of Double)) = Array.Empty(Of IReadOnlyList(Of Double))()
    Private _nombres As IReadOnlyList(Of String) = Array.Empty(Of String)()

    ''' <summary>Se lee al pintar, para seguir el modo claro u oscuro.</summary>
    Private Shared ReadOnly Property Colores As Color()
        Get
            Return {Tema.Acento, ColorTranslator.FromHtml("#D97706")}
        End Get
    End Property

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or ControlStyles.UserPaint, True)
        BackColor = Tema.Superficie
    End Sub

    Public Property Tipo As TipoGrafico
        Get
            Return _tipo
        End Get
        Set(value As TipoGrafico)
            _tipo = value
            Invalidate()
        End Set
    End Property

    ''' <summary>Las categorías del eje horizontal y una lista de valores por serie, del mismo largo.</summary>
    Public Sub Mostrar(etiquetas As IReadOnlyList(Of String), nombres As IReadOnlyList(Of String), ParamArray series As IReadOnlyList(Of Double)())
        _etiquetas = etiquetas
        _nombres = nombres
        _series = series
        Invalidate()
    End Sub

    Public Sub Limpiar()
        Mostrar(Array.Empty(Of String)(), Array.Empty(Of String)())
    End Sub

    ''' <summary>Un tope redondo para la escala: 1, 2 o 5 por una potencia de diez.</summary>
    Friend Shared Function Tope(maximo As Double) As Double
        If maximo <= 0 Then Return 1
        Dim potencia = Math.Pow(10, Math.Floor(Math.Log10(maximo)))
        For Each f In {1.0, 2.0, 2.5, 5.0, 10.0}
            If f * potencia >= maximo Then Return f * potencia
        Next
        Return 10 * potencia
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(BackColor)
        Dim n = _etiquetas.Count
        If n = 0 OrElse _series.Count = 0 Then Return

        Dim valores = _series.SelectMany(Function(s) s).ToList()
        Dim minimo = Math.Min(0, valores.Min())
        Dim maximo = Tope(Math.Max(valores.Max(), 0))
        If minimo < 0 Then minimo = -Tope(-minimo)

        Using fuente As New Font(Font.FontFamily, Font.Size * 0.9F), lapizEje As New Pen(Tema.BordeFuerte), lapizGuia As New Pen(Tema.Borde) With {.DashStyle = DashStyle.Dot}
            Dim formato = If(maximo >= 100, "N0", "N1")
            Dim anchoEtiqueta = CInt(Math.Ceiling(Math.Max(g.MeasureString(maximo.ToString(formato, CultureInfo.CurrentCulture), fuente).Width, g.MeasureString(minimo.ToString(formato, CultureInfo.CurrentCulture), fuente).Width)))
            Dim altoTexto = CInt(Math.Ceiling(fuente.GetHeight(g)))
            Dim area = New Rectangle(anchoEtiqueta + 12, altoTexto + 14, Width - anchoEtiqueta - 24, Height - altoTexto * 2 - 30)
            If area.Width < 20 OrElse area.Height < 20 Then Return
            Dim Y = Function(v As Double) CSng(area.Bottom - (v - minimo) / (maximo - minimo) * area.Height)

            ' Escala con cinco divisiones.
            For i = 0 To 5
                Dim v = minimo + (maximo - minimo) * i / 5
                Dim yy = Y(v)
                g.DrawLine(If(i = 0, lapizEje, lapizGuia), area.Left, yy, area.Right, yy)
                Dim texto = v.ToString(formato, CultureInfo.CurrentCulture)
                g.DrawString(texto, fuente, Brushes.DimGray, area.Left - 6 - g.MeasureString(texto, fuente).Width, yy - altoTexto / 2.0F)
            Next
            Dim cero = Y(0)
            g.DrawLine(lapizEje, area.Left, cero, area.Right, cero)

            Dim ancho = area.Width / CSng(n)
            For i = 0 To n - 1
                Dim etiqueta = _etiquetas(i)
                Dim t = g.MeasureString(etiqueta, fuente)
                g.DrawString(etiqueta, fuente, Brushes.DimGray, area.Left + ancho * i + (ancho - t.Width) / 2, area.Bottom + 4)
            Next

            Dim cantidad = _series.Count
            For s = 0 To cantidad - 1
                Dim serie = _series(s)
                Dim tono = Colores(s Mod Colores.Length)
                Dim forma = If(_tipo = TipoGrafico.Combinacion, If(s = 0, TipoGrafico.Barras, TipoGrafico.Lineas), _tipo)
                Dim centros = Enumerable.Range(0, n).Select(Function(i) New PointF(area.Left + ancho * (i + 0.5F), Y(If(i < serie.Count, serie(i), 0)))).ToArray()
                Select Case forma
                    Case TipoGrafico.Barras
                        Dim barras = If(_tipo = TipoGrafico.Combinacion, 1, cantidad)
                        Dim posicion = If(_tipo = TipoGrafico.Combinacion, 0, s)
                        Dim grueso = ancho * 0.7F / barras
                        Using pincel As New SolidBrush(tono)
                            For i = 0 To n - 1
                                Dim x = area.Left + ancho * i + ancho * 0.15F + grueso * posicion
                                Dim arriba = Math.Min(centros(i).Y, cero)
                                g.FillRectangle(pincel, x, arriba, grueso - 1, Math.Abs(centros(i).Y - cero))
                            Next
                        End Using
                    Case TipoGrafico.Lineas
                        Using lapiz As New Pen(tono, 2.5F), pincel As New SolidBrush(tono)
                            If n > 1 Then g.DrawLines(lapiz, centros)
                            For Each p In centros
                                g.FillEllipse(pincel, p.X - 3, p.Y - 3, 6, 6)
                            Next
                        End Using
                    Case TipoGrafico.Area
                        Dim puntos = {New PointF(centros(0).X, cero)}.Concat(centros).Concat({New PointF(centros(n - 1).X, cero)}).ToArray()
                        Using pincel As New SolidBrush(Color.FromArgb(90, tono)), lapiz As New Pen(tono, 2)
                            g.FillPolygon(pincel, puntos)
                            If n > 1 Then g.DrawLines(lapiz, centros)
                        End Using
                    Case TipoGrafico.Gradas
                        Dim puntos As New List(Of PointF)
                        For i = 0 To n - 1
                            puntos.Add(New PointF(area.Left + ancho * i, centros(i).Y))
                            puntos.Add(New PointF(area.Left + ancho * (i + 1), centros(i).Y))
                        Next
                        Using lapiz As New Pen(tono, 2.5F)
                            g.DrawLines(lapiz, puntos.ToArray())
                        End Using
                End Select
            Next

            ' Leyenda arriba a la derecha.
            Dim xl = area.Right
            For s = cantidad - 1 To 0 Step -1
                Dim nombre = If(s < _nombres.Count, _nombres(s), $"Serie {s + 1}")
                Dim w = g.MeasureString(nombre, fuente).Width
                xl -= CInt(w) + 22
                Using pincel As New SolidBrush(Colores(s Mod Colores.Length))
                    g.FillRectangle(pincel, xl, 6, 12, 12)
                End Using
                g.DrawString(nombre, fuente, Brushes.DimGray, xl + 16, 4)
            Next
        End Using
    End Sub

End Class
