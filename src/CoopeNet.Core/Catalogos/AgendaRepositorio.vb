Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Catalogos

    Public NotInheritable Class PersonaAgenda
        Public Property Nombre As String = ""
        Public Property Id As String = ""
    End Class

    Public NotInheritable Class ContactoAgenda
        Public Property Tipo As String = ""
        Public Property Dato As String = ""
    End Class

    ''' <summary>Lo que la Agenda muestra de una persona.</summary>
    Public NotInheritable Class FichaAgenda
        Public Property Id As String = ""
        Public Property Nombre As String = ""
        Public Property Tipo As String = ""
        Public Property Provincia As String = ""
        Public Property Canton As String = ""
        Public Property Distrito As String = ""
        Public Property Sector As String = ""
        Public Property Senas As String = ""
        Public Property Contactos As New List(Of ContactoAgenda)
    End Class

    ''' <summary>Encabezado y pie de la ficha: SDACA ponía el nombre y la cédula del registro y el teléfono del local.</summary>
    Public NotInheritable Class MarcoAgenda
        Public Property Empresa As String = ""
        Public Property Cedula As String = ""
        Public Property Telefono As String = ""
        Public Property Usuario As String = ""
        Public Property Fecha As Date
    End Class

    ''' <summary>
    ''' Agenda Telefónica. Migración de SD_Cargar_Lista de SD0001\FrmAgenda.frm y de SD_Agenda_Telefonicai de
    ''' ModPrin.bas. Lista personas activas y no fallecidas, y arma la ficha en HTML con la ubicación y los
    ''' contactos de gacdptel. Solo lee. Los tipos 0 a 3 conservan el texto de VB6; los demás salen de gactiposid.
    ''' </summary>
    Public NotInheritable Class AgendaRepositorio

        Public Const Maximo As Integer = 100

        Friend Const SqlLista As String =
            "SELECT NOMBRE AS Nombre, IDENTIFICACION AS Id FROM " &
            "(SELECT UCASE(TRIM(CONCAT(D.apellido1, ' ', D.apellido2, ' ', D.nombre, " &
            "CASE " &
            "WHEN IFNULL(A.CODIGO, '') = '' AND D.alias = '' THEN '' " &
            "WHEN IFNULL(A.CODIGO, '') <> '' AND D.alias <> '' THEN CONCAT(' (', A.CODIGO, IF(A.ASOCIADO = '0', ' NO ASOCIADO', ''), ', cc. ', D.alias, ')') " &
            "WHEN IFNULL(A.CODIGO, '') = '' AND D.alias <> '' THEN CONCAT(' (', D.alias, ')') " &
            "ELSE CONCAT(' (', A.CODIGO, IF(A.ASOCIADO = '0', ' NO ASOCIADO', ''), ')') " &
            "END))) AS NOMBRE, D.id AS IDENTIFICACION " &
            "FROM gacdp D LEFT JOIN gacasociados A ON A.ID = D.id " &
            "WHERE D.fdef = '1900-01-01' AND D.activo = '1') L " &
            "WHERE NOMBRE LIKE @patron OR IDENTIFICACION LIKE @patron ORDER BY NOMBRE LIMIT 100"

        Friend Const SqlFicha As String =
            "SELECT DP.id AS Id, TRIM(UCASE(CONCAT(DP.apellido1, ' ', DP.apellido2, ' ', DP.nombre))) AS Nombre, " &
            "CASE DP.tipo WHEN '0' THEN 'FISICO' WHEN '1' THEN 'JURIDICA' WHEN '2' THEN 'FISICO' WHEN '3' THEN 'CODIGO INTERNO' ELSE IFNULL(UCASE(I.detalle), '') END AS Tipo, " &
            "IFNULL(UCASE(P.Descripcion), '') AS Provincia, IFNULL(UCASE(C.Descripcion), '') AS Canton, IFNULL(UCASE(D.Descripcion), '') AS Distrito, " &
            "IFNULL(UCASE(S.Descripcion), '') AS Sector, UCASE(TRIM(DP.dir)) AS Senas " &
            "FROM gacdp DP " &
            "LEFT JOIN gactiposid I ON I.tipo = DP.tipo " &
            "LEFT JOIN gatprovincias P ON P.Codigo = DP.prov " &
            "LEFT JOIN gatcantones C ON C.Provincia = DP.prov AND C.Codigo = DP.cant " &
            "LEFT JOIN gatdistritos D ON D.Provincia = DP.prov AND D.Canton = DP.cant AND D.Codigo = DP.dist " &
            "LEFT JOIN gatsectores S ON S.Provincia = DP.prov AND S.Canton = DP.cant AND S.Distrito = DP.dist AND S.Codigo = DP.caserio " &
            "WHERE DP.id = @id"

        Friend Const SqlContactos As String =
            "SELECT CONCAT(CAST(T.pos AS CHAR), ' - ', IFNULL(T2.descripcion, '')) AS Tipo, T.dato AS Dato " &
            "FROM gacdptel T LEFT JOIN gacdpteltipos T2 ON T2.tipo = T.tipo WHERE T.id = @id ORDER BY T.pos"

        ''' <summary>SD_NumLocObtener(cn, "local" &amp; local, 0): el primer teléfono de trabajo del local.</summary>
        Friend Const SqlTelefonoLocal As String = "SELECT dato FROM gacdptel WHERE id = @id AND tipo = 0 ORDER BY pos LIMIT 1"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>Como VB6: vacío busca "A", y cada espacio separa partes que deben aparecer en ese orden.</summary>
        Friend Shared Function Patron(texto As String) As String
            Dim limpio = Regex.Replace(If(texto, "").Trim(), " +", " ")
            If limpio.Length = 0 Then limpio = "A"
            Return $"%{limpio.Replace(" ", "%")}%"
        End Function

        Public Async Function ListarAsync(texto As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of PersonaAgenda))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of PersonaAgenda)(New CommandDefinition(SqlLista, New With {.patron = Patron(texto)}, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>Nothing si la identificación no está en gacdp.</summary>
        Public Async Function FichaAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of FichaAgenda)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim ficha = Await cn.QueryFirstOrDefaultAsync(Of FichaAgenda)(New CommandDefinition(SqlFicha, New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
                If ficha Is Nothing Then Return Nothing
                ficha.Contactos = (Await cn.QueryAsync(Of ContactoAgenda)(New CommandDefinition(SqlContactos, New With {id}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                Return ficha
            End Using
        End Function

        Public Async Function TelefonoLocalAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(SqlTelefonoLocal, New With {.id = $"local{local}"}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ' ---- HTML ----

        Private Shared Function H(texto As String) As String
            Return WebUtility.HtmlEncode(If(texto, ""))
        End Function

        ''' <summary>Provincia, cantón, distrito y sector, y las señas. En VB6 un dato faltante dejaba la dirección en blanco.</summary>
        Friend Shared Function Direccion(f As FichaAgenda) As String
            Dim partes = {f.Provincia, f.Canton, f.Distrito, f.Sector}.Where(Function(p) Not String.IsNullOrWhiteSpace(p)).Select(Function(p) H(p.Trim()))
            Dim texto = String.Join(", ", partes)
            Dim senas = $"<b><u>SEÑAS:</u></b> {H(If(f.Senas, "").Trim())}"
            Return If(texto.Length = 0, senas, $"{texto}, {senas}")
        End Function

        ''' <summary>La ficha de SD_Agenda_Telefonicai. Con ficha Nothing, el aviso de que no hay registro, salvo sin identificación.</summary>
        Public Shared Function Html(id As String, ficha As FichaAgenda, marco As MarcoAgenda) As String
            Dim sb As New StringBuilder()
            sb.Append("<!DOCTYPE html><html><head><meta charset=""utf-8""><title>Datos personales</title><style>")
            sb.Append("body{background:#CCCCCC;font-family:'Segoe UI',Arial,sans-serif;font-size:10pt;margin:12px}")
            sb.Append("table{width:100%;border-collapse:collapse}td{padding:3px 6px;vertical-align:top}")
            sb.Append(".t td{background:#666699;color:#FFFFFF}.t td.e{width:20%;font-weight:bold}")
            sb.Append(".c th{background:#666699;color:#FFFFFF;padding:3px 6px}.c td.e{width:35%}")
            sb.Append(".centro{text-align:center;font-weight:bold;font-style:italic}.rojo{color:#FF0000;text-align:center;font-weight:bold}")
            sb.Append("</style></head><body>")
            sb.Append($"<p class=""centro"">{H(marco.Empresa)}<br>Cédula: {H(marco.Cedula)} Tel: {H(marco.Telefono)}</p>")
            If ficha Is Nothing Then
                If If(id, "").Trim().Length > 0 Then sb.Append($"<p class=""rojo"">NO HAY REGISTRO PARA '{H(id)}' EN LA BASE DE DATOS</p>")
            Else
                sb.Append("<table class=""t"">")
                sb.Append($"<tr><td class=""e"">Identificación</td><td>{H(ficha.Id)}, {H(ficha.Tipo)}</td></tr>")
                sb.Append($"<tr><td class=""e"">Nombre</td><td>{H(ficha.Nombre)}</td></tr>")
                sb.Append($"<tr><td class=""e"">Dirección</td><td>{Direccion(ficha)}</td></tr>")
                sb.Append("</table><br><table class=""c""><tr><th>Tipo</th><th>Número / Dato</th></tr>")
                If ficha.Contactos.Count = 0 Then
                    sb.Append("<tr><td colspan=""2"" class=""rojo"">NO HAY DATOS DE LOCALIZACIÓN...</td></tr>")
                End If
                For Each c In ficha.Contactos
                    sb.Append($"<tr><td class=""e"">{H(c.Tipo)}</td><td>{H(c.Dato)}</td></tr>")
                Next
                sb.Append("</table>")
            End If
            sb.Append($"<p class=""centro"">{H(marco.Usuario)}, {marco.Fecha:dd/MM/yyyy hh:mm:ss tt}</p></body></html>")
            Return sb.ToString()
        End Function

    End Class

End Namespace
