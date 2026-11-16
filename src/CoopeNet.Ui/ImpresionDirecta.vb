Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' Envía texto crudo a una impresora, sin controlador de por medio: SD_ImpAPI de Comun\SD_MOD_UTILES01.bas. Es lo
''' que usan las impresoras de tiquete, que entienden los códigos de control de los formatos (letra, corte, cajón).
''' </summary>
Public Module ImpresionDirecta

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Private Structure DOCINFOW
        <MarshalAs(UnmanagedType.LPWStr)> Public pDocName As String
        <MarshalAs(UnmanagedType.LPWStr)> Public pOutputFile As String
        <MarshalAs(UnmanagedType.LPWStr)> Public pDatatype As String
    End Structure

    <DllImport("winspool.drv", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Function OpenPrinterW(pPrinterName As String, ByRef phPrinter As IntPtr, pDefault As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", SetLastError:=True)>
    Private Function ClosePrinter(hPrinter As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Function StartDocPrinterW(hPrinter As IntPtr, level As Integer, ByRef pDocInfo As DOCINFOW) As Integer
    End Function

    <DllImport("winspool.drv", SetLastError:=True)>
    Private Function EndDocPrinter(hPrinter As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", SetLastError:=True)>
    Private Function StartPagePrinter(hPrinter As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", SetLastError:=True)>
    Private Function EndPagePrinter(hPrinter As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", SetLastError:=True)>
    Private Function WritePrinter(hPrinter As IntPtr, pBytes As Byte(), dwCount As Integer, ByRef dwWritten As Integer) As Boolean
    End Function

    ''' <summary>
    ''' SD_TrunckPrint con corte: cambia la Ñ por N (las impresoras de tiquete no la tienen), agrega las líneas en
    ''' blanco antes del corte (una más para Samsung) y el comando de corte. Devuelve False si la impresora no existe.
    ''' </summary>
    Public Function ImprimirTiquete(texto As String, impresora As String, Optional tipo As String = "", Optional cortar As Boolean = True) As Boolean
        Dim t = If(texto, "").Replace("Ñ", "N").Replace("ñ", "n") & vbCrLf
        If cortar Then
            t &= String.Concat(Enumerable.Repeat(" " & vbCrLf, If(tipo = "SAMSUNG", 7, 6))) & ChrW(27) & "i" & vbCrLf
        End If
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        Return EnviarCrudo(Encoding.GetEncoding(1252).GetBytes(t), impresora, "Factura")
    End Function

    ''' <summary>OpenPrinter, StartDocPrinter con tipo RAW, WritePrinter.</summary>
    Public Function EnviarCrudo(datos As Byte(), impresora As String, documento As String) As Boolean
        Dim h As IntPtr
        If Not OpenPrinterW(impresora, h, IntPtr.Zero) Then Return False
        Try
            Dim info As New DOCINFOW With {.pDocName = documento, .pDatatype = "RAW"}
            If StartDocPrinterW(h, 1, info) = 0 Then Return False
            Try
                StartPagePrinter(h)
                Dim escritos As Integer
                WritePrinter(h, datos, datos.Length, escritos)
                EndPagePrinter(h)
            Finally
                EndDocPrinter(h)
            End Try
            Return True
        Finally
            ClosePrinter(h)
        End Try
    End Function

End Module
