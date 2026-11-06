Imports System.Globalization
Imports System.Text
Imports Microsoft.VisualBasic

Namespace Bancos

    ''' <summary>
    ''' Línea del archivo .ENV. Migración de SD_PrintAPI2 y SD_Obtener_Elemento de
    ''' SD1401\FrmTransferenciasBancos.frm. El banco lee este archivo por posición: cualquier cambio aquí
    ''' cambia lo que se paga. Cada elemento del formato es VARIABLE,TAMAÑO,JUSTIFICACIÓN,RELLENO,DECIMALES,TIPO
    ''' y los elementos se separan con |.
    ''' </summary>
    Public NotInheritable Class ArchivoTransferencia

        Private Const FinDeLinea As String = vbCrLf
        Private Const PaginaDeCodigos As Integer = 1252

        Private Sub New()
        End Sub

        Public Shared Function Linea(formato As String, valores As IReadOnlyDictionary(Of String, String)) As String
            Dim texto As New StringBuilder()
            For Each elemento In If(formato, "").Split("|"c)
                If elemento.Length > 0 Then texto.Append(Elemento_(elemento, valores))
            Next
            Return texto.ToString()
        End Function

        Private Shared Function Elemento_(formato As String, valores As IReadOnlyDictionary(Of String, String)) As String
            Dim partes = formato.Split(","c)
            Dim tamano = CInt(Conversion.Val(partes(1)))
            Dim justificacion = partes(2)
            Dim relleno = ChrW(CInt(partes(3)))
            Dim valor As String = Nothing
            If Not valores.TryGetValue(partes(0), valor) Then Return Ajusta("", tamano, justificacion, relleno)

            Dim texto = If(valor, "").Trim()
            Dim decimales = CInt(Conversion.Val(partes(4)))
            Select Case CInt(Conversion.Val(partes(5)))
                Case 0
                    texto = Ajustar(texto, tamano, relleno)
                Case 1
                    texto = Fecha(texto, "ddMMyyyy")
                Case 2
                    texto = Fecha(texto, "yyyyMMdd")
                Case 3
                    texto = Fecha(texto, "MMddyyyy")
                Case 4
                    texto = Numero(texto, 0)
                    relleno = " "c
                Case 5
                    texto = Numero(texto, 0)
                Case 6, 8
                    texto = Numero(texto, decimales)
                    relleno = " "c
                Case 7, 9
                    texto = Numero(texto, decimales)
            End Select
            Return Ajusta(texto, tamano, justificacion, relleno)
        End Function

        ''' <summary>Como en VB6, el valor pasa por texto antes de formatearse. Ver <see cref="Texto"/>.</summary>
        Private Shared Function Numero(texto As String, decimales As Integer) As String
            Dim redondeado = Math.Round(CDec(Conversion.Val(texto)), decimales, MidpointRounding.AwayFromZero)
            Return redondeado.ToString("F" & decimales, CultureInfo.InvariantCulture).Replace(".", "")
        End Function

        Private Shared Function Fecha(texto As String, formato As String) As String
            Return Date.Parse(texto, CultureInfo.InvariantCulture).ToString(formato, CultureInfo.InvariantCulture)
        End Function

        ''' <summary>Ajustar de Comun, sin pIzq: recorta por la derecha o rellena a la derecha.</summary>
        Friend Shared Function Ajustar(texto As String, tamano As Integer, relleno As Char) As String
            Return If(texto.Length > tamano, texto.Substring(0, tamano), texto.PadRight(tamano, relleno))
        End Function

        ''' <summary>
        ''' Ajusta de Comun. I rellena a la izquierda. Un texto más largo que el campo se recorta conservando
        ''' el final, salvo con D: 29102026 en un campo I de 6 queda 102026.
        ''' </summary>
        Friend Shared Function Ajusta(texto As String, tamano As Integer, justificacion As String, relleno As Char) As String
            If tamano <= texto.Length Then
                Return If(justificacion = "D", texto.Substring(0, tamano), texto.Substring(texto.Length - tamano))
            End If
            Select Case justificacion
                Case "D"
                    Return texto.PadRight(tamano, relleno)
                Case "C"
                    Dim lado = New String(relleno, CInt(Math.Round((tamano - texto.Length) / 2, MidpointRounding.ToEven)))
                    Dim centrado = lado & texto & lado
                    Return If(centrado.Length < tamano, relleno & centrado, centrado.Substring(0, tamano))
                Case Else
                    Return texto.PadLeft(tamano, relleno)
            End Select
        End Function

        ''' <summary>
        ''' Valor de la consulta como lo deja SD_RS2Str: un Double con 15 cifras, como CStr de VB6, y una
        ''' fecha en un formato que <see cref="Fecha"/> pueda leer.
        ''' </summary>
        Public Shared Function Texto(valor As Object) As String
            If valor Is Nothing OrElse TypeOf valor Is DBNull Then Return ""
            If TypeOf valor Is Double Then Return CDbl(valor).ToString("G15", CultureInfo.InvariantCulture)
            If TypeOf valor Is Date Then Return CDate(valor).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            Return Convert.ToString(valor, CultureInfo.InvariantCulture)
        End Function

        ''' <summary>Print # de VB6: ANSI y CRLF al final de cada línea.</summary>
        Public Shared Function Bytes(lineas As IEnumerable(Of String)) As Byte()
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
            Dim contenido As New StringBuilder()
            For Each fila As String In lineas
                contenido.Append(fila).Append(FinDeLinea)
            Next
            Return Encoding.GetEncoding(PaginaDeCodigos).GetBytes(contenido.ToString())
        End Function

        ''' <summary>Nombre sin extensión: fecha, departamento, módulo y transferencia.</summary>
        Public Shared Function NombreArchivo(hoy As Date, local As Long, modulo As Long, numero As String) As String
            Return $"{hoy.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{local}-{modulo}-{numero.Trim()}"
        End Function

    End Class

End Namespace
