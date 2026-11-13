Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Security
Imports System.Text

''' <summary>
''' Exporta una tabla como "Enviar a excel" de los REP: un libro de Excel (.xlsx) con los nombres de las columnas, o
''' un texto separado por el separador de listas de Windows, sin encabezado, como sgFormatCharSeparatedValue.
''' </summary>
Public NotInheritable Class ExportacionTabla

    Private Sub New()
    End Sub

    Public Const Filtro As String = "Libro de Excel (*.xlsx)|*.xlsx|Archivo de texto (*.txt)|*.txt"

    Public Shared Sub Guardar(tabla As DataTable, ruta As String)
        If String.Equals(Path.GetExtension(ruta), ".txt", StringComparison.OrdinalIgnoreCase) Then
            File.WriteAllText(ruta, Texto(tabla, CultureInfo.CurrentCulture.TextInfo.ListSeparator), Encoding.UTF8)
        Else
            Libro(tabla, ruta)
        End If
    End Sub

    Friend Shared Function Texto(tabla As DataTable, separador As String) As String
        Dim sb As New StringBuilder()
        For Each fila As DataRow In tabla.Rows
            sb.AppendLine(String.Join(separador, fila.ItemArray.Select(Function(v) Campo(Valor(v), separador))))
        Next
        Return sb.ToString()
    End Function

    Private Shared Function Campo(texto As String, separador As String) As String
        If texto.Contains(separador) OrElse texto.Contains(""""c) OrElse texto.Contains(vbCr) OrElse texto.Contains(vbLf) Then Return """" & texto.Replace("""", """""") & """"
        Return texto
    End Function

    Private Shared Function Valor(v As Object) As String
        If v Is Nothing OrElse TypeOf v Is DBNull Then Return ""
        If TypeOf v Is Date Then Return CDate(v).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture)
        Return Convert.ToString(v, CultureInfo.CurrentCulture)
    End Function

    Private Shared Function EsNumero(v As Object) As Boolean
        Return TypeOf v Is Byte OrElse TypeOf v Is Short OrElse TypeOf v Is Integer OrElse TypeOf v Is Long OrElse TypeOf v Is SByte OrElse
               TypeOf v Is UShort OrElse TypeOf v Is UInteger OrElse TypeOf v Is ULong OrElse TypeOf v Is Single OrElse TypeOf v Is Double OrElse TypeOf v Is Decimal
    End Function

    ''' <summary>Un .xlsx mínimo: una hoja con texto en línea y números, sin estilos.</summary>
    Friend Shared Sub Libro(tabla As DataTable, ruta As String)
        If File.Exists(ruta) Then File.Delete(ruta)
        Using zip = ZipFile.Open(ruta, ZipArchiveMode.Create)
            Parte(zip, "[Content_Types].xml",
                "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">" &
                "<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/><Default Extension=""xml"" ContentType=""application/xml""/>" &
                "<Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>" &
                "<Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/></Types>")
            Parte(zip, "_rels/.rels",
                "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
                "<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/></Relationships>")
            Parte(zip, "xl/workbook.xml",
                "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" " &
                "xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""><sheets><sheet name=""Consulta"" sheetId=""1"" r:id=""rId1""/></sheets></workbook>")
            Parte(zip, "xl/_rels/workbook.xml.rels",
                "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
                "<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/></Relationships>")
            Dim hoja As New StringBuilder()
            hoja.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""><sheetData>")
            hoja.Append("<row>")
            For Each c As DataColumn In tabla.Columns
                hoja.Append(Celda(c.ColumnName))
            Next
            hoja.Append("</row>")
            For Each fila As DataRow In tabla.Rows
                hoja.Append("<row>")
                For Each v In fila.ItemArray
                    If EsNumero(v) Then
                        hoja.Append("<c><v>").Append(Convert.ToString(v, CultureInfo.InvariantCulture)).Append("</v></c>")
                    Else
                        hoja.Append(Celda(Valor(v)))
                    End If
                Next
                hoja.Append("</row>")
            Next
            hoja.Append("</sheetData></worksheet>")
            Parte(zip, "xl/worksheets/sheet1.xml", hoja.ToString())
        End Using
    End Sub

    Private Shared Function Celda(texto As String) As String
        Return $"<c t=""inlineStr""><is><t xml:space=""preserve"">{SecurityElement.Escape(Limpio(texto))}</t></is></c>"
    End Function

    ''' <summary>XML no admite caracteres de control salvo tabulador y saltos de línea.</summary>
    Private Shared Function Limpio(texto As String) As String
        Return New String(texto.Where(Function(c) c >= " "c OrElse c = vbTab OrElse c = vbCr OrElse c = vbLf).ToArray())
    End Function

    Private Shared Sub Parte(zip As ZipArchive, nombre As String, contenido As String)
        Using s = zip.CreateEntry(nombre, CompressionLevel.Optimal).Open(), w As New StreamWriter(s, New UTF8Encoding(False))
            w.Write(contenido)
        End Using
    End Sub

End Class
