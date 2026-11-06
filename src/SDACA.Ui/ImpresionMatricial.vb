Imports System.ComponentModel
Imports System.Drawing.Printing
Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' Salidas del texto para impresora matricial. A la impresora va tal cual, como SD_ImpAPI. Al PDF se
''' dibuja interpretando los códigos ESC/P de las Epson FX, para respetar columnas y tamaños de letra.
''' </summary>
Public NotInheritable Class ImpresionMatricial

    Private Const ImpresoraPdf As String = "Microsoft Print to PDF"
    Private Const LineasPorPulgada As Single = 6
    Private Const MargenPuntos As Single = 18

    Private Sub New()
    End Sub

    ''' <param name="impresora">Vacío: la predeterminada de Windows.</param>
    Public Shared Sub Enviar(texto As String, impresora As String)
        Dim nombre = If(String.IsNullOrEmpty(impresora), New PrinterSettings().PrinterName, impresora)
        Dim datos = Encoding.Latin1.GetBytes(texto & vbCrLf)
        Dim manejador As IntPtr
        If Not OpenPrinter(nombre, manejador, IntPtr.Zero) Then Throw New Win32Exception(Marshal.GetLastWin32Error(), $"La Impresora {nombre} no es reconocida")
        Try
            Dim documento As New DocInfo With {.Nombre = "Cheque", .Tipo = "RAW"}
            If StartDocPrinter(manejador, 1, documento) = 0 Then Throw New Win32Exception(Marshal.GetLastWin32Error())
            Try
                If Not StartPagePrinter(manejador) Then Throw New Win32Exception(Marshal.GetLastWin32Error())
                Dim escritos As Integer
                If Not WritePrinter(manejador, datos, datos.Length, escritos) Then Throw New Win32Exception(Marshal.GetLastWin32Error())
                EndPagePrinter(manejador)
            Finally
                EndDocPrinter(manejador)
            End Try
        Finally
            ClosePrinter(manejador)
        End Try
    End Sub

    Public Shared Sub GuardarPdf(texto As String, ruta As String)
        GuardarPdf({texto}, ruta)
    End Sub

    ''' <summary>Un solo PDF, con cada texto en su propia página.</summary>
    Public Shared Sub GuardarPdf(textos As IEnumerable(Of String), ruta As String)
        Dim lineas As New List(Of List(Of Tramo))
        For Each texto In textos
            If lineas.Count > 0 Then lineas.Add(Nothing)
            lineas.AddRange(Interpretar(texto))
        Next
        Dim siguiente = 0
        Using documento As New PrintDocument()
            documento.PrinterSettings.PrinterName = ImpresoraPdf
            If Not documento.PrinterSettings.IsValid Then Throw New InvalidOperationException($"No está instalada la impresora {ImpresoraPdf}.")
            documento.PrinterSettings.PrintToFile = True
            documento.PrinterSettings.PrintFileName = ruta
            documento.PrintController = New StandardPrintController()
            documento.DocumentName = "Cheque"
            AddHandler documento.PrintPage, Sub(s, e)
                                                e.Graphics.PageUnit = GraphicsUnit.Point
                                                Dim alto = e.PageSettings.PaperSize.Height / 100.0F * 72
                                                Dim y = MargenPuntos
                                                Dim paso = 72 / LineasPorPulgada
                                                While siguiente < lineas.Count AndAlso y + paso <= alto - MargenPuntos
                                                    If lineas(siguiente) Is Nothing Then
                                                        siguiente += 1
                                                        Exit While
                                                    End If
                                                    Dibujar(e.Graphics, lineas(siguiente), y)
                                                    y += paso
                                                    siguiente += 1
                                                End While
                                                e.HasMorePages = siguiente < lineas.Count
                                            End Sub
            documento.Print()
        End Using
    End Sub

    ''' <summary>Un tramo con la misma letra.</summary>
    Public NotInheritable Class Tramo
        Public Property Texto As String = ""
        ''' <summary>Valor de ESC ! n: 1 = 12 cpi, 4 = condensada, 8 = negrita, 32 = doble ancho.</summary>
        Public Property Modo As Integer
    End Class

    ''' <summary>Separa el texto en líneas y tramos, y descarta los comandos que no dibujan nada.</summary>
    Public Shared Function Interpretar(texto As String) As List(Of List(Of Tramo))
        Dim lineas As New List(Of List(Of Tramo))
        Dim actual As New List(Of Tramo)
        Dim tramo As New StringBuilder()
        Dim modo = 0
        Dim cerrar = Sub()
                         If tramo.Length > 0 Then actual.Add(New Tramo With {.Texto = tramo.ToString(), .Modo = modo})
                         tramo.Clear()
                     End Sub
        Dim i = 0
        While i < texto.Length
            Dim c = texto(i)
            If c = ChrW(27) AndAlso i + 1 < texto.Length Then
                Select Case texto(i + 1)
                    Case "!"c
                        cerrar()
                        If i + 2 < texto.Length Then modo = AscW(texto(i + 2))
                        i += 3
                    Case "i"c : i += 2
                    Case "K"c, "e"c : i += 3
                    Case "p"c : i += 5
                    Case Else : i += 2
                End Select
                Continue While
            End If
            If c = vbLf Then
                cerrar()
                lineas.Add(actual)
                actual = New List(Of Tramo)
            ElseIf c <> vbCr Then
                tramo.Append(c)
            End If
            i += 1
        End While
        cerrar()
        If actual.Count > 0 Then lineas.Add(actual)
        Return lineas
    End Function

    Public Shared Function CaracteresPorPulgada(modo As Integer) As Single
        Dim cpi = If((modo And 4) <> 0, 17.16F, If((modo And 1) <> 0, 12.0F, 10.0F))
        Return If((modo And 32) <> 0, cpi / 2, cpi)
    End Function

    Private Shared Sub Dibujar(g As Graphics, linea As List(Of Tramo), y As Single)
        Dim x = MargenPuntos
        For Each t In linea
            Dim ancho = 72 / CaracteresPorPulgada(t.Modo)
            ' El ancho de Courier New es 0.6 del tamaño de la letra.
            Using letra As New Font("Courier New", ancho / 0.6F, If((t.Modo And 8) <> 0, FontStyle.Bold, FontStyle.Regular), GraphicsUnit.Point)
                For Each c In t.Texto
                    If c <> " "c Then g.DrawString(c.ToString(), letra, Brushes.Black, x, y, StringFormat.GenericTypographic)
                    x += ancho
                Next
            End Using
        Next
    End Sub

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Private Class DocInfo
        <MarshalAs(UnmanagedType.LPWStr)> Public Nombre As String
        <MarshalAs(UnmanagedType.LPWStr)> Public Salida As String
        <MarshalAs(UnmanagedType.LPWStr)> Public Tipo As String
    End Class

    <DllImport("winspool.drv", EntryPoint:="OpenPrinterW", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function OpenPrinter(nombre As String, ByRef manejador As IntPtr, valores As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", EntryPoint:="StartDocPrinterW", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function StartDocPrinter(manejador As IntPtr, nivel As Integer, <[In]> documento As DocInfo) As Integer
    End Function

    <DllImport("winspool.drv", SetLastError:=True)>
    Private Shared Function StartPagePrinter(manejador As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", SetLastError:=True)>
    Private Shared Function WritePrinter(manejador As IntPtr, datos As Byte(), cantidad As Integer, ByRef escritos As Integer) As Boolean
    End Function

    <DllImport("winspool.drv")>
    Private Shared Function EndPagePrinter(manejador As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv")>
    Private Shared Function EndDocPrinter(manejador As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv")>
    Private Shared Function ClosePrinter(manejador As IntPtr) As Boolean
    End Function

End Class
