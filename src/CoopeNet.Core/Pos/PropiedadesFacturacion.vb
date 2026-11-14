Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>La pestaña "Parámetros de impresión" de frmOpImpres: valores de gatparametros por local.</summary>
    Public NotInheritable Class ParametrosFacturacion
        ''' <summary>PosFacLinea1 a PosFacLinea4: la leyenda al pie (40 caracteres por línea).</summary>
        Public Property Leyendas As String() = {"", "", "", ""}
        ''' <summary>PosFacLinea5: el texto de la proforma.</summary>
        Public Property TextoProforma As String = ""
        ''' <summary>POSFacRedon y POSFacRedonDir: redondeo de cada artículo.</summary>
        Public Property RedondeoArticulo As Double
        Public Property DireccionArticulo As DireccionRedondeo
        ''' <summary>POSFacLimRed y POSFacLimRedDir: redondeo del total de la factura.</summary>
        Public Property RedondeoTotal As Double
        Public Property DireccionTotal As DireccionRedondeo
        ''' <summary>POSObservFac1 a POSObservFac3: el largo de cada línea de observaciones.</summary>
        Public Property LargoObservaciones As Integer() = {0, 0, 0}
        ''' <summary>POSCopiasCredito: de 1 a 3.</summary>
        Public Property CopiasCredito As Integer = 1
    End Class

    ''' <summary>Una línea de un formato de impresión (gatprintapi).</summary>
    Public NotInheritable Class LineaFormato
        Public Property Pos As Integer
        Public Property Linea As String = ""
        Public Property Multiple As Integer = -1
    End Class

    ''' <summary>cmbPrinters de frmOpImpres: los documentos con impresora y formato propios.</summary>
    Public Enum DocumentoImpresion
        Contado = 0
        Credito = 1
        Proforma = 2
        OrdenEntrega = 3
        Cupon = 4
    End Enum

    ''' <summary>
    ''' La impresora y el formato de cada documento, en la sección [POS] del CONFIG.INI de CoopeNet. Son las mismas
    ''' llaves que lee SD_ImprimeFAC de FACPOS. TIPOPRINTER y TIPOPRINTERCRED (EPSON, SAMSUNG u OTRA) solo existen
    ''' para contado y crédito.
    ''' </summary>
    Public NotInheritable Class ImpresorasPos

        Public Const Seccion As String = "POS"
        ''' <summary>cmbFormat agrega este formato a los de gatprintapi: es la factura en carta de SDClienteEspecial.</summary>
        Public Const FormatoCarta As String = "PosFacCarta"

        Private Sub New()
        End Sub

        Public Shared ReadOnly Property Nombres As IReadOnlyList(Of String) = {
            "Formato para facturas de contado", "Formato para facturas de crédito", "Formato para facturas proforma",
            "Formato para órdenes de entrega", "Formato para cupón"}

        Public Shared Function LlaveImpresora(d As DocumentoImpresion) As String
            Select Case d
                Case DocumentoImpresion.Contado : Return "PRINTER_POS"
                Case DocumentoImpresion.Credito : Return "PRINTER_POSCRED"
                Case DocumentoImpresion.Proforma : Return "PRINTER_POSPROF"
                Case DocumentoImpresion.OrdenEntrega : Return "PRINTER_POSBODE"
                Case Else : Return "PRINTER_CUPON"
            End Select
        End Function

        ''' <summary>
        ''' La llave del formato. VB6 leía el de crédito de "POSCREDFROMAT" (mal escrita) y lo guardaba en
        ''' POSCREDFORMAT, que es la que usa la impresión; aquí se lee y se guarda la correcta.
        ''' </summary>
        Public Shared Function LlaveFormato(d As DocumentoImpresion) As String
            Select Case d
                Case DocumentoImpresion.Contado : Return "POSCONTFORMAT"
                Case DocumentoImpresion.Credito : Return "POSCREDFORMAT"
                Case DocumentoImpresion.Proforma : Return "POSPROFFORMAT"
                Case DocumentoImpresion.OrdenEntrega : Return "POSCOMPFORMAT"
                Case Else : Return "POSCUPON"
            End Select
        End Function

        ''' <summary>El formato que usa SD_ImprimeFAC si la llave no está.</summary>
        Public Shared Function FormatoPredeterminado(d As DocumentoImpresion) As String
            Select Case d
                Case DocumentoImpresion.Contado : Return "PosFacCon"
                Case DocumentoImpresion.Credito : Return "PosFacCre"
                Case DocumentoImpresion.Cupon : Return "POSCupon"
                Case Else : Return ""
            End Select
        End Function

        Public Shared Function LlaveTipo(d As DocumentoImpresion) As String
            Select Case d
                Case DocumentoImpresion.Contado : Return "TIPOPRINTER"
                Case DocumentoImpresion.Credito : Return "TIPOPRINTERCRED"
                Case Else : Return Nothing
            End Select
        End Function

        ''' <summary>La impresora del documento, o la predeterminada si no hay.</summary>
        Public Shared Function Impresora(configIni As String, d As DocumentoImpresion, predeterminada As String) As String
            Dim valor = ConexionesIni.LeerValor(configIni, Seccion, LlaveImpresora(d))
            Return If(valor.Length = 0, If(predeterminada, ""), valor)
        End Function

        Public Shared Function Formato(configIni As String, d As DocumentoImpresion) As String
            Dim valor = ConexionesIni.LeerValor(configIni, Seccion, LlaveFormato(d))
            Return If(valor.Length = 0, FormatoPredeterminado(d), valor)
        End Function

        ''' <summary>frmSelPrinters.cmdacp: la impresora y, para contado y crédito, su tipo.</summary>
        Public Shared Sub GuardarImpresora(configIni As String, d As DocumentoImpresion, impresora As String, tipo As String)
            ConexionesIni.FijarValor(configIni, Seccion, LlaveImpresora(d), impresora)
            Dim llave = LlaveTipo(d)
            If llave IsNot Nothing Then ConexionesIni.FijarValor(configIni, Seccion, llave, tipo)
        End Sub

        ''' <summary>cmdGuardaPrint.</summary>
        Public Shared Sub GuardarFormato(configIni As String, d As DocumentoImpresion, formatoElegido As String)
            ConexionesIni.FijarValor(configIni, Seccion, LlaveFormato(d), formatoElegido)
        End Sub

        ''' <summary>SD_TipoPrint: EPSON o SAMSUNG si el controlador lo dice; si no, OTRA.</summary>
        Public Shared Function TipoPorControlador(controlador As String) As String
            Dim c = If(controlador, "").ToUpperInvariant()
            If c.Contains("SAMSUNG") Then Return "SAMSUNG"
            If c.Contains("EPSON") Then Return "EPSON"
            Return "OTRA"
        End Function

    End Class

    ''' <summary>
    ''' Propiedades de facturación del POS. Migración de SD0502\FACPOS\frmOpImpres.frm (opción 043 de Mantenimiento,
    ''' privilegio 5072): los parámetros de impresión y los formatos de gatprintapi.
    ''' </summary>
    ''' <remarks>
    ''' Nada de esto se replica, igual que en VB6: cada local tiene sus parámetros y sus formatos.
    ''' </remarks>
    Public NotInheritable Class PropiedadesFacturacionRepositorio

        Public Const LargoLeyenda As Integer = 40
        Public Const LargoNombreFormato As Integer = 16
        Public Const LargoLinea As Integer = 255

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones
        Private ReadOnly _parametros As Parametros

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
            _parametros = New Parametros(fabrica)
        End Sub

        ''' <summary>cmdRestaurar: los valores de fábrica. Las observaciones, el texto de proforma y las copias quedan.</summary>
        Public Shared Sub ValoresPorDefecto(p As ParametrosFacturacion)
            p.Leyendas = {"CALIDAD 100% GARANTIZADA!! NO SE CAMBIA", "MERCADERIA DESPUES DE TRES DIAS, FAVOR",
                          "PRESENTAR ESTA FACTURA PARA SU RECLAMO", "GRACIAS POR PREFERIRNOS"}
            p.RedondeoArticulo = 0
            p.RedondeoTotal = 0
            p.DireccionArticulo = DireccionRedondeo.Ambas
            p.DireccionTotal = DireccionRedondeo.Ambas
        End Sub

        Private Shared Function Direccion(valor As Long) As DireccionRedondeo
            Return If(valor >= 0 AndAlso valor <= 2, CType(CInt(valor), DireccionRedondeo), DireccionRedondeo.Ascendente)
        End Function

        ''' <summary>SD_Cargar: los parámetros del local; los que no tiene salen del global (1000).</summary>
        Public Async Function CargarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of ParametrosFacturacion)
            Dim p As New ParametrosFacturacion()
            For i = 0 To 3
                p.Leyendas(i) = Await _parametros.ObtenerAsync(local, $"PosFacLinea{i + 1}", ct).ConfigureAwait(False)
            Next
            p.TextoProforma = Await _parametros.ObtenerAsync(local, "PosFacLinea5", ct).ConfigureAwait(False)
            p.RedondeoArticulo = Conversion.Val(Await _parametros.ObtenerAsync(local, "POSFacRedon", ct).ConfigureAwait(False))
            p.DireccionArticulo = Direccion(Await _parametros.ObtenerNumeroAsync(local, "POSFacRedonDir", ct).ConfigureAwait(False))
            p.RedondeoTotal = Conversion.Val(Await _parametros.ObtenerAsync(local, "POSFacLimRed", ct).ConfigureAwait(False))
            p.DireccionTotal = Direccion(Await _parametros.ObtenerNumeroAsync(local, "POSFacLimRedDir", ct).ConfigureAwait(False))
            For i = 0 To 2
                p.LargoObservaciones(i) = CInt(Await _parametros.ObtenerNumeroAsync(local, $"POSObservFac{i + 1}", ct).ConfigureAwait(False))
            Next
            p.CopiasCredito = CInt(Math.Max(1, Math.Min(3, Await _parametros.ObtenerNumeroAsync(local, "POSCopiasCredito", ct).ConfigureAwait(False))))
            Return p
        End Function

        ''' <summary>cmdGuardar: graba cada parámetro en el local.</summary>
        Public Async Function GuardarAsync(p As ParametrosFacturacion, local As Long, uid As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim valores As New List(Of KeyValuePair(Of String, String))
            For i = 0 To 3
                valores.Add(New KeyValuePair(Of String, String)($"PosFacLinea{i + 1}", Recortar(p.Leyendas(i), LargoLeyenda)))
            Next
            valores.Add(New KeyValuePair(Of String, String)("PosFacLinea5", If(p.TextoProforma, "").Trim()))
            valores.Add(New KeyValuePair(Of String, String)("POSFacRedon", p.RedondeoArticulo.ToString("0.00", Invariante)))
            valores.Add(New KeyValuePair(Of String, String)("POSFacRedonDir", CInt(p.DireccionArticulo).ToString(Invariante)))
            valores.Add(New KeyValuePair(Of String, String)("POSFacLimRed", p.RedondeoTotal.ToString("0.00", Invariante)))
            valores.Add(New KeyValuePair(Of String, String)("POSFacLimRedDir", CInt(p.DireccionTotal).ToString(Invariante)))
            For i = 0 To 2
                valores.Add(New KeyValuePair(Of String, String)($"POSObservFac{i + 1}", p.LargoObservaciones(i).ToString(Invariante)))
            Next
            valores.Add(New KeyValuePair(Of String, String)("POSCopiasCredito", p.CopiasCredito.ToString(Invariante)))
            For Each v In valores
                Await _parametros.GuardarAsync(local, v.Key, v.Value, uid, ct).ConfigureAwait(False)
            Next
        End Function

        Private Shared Function Recortar(texto As String, largo As Integer) As String
            Dim t = If(texto, "").Trim()
            Return If(t.Length > largo, t.Substring(0, largo), t)
        End Function

        ' ---- Formatos (gatprintapi) ----

        ''' <summary>Los formatos del local cuyo nombre empieza con POS.</summary>
        Public Async Function FormatosAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of String))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of String)(New CommandDefinition(
                    "SELECT DISTINCT informe FROM gatprintapi WHERE local = @local AND informe LIKE 'POS%' ORDER BY informe", New With {local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>cmbFormatos_Click: las líneas del formato en el local.</summary>
        Public Async Function LineasAsync(local As Long, informe As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of LineaFormato))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of LineaFormato)(New CommandDefinition(
                    "SELECT pos AS Pos, Linea, Multiple FROM gatprintapi WHERE local = @local AND informe = @informe ORDER BY pos",
                    New With {local, informe}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>
        ''' Revisa las líneas antes de guardar. Devuelve el problema, o "" si están bien. Las que no tienen texto no se
        ''' guardan, como en VB6.
        ''' </summary>
        Public Shared Function Validar(informe As String, lineas As IEnumerable(Of LineaFormato)) As String
            Dim nombre = If(informe, "").Trim()
            If nombre.Length = 0 Then Return "Escriba el nombre del formato."
            If nombre.Length > LargoNombreFormato Then Return $"El nombre del formato admite hasta {LargoNombreFormato} caracteres."
            If Not nombre.StartsWith("POS", StringComparison.OrdinalIgnoreCase) Then Return "El nombre del formato debe empezar con POS."
            Dim vistas As New HashSet(Of Integer)
            For Each l In lineas.Where(Function(x) If(x.Linea, "").Length > 0)
                If l.Pos < 0 OrElse l.Pos > 255 Then Return $"La posición {l.Pos} no es válida: va de 0 a 255."
                If Not vistas.Add(l.Pos) Then Return $"La posición {l.Pos} está repetida."
                If l.Linea.Length > LargoLinea Then Return $"La línea de la posición {l.Pos} pasa de {LargoLinea} caracteres."
            Next
            Return ""
        End Function

        ''' <summary>cmdGuardaFormato: reemplaza las líneas del formato en el local. No se replica.</summary>
        Public Async Function GuardarFormatoAsync(local As Long, informe As String, lineas As IEnumerable(Of LineaFormato), Optional ct As CancellationToken = Nothing) As Task
            Dim nombre = informe.Trim()
            Dim filas = lineas.Where(Function(x) If(x.Linea, "").Length > 0).ToList()
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM gatprintapi WHERE informe = @nombre AND local = @local", New With {nombre, local}, cancellationToken:=ct)).ConfigureAwait(False)
                For Each l In filas
                    Await cn.ExecuteAsync(New CommandDefinition(
                        "INSERT INTO gatprintapi (local, informe, pos, Linea, Multiple) VALUES (@local, @nombre, @pos, @linea, @multiple)",
                        New With {local, nombre, .pos = l.Pos, .linea = l.Linea, .multiple = l.Multiple}, cancellationToken:=ct)).ConfigureAwait(False)
                Next
            End Using
        End Function

        ''' <summary>
        ''' cmdExportar: una línea por fila con posición, línea y múltiple separados por punto y coma, sin encabezado,
        ''' como el .sdformat que exportaba la rejilla de VB6.
        ''' </summary>
        Public Shared Function Exportar(lineas As IEnumerable(Of LineaFormato)) As String
            Dim sb As New StringBuilder()
            For Each l In lineas.Where(Function(x) If(x.Linea, "").Length > 0)
                sb.Append(l.Pos.ToString(Invariante)).Append(";"c).Append(l.Linea).Append(";"c).Append(l.Multiple.ToString(Invariante)).Append(vbCrLf)
            Next
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' cmdImportar: lee un .sdformat. La línea puede traer punto y coma: lo que queda entre el primero y el último
        ''' es la línea. Se saltan las filas que no empiezan con un número.
        ''' </summary>
        Public Shared Function Importar(texto As String) As List(Of LineaFormato)
            Dim lista As New List(Of LineaFormato)
            For Each crudo In If(texto, "").Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                Dim primero = crudo.IndexOf(";"c)
                Dim ultimo = crudo.LastIndexOf(";"c)
                If primero <= 0 Then Continue For
                Dim pos As Integer
                If Not Integer.TryParse(crudo.Substring(0, primero).Trim(), NumberStyles.Integer, Invariante, pos) Then Continue For
                Dim linea As String
                Dim multiple = -1
                If ultimo > primero Then
                    linea = crudo.Substring(primero + 1, ultimo - primero - 1)
                    If Not Integer.TryParse(crudo.Substring(ultimo + 1).Trim(), NumberStyles.Integer, Invariante, multiple) Then multiple = -1
                Else
                    linea = crudo.Substring(primero + 1)
                End If
                lista.Add(New LineaFormato With {.Pos = pos, .Linea = linea, .Multiple = multiple})
            Next
            Return lista
        End Function

        ''' <summary>cmdExportar e Importar leen y escriben en Windows-1252, como los archivos de VB6.</summary>
        Public Shared ReadOnly Property CodificacionArchivo As Encoding
            Get
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
                Return Encoding.GetEncoding(1252)
            End Get
        End Property

    End Class

End Namespace
