Imports System.Data
Imports System.Globalization
Imports System.Text
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Lo que hace falta para imprimir una factura con un formato: las variables y la clave del QR.</summary>
    Public NotInheritable Class DatosImpresion
        ''' <summary>Los nombres de las variables separados por |, en el orden en que VB6 los arma.</summary>
        Public Property Parametros As String = ""
        ''' <summary>Los valores separados por |, en el mismo orden.</summary>
        Public Property Valores As String = ""
        ''' <summary>La dirección de consulta de la factura electrónica más su clave, para el QR.</summary>
        Public Property Clave As String = ""
        Public Property Lineas As Integer
    End Class

    ''' <summary>Los datos de la empresa y del local que van en la factura.</summary>
    Public NotInheritable Class EmisorFactura
        ''' <summary>Registro.NombreCorto: COOPEPALMARES R.L.</summary>
        Public Property NombreCorto As String = ""
        ''' <summary>Registro.Cedula, con guiones.</summary>
        Public Property Cedula As String = ""
    End Class

    ''' <summary>
    ''' Los datos de una factura del POS para imprimirla con un formato. Migración de SD_ImprimeCred de
    ''' SD0502\FACPOS\ModPOS.bas, más SD_SumTot y SD_NumLocObtener; la impresión es <see cref="ImpresionFormato"/>.
    ''' </summary>
    ''' <remarks>
    ''' Los números salen con punto decimal y coma de miles, como en las cajas de Coopepalmares, y los meses en
    ''' español como los daba Windows a VB6. VB6 comparaba el nombre del cliente con 0 para dejarlo en blanco; aquí
    ''' se deja en blanco solo cuando es "0".
    ''' </remarks>
    Public NotInheritable Class FacturaImpresionRepositorio

        Private Const LargoDescLarga As Integer = 45
        Private Const LargoDescCorta As Integer = 28

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private Shared ReadOnly Meses As String() = {"enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"}
        Private Shared ReadOnly MesesCortos As String() = {"ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"}

        Private ReadOnly _fabrica As IFabricaConexiones
        Private ReadOnly _parametros As Parametros

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
            _parametros = New Parametros(fabrica)
        End Sub

        ' ---- Formatos de número y fecha de VB6 ----

        ''' <summary>FormatNumber(x, decimales).</summary>
        Public Shared Function Numero(valor As Double, Optional decimales As Integer = 2) As String
            Return valor.ToString("#,##0" & If(decimales > 0, "." & New String("0"c, decimales), ""), Invariante)
        End Function

        ''' <summary>Format(x, "#0.00") y Format(x, "#0.0").</summary>
        Public Shared Function Corto(valor As Double, decimales As Integer) As String
            Return valor.ToString("0." & New String("0"c, decimales), Invariante)
        End Function

        ''' <summary>Format(fecha, "dd/mmm/yyyy hh:mm:ss") con los meses de Windows en español.</summary>
        Public Shared Function FechaLarga(f As Date) As String
            Return $"{f:dd}/{MesesCortos(f.Month - 1)}/{f:yyyy} {f.ToString("HH:mm:ss", Invariante)}"
        End Function

        Public Shared Function NombreMes(mes As Integer) As String
            Return Meses(mes - 1)
        End Function

        Private Shared Function Texto(o As Object) As String
            Return If(o Is Nothing OrElse TypeOf o Is DBNull, "", Convert.ToString(o, Invariante))
        End Function

        Private Shared Function Num(o As Object) As Double
            Return If(o Is Nothing OrElse TypeOf o Is DBNull, 0.0, Convert.ToDouble(o, Invariante))
        End Function

        Private Shared Function Escalar(cn As MySqlConnection, sql As String, Optional p As Object = Nothing) As String
            Return Texto(cn.ExecuteScalar(sql, p))
        End Function

        ' ---- Navegación ----

        ''' <summary>UpDown1: la factura anterior o siguiente del local, o "" si no hay.</summary>
        Public Async Function VecinaAsync(numfac As Long, siguiente As Boolean, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Dim sql = If(siguiente,
                "SELECT NumFac FROM posfacturaencabezado WHERE NumFac > @numfac AND local = @local ORDER BY NumFac LIMIT 1",
                "SELECT NumFac FROM posfacturaencabezado WHERE NumFac < @numfac AND local = @local ORDER BY NumFac DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Texto(Await cn.ExecuteScalarAsync(New CommandDefinition(sql, New With {numfac, local}, cancellationToken:=ct)).ConfigureAwait(False))
            End Using
        End Function

        ''' <summary>La última factura del local, para empezar.</summary>
        Public Async Function UltimaAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Texto(Await cn.ExecuteScalarAsync(New CommandDefinition("SELECT MAX(NumFac) FROM posfacturaencabezado WHERE local = @local", New With {local}, cancellationToken:=ct)).ConfigureAwait(False))
            End Using
        End Function

        ''' <summary>
        ''' SDPrintInfoAPI: las líneas del formato en el local, o las del local global (-1) si el local no lo tiene. El
        ''' segundo valor es 1 cuando salen del global (ver <see cref="ImpresionFormato.Interpretar"/>). Nothing si
        ''' no está en ninguno.
        ''' </summary>
        Public Async Function FormatoAsync(local As Long, informe As String, Optional ct As CancellationToken = Nothing) As Task(Of (Lineas As List(Of LineaFormato), Inicio As Integer)?)
            Const sql As String = "SELECT pos AS Pos, Linea, Multiple FROM gatprintapi WHERE local = @local AND informe = @informe ORDER BY pos"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim lineas = (Await cn.QueryAsync(Of LineaFormato)(New CommandDefinition(sql, New With {local, informe}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                If lineas.Count > 0 Then Return (lineas, 0)
                lineas = (Await cn.QueryAsync(Of LineaFormato)(New CommandDefinition(sql, New With {.local = -1, informe}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                If lineas.Count > 0 Then Return (lineas, 1)
                Return Nothing
            End Using
        End Function

        ''' <summary>
        ''' SD_ImprimeCred: las variables de la factura. <paramref name="localUsuario"/> es el local de la sesión, de
        ''' donde salen las leyendas, las observaciones y el redondeo. Nothing si la factura no existe en el local.
        ''' </summary>
        Public Async Function DatosAsync(numfac As String, local As Long, localUsuario As Long, emisor As EmisorFactura, Optional ct As CancellationToken = Nothing) As Task(Of DatosImpresion)
            Dim redondeo = Conversion.Val(Await _parametros.ObtenerAsync(localUsuario, "POSFacRedon", ct).ConfigureAwait(False))
            Dim direccion = CType(CInt(Math.Max(0, Math.Min(2, Await _parametros.ObtenerNumeroAsync(localUsuario, "POSFacRedonDir", ct).ConfigureAwait(False)))), DireccionRedondeo)
            Dim inicioAcumulado = Await _parametros.ObtenerAsync(localUsuario, "PosFecIniAcum", ct).ConfigureAwait(False)

            Dim sql = "SELECT Credito, IF(P.nombre IS NULL, ' ', E.cliente) Cliente, E.FC, IFNULL(U.Usuario, '') Usuario, E.NumFac," &
                      " IFNULL(CONCAT(P.nombre, ' ', P.apellido1, ' ', P.apellido2), E.cliente) AS NOMBRE," &
                      $" P.dir, D.CodigoArt, D.Cantidad, LEFT(A.DESCCORTA, {LargoDescCorta}) DCorta, D.PDescuent, D.Precio," &
                      " IF(D.EXENTO = 0, D.CANTIDAD * PRECIO, 0) VE, IF(D.EXENTO > 0, D.CANTIDAD * PRECIO, 0) VG," &
                      " (E.PagoEfectivo + E.PagoTarjeta + E.PagoCheque + E.PagoOtros) TOT," &
                      " E.Descuento DescuentoE, E.Impuesto ImpuestoE, D.EXENTO, D.IMPUESTO, D.PDescMont, E.REDONDEO," &
                      $" IFNULL(E.Observ, '') Observ, LEFT(A.DESCRIPCION, {LargoDescLarga}) DLarga, E.pagocon, D.descuento, D.promocion," &
                      " E.pagocon - E.pagotarjeta - E.pagocheque - E.pagootros AS pagoefectivo," &
                      " E.pagotarjeta, E.Pagocheque, E.pagootros, L.descripcion LocalDes, E.local," &
                      " CONCAT(D1.DESCRIPCION, '-', D2.DESCRIPCION) PROV_CANT, CONCAT(D3.DESCRIPCION, '-', D4.DESCRIPCION) DIST_SECT, D.TipoPrecio, IF(A.exento > 0, '*', ' ') IVA," &
                      " 0 saldopromo, UN.consec, EN.CLAVE00, L.Direccion, L.DirLoc," &
                      " EN.NNUME00, EN.NFECH00, res.res_version, res.res_fecha_text, EN.REMAI00," &
                      " CASE EN.TIREG00 WHEN 'TE' THEN 'TIQUETE ELECTRONICO' WHEN 'FE' THEN 'FACTURA ELECTRONICA' ELSE 'TIPO DESCONOCIDO' END AS TIREG00, pos_fun_desglose_iva(E.numfac) DESG_IVA" &
                      " FROM posfacturaencabezado E" &
                      " INNER JOIN posfacturadetalle D ON D.Local = E.Local AND D.NumFac = E.NumFac" &
                      " INNER JOIN gatlocales L ON L.local = E.local" &
                      " LEFT JOIN posarticulos A ON A.CODIGOART = D.CODIGOART" &
                      " LEFT JOIN gacdp P ON P.id = E.Cliente" &
                      " LEFT JOIN gatprovincias D1 ON D1.Codigo = P.prov" &
                      " LEFT JOIN gatcantones D2 ON D2.Provincia = P.prov AND D2.Codigo = P.cant" &
                      " LEFT JOIN gatdistritos D3 ON D3.Provincia = P.prov AND D3.Canton = P.cant AND D3.Codigo = P.dist" &
                      " LEFT JOIN gatsectores D4 ON D4.Provincia = P.prov AND D4.Canton = P.cant AND D4.Distrito = P.dist AND D4.Codigo = P.caserio" &
                      " LEFT JOIN gatusuarios U ON U.UID = E.USUARIO" &
                      " LEFT JOIN poscreditodetalle CD ON CD.numfac = E.numfac" &
                      " LEFT JOIN fe.posfe_consec_un UN ON UN.local = E.Local AND UN.numfac = E.NumFac" &
                      " LEFT JOIN fe.posfe_encabezado EN ON EN.CONSE00 = UN.consec" &
                      " CROSS JOIN fe.posfe_resolucion res" &
                      " WHERE E.NumFac = @numfac AND E.local = @local AND res.res_vigente = 1 ORDER BY A.descripcion"

            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim tabla = Await FilasAsync(cn, New CommandDefinition(sql, New With {numfac, local}, cancellationToken:=ct)).ConfigureAwait(False)
                If tabla.Count = 0 Then Return Nothing
                Dim r0 = tabla(0)

                Dim ip As New StringBuilder("TIPOF|CRE|CONT|IDCLI|IDE|DIA|MES|ANO|HORA|FECHA|FACT|REF|FR|REFS|FRS|NOM|DIR|DIR2|IMP|TOT|TOTX2|STT|PAG|VUEL|REDM|AHORR|PEFEC|PTARJ|PCHEQ|POTRO|PROV_CANT|DIST_SECT|FE_COSEC|FE_CLAV1|FE_CLAV2|DIRECCIOND|DIRLOC|RES1|RES2|VERSION|REMAI00|TIREG00|DESG_IVA|")
                Dim iv As New StringBuilder()

                ' NNUME00 vacío se busca en posfe_encabezado44; nulo queda en blanco (VB6 fallaba al asignarlo).
                Dim nnume = r0("NNUME00")
                Dim numResol = If(TypeOf nnume Is DBNull, "", If(Texto(nnume) = "", Escalar(cn, "SELECT nnume00 FROM fe.posfe_encabezado44 WHERE clave00 = @c", New With {.c = Texto(r0("CLAVE00"))}), Texto(nnume)))
                Dim localFactura = CLng(Num(r0("local")))
                Dim numFactura = Texto(r0("NumFac"))
                Dim observ = Texto(r0("Observ"))
                Dim localDes = Texto(r0("LocalDes"))
                Dim facPor = Texto(r0("Usuario")).ToUpperInvariant()
                iv.Append(If(Core.Vb6.Val(Texto(r0("Credito"))) = 0, "CONTADO| |X|", "CREDITO|X| |"))

                Dim cliente = Texto(r0("Cliente"))
                Dim sqlCodigo As String
                Select Case Izq(Texto(r0("Credito")), 1)
                    Case "1" : sqlCodigo = "SELECT CONCAT(CODIGO, IF(ASOCIADO = '1', '(ASOC.)', '(NO ASOC.)')) FROM gacasociados WHERE ACTIVO = '1' AND ID = @cliente"
                    Case "2" : sqlCodigo = "SELECT CONCAT(codigo, '(EMP.)') FROM gaccodclientes WHERE id = @cliente LIMIT 1"
                    Case "3" : sqlCodigo = "SELECT CONCAT(codigo, '(CLI.)') FROM gaccodclientes WHERE id = @cliente LIMIT 1"
                    Case Else : sqlCodigo = "SELECT codigo FROM gaccodclientes WHERE id = @cliente LIMIT 1"
                End Select
                Dim codigo = Escalar(cn, sqlCodigo, New With {cliente})
                Dim fc = CDate(r0("FC"))
                iv.Append($"({codigo}){cliente}|{codigo}|")
                iv.Append($"{fc.Day}|{NombreMes(fc.Month)}|{fc.Year}|{fc.ToString("HH:mm:ss", Invariante)}|{FechaLarga(fc)}|")
                iv.Append($"FACTURA: {numFactura}|REF:{numFactura}|{numFactura}|REF:{Der(numFactura, 6)}|{Der(numFactura, 6)}|")
                Dim nombre = Texto(r0("NOMBRE"))
                iv.Append(If(nombre = "0", "", nombre)).Append(" |")
                Dim dir = Texto(r0("dir"))
                If dir.Length > 39 Then
                    iv.Append(Izq(dir, 40)).Append(" |").Append(ImpresionFormato.Medio(dir, 41)).Append(" |")
                Else
                    iv.Append(dir).Append(" |").Append(" |")
                End If
                Dim tot = Num(r0("TOT"))
                Dim redon = Num(r0("REDONDEO"))
                Dim pagocon = Num(r0("pagocon"))
                iv.Append(Numero(Num(r0("ImpuestoE")))).Append("|").Append(Numero(tot)).Append("|").Append(Numero(tot * 2)).Append("|").Append(Numero(tot - redon)).Append("|")
                iv.Append(Numero(pagocon)).Append("|").Append(If(pagocon - tot = 0, "", Numero(pagocon - tot))).Append("|")
                iv.Append(If(redon = 0, " ", Numero(redon))).Append("|")
                iv.Append(Si(Num(r0("DescuentoE")) > 0, Numero(Num(r0("DescuentoE"))))).Append("|")
                Dim tarjeta = Num(r0("pagotarjeta")), cheque = Num(r0("Pagocheque")), otros = Num(r0("pagootros"))
                iv.Append(Si(Num(r0("pagoefectivo")) > 0, Numero(pagocon - tarjeta - cheque - otros))).Append("|")
                iv.Append(Si(tarjeta > 0, Numero(tarjeta))).Append("|").Append(Si(cheque > 0, Numero(cheque))).Append("|").Append(Si(otros > 0, Numero(otros))).Append("|")
                iv.Append(Texto(r0("PROV_CANT"))).Append("|").Append(Texto(r0("DIST_SECT"))).Append("|").Append(Texto(r0("consec"))).Append("|")
                Dim clave00 = Texto(r0("CLAVE00"))
                iv.Append(Izq(clave00, 25)).Append("|").Append(Der(clave00, 25)).Append("|")
                iv.Append(Texto(r0("Direccion")).ToUpperInvariant()).Append("|")
                iv.Append(Escalar(cn, "SELECT dir FROM v_lugares WHERE cod = @cod", New With {.cod = Texto(r0("DirLoc"))})).Append("|")
                iv.Append(numResol).Append("|").Append(Izq(Texto(r0("NFECH00")), 10)).Append("|")
                iv.Append("VERSION: ").Append(Texto(r0("res_version"))).Append("|").Append(Texto(r0("REMAI00"))).Append("|").Append(Texto(r0("TIREG00"))).Append("|").Append(Texto(r0("DESG_IVA"))).Append("|")

                Dim clave = Escalar(cn, "SELECT url FROM fe.posfe_urlconsulta") & clave00

                Dim i = 0
                Dim subtotal = 0.0, descMont = 0.0, totExento = 0.0, totGravado = 0.0, descuento = 0.0
                Dim tmp = 0.0
                For Each r In tabla
                    ip.Append($"COD{i}|CAN{i}|DES{i}|DEC{i}|PU{i}|VE{i}|VG{i}|VE_D{i}|VG_D{i}|EX{i}|VT{i}|VT_D{i}|DESL{i}|VTT{i}|ESPEC{i}|PATROC{i}|PU_C{i}|VT_C{i}|CAN_C{i}|IVA{i}|VTT_C{i}|IVA_N{i}|")
                    Dim cantidad = Num(r("Cantidad"))
                    Dim pDescuent = Num(r("PDescuent"))
                    Dim impuesto = Num(r("IMPUESTO"))
                    Dim pDescMont = Num(r("PDescMont"))
                    Dim exento = Num(r("EXENTO"))
                    iv.Append(Texto(r("CodigoArt"))).Append("|").Append(Numero(cantidad)).Append("|").Append(Texto(r("DCorta"))).Append("|")
                    iv.Append(If(pDescuent > 0, Texto(r("PDescuent")), " ")).Append("|")
                    tmp = If(cantidad > 0, Num(r("Precio")) - impuesto / cantidad, 0)
                    ' Con 100 % de descuento VB6 dividía entre cero y dejaba el precio sin el descuento.
                    If pDescuent <> 100 Then tmp += tmp / (100 - pDescuent) * pDescuent
                    iv.Append(If(tmp > 0, Numero(tmp), " ")).Append("|")
                    Dim linea = tmp * cantidad
                    If exento = 0 Then
                        iv.Append(Numero(linea)).Append("| |").Append(Numero(linea - pDescMont)).Append("| |").Append("*|")
                        totExento += linea
                    Else
                        iv.Append(" |").Append(Numero(linea)).Append("| |")
                        totGravado += linea
                        iv.Append(Numero(linea - pDescMont)).Append("|").Append(" |")
                    End If
                    iv.Append(Numero(linea)).Append("|").Append(Numero(linea - pDescMont)).Append("|").Append(Texto(r("DLarga"))).Append("|")
                    iv.Append(Numero(PreciosArticulo.Redondear(linea + impuesto, redondeo, direccion))).Append("|")
                    subtotal += linea
                    descMont += pDescMont
                    Dim especial = If(exento = 0, "+", "")
                    If pDescMont > 0 OrElse Num(r("descuento")) > 0 Then especial &= "*"
                    If especial = "" Then especial = "  "
                    iv.Append(especial).Append("|").Append(If(Texto(r("promocion")) = "1", "P", " ")).Append("|")
                    iv.Append(If(tmp > 0, Corto(tmp, 2), " ")).Append("|").Append(Corto(linea, 2)).Append("|").Append(Corto(cantidad, 1)).Append("|")
                    iv.Append(Texto(r("IVA"))).Append("|").Append(Numero(linea, 0)).Append("|").Append(Texto(r("EXENTO"))).Append("|")
                    descuento += Num(r("descuento"))
                    i += 1
                Next

                iv.Append(If(descMont > 0, "DESCUENTO|" & Numero(descMont) & "|", " | |"))
                iv.Append(" |") ' SALDOPROMO: VB6 lo consulta con 0 fijo.

                ' Leyendas y largos de observación, en el orden de Parametro (PosFacLinea1-5 y luego POSObservFac1-3).
                Dim fijos = (Await cn.QueryAsync(Of String)(New CommandDefinition(
                    "SELECT Valor FROM gatparametros WHERE local = @localUsuario AND (Parametro LIKE 'POSObservFac%' OR Parametro LIKE 'PosFacLinea%') ORDER BY Parametro",
                    New With {localUsuario}, cancellationToken:=ct)).ConfigureAwait(False)).Select(Function(v) If(v, "")).ToList()
                Dim fijo = Function(n As Integer) If(n < fijos.Count, fijos(n), "")
                Dim obs = {CInt(Core.Vb6.Val(fijo(5))), CInt(Core.Vb6.Val(fijo(6))), CInt(Core.Vb6.Val(fijo(7)))}
                For k = 0 To 2
                    iv.Append(If(observ = "", " ", Izq(observ, obs(k)))).Append("|")
                    observ = If(observ.Length > obs(k), Der(observ, observ.Length - obs(k)), "")
                Next
                iv.Append(Numero(subtotal)).Append("|").Append(Numero(subtotal - descMont)).Append("|")

                Dim acumulado = 0.0
                If cliente <> " " Then acumulado = SumarCompras(cn, cliente, inicioAcumulado, localFactura)
                iv.Append(If(acumulado > 0, Numero(acumulado), " ")).Append("|")
                iv.Append(Numero(totExento)).Append("|").Append(Numero(totGravado)).Append("|")
                iv.Append(If(descuento > 0, Numero(descuento), " ")).Append("|")
                ip.Append("DES|DESM|SALDOPROMO|OB1|OB2|OB3|ST|ST_D|TOTCOM|TOTEXE|TOTGRA|DESCUENTO|")

                Dim orden = "", pedido = "", vence = "", zona = "", vendedor = "", transporte = "", numLoc = ""
                Dim otros2 = Await FilasAsync(cn, New CommandDefinition(
                    "SELECT IFNULL(OrdenCompra, '') Orden, IFNULL(OrdenPedido, '') Pedido, IFNULL(DATE_ADD(frige, INTERVAL plazo DAY), '') VENCE, IFNULL(zona, 0) Zona," &
                    " IFNULL(UCASE(U.usuario), '') Vendedor, IFNULL(transporte, '') Transporte, IFNULL(vendedor, '') CodVendedor FROM posfacturaencabezado E" &
                    " LEFT JOIN posfactotros O ON O.numfac = E.numfac LEFT JOIN cxc C ON C.numdcto = E.numfac LEFT JOIN posclientes T ON T.cliente = E.cliente" &
                    " LEFT JOIN gatusuarios U ON U.id = T.vendedor WHERE E.numfac = @numFactura AND E.CLIENTE = @cliente",
                    New With {numFactura, cliente}, cancellationToken:=ct)).ConfigureAwait(False)
                If otros2.Count > 0 Then
                    Dim o = otros2(0)
                    orden = Texto(o("Orden"))
                    pedido = Texto(o("Pedido"))
                    Dim v = o("VENCE")
                    vence = If(TypeOf v Is Date, CDate(v).ToString("dd/MM/yyyy", Invariante), Texto(v))
                    zona = Texto(o("Zona"))
                    vendedor = Texto(o("Vendedor"))
                    transporte = Texto(o("Transporte"))
                    numLoc = Escalar(cn, "SELECT A.dato FROM gacdptel A WHERE A.id = @cliente AND A.tipo = 0 ORDER BY A.pos LIMIT 1", New With {cliente})
                End If
                ip.Append("FP|PED|OCO|ZON|TEL|VEC|TRA|LI1|LI2|LI3|LI4|LI5|TRANS|AGENT|")
                For Each dato In {facPor, pedido, orden, zona, numLoc, vence}
                    iv.Append(If(dato = "", " ", dato)).Append("|")
                Next
                iv.Append("DIARIA|")
                For k = 0 To 4
                    iv.Append(If(fijo(k) = "", " ", fijo(k))).Append("|")
                Next
                iv.Append(If(transporte = "", " ", transporte)).Append("|").Append(If(vendedor = "", " ", vendedor)).Append("|")

                Dim telefono = Escalar(cn, "SELECT A.dato FROM gacdptel A WHERE A.id = @id AND A.tipo = 0 ORDER BY A.pos LIMIT 1", New With {.id = $"local{localUsuario}"})
                Dim correo = Escalar(cn, "SELECT A.dato FROM gacdptel A WHERE A.id = @id AND A.tipo = 4 ORDER BY A.pos LIMIT 1", New With {.id = $"local{localUsuario}"})
                ip.Append("EMPR|LOCAL|CANLIN|CEDULA|LTEL|CORREO|LSEP")
                iv.Append(emisor.NombreCorto).Append("|").Append(localDes).Append("|")
                iv.Append(ImpresionFormato.Ajusta($"( {i} LINEAS )", 40, AjusteTexto.Centrado, ".")).Append("|")
                iv.Append(emisor.Cedula).Append("|").Append(telefono).Append("|").Append(correo).Append("|")
                iv.Append(ImpresionFormato.Ajusta("-", 40, AjusteTexto.Centrado, "-"))

                Return New DatosImpresion With {.Parametros = ip.ToString(), .Valores = iv.ToString().Replace(vbCr, ""), .Clave = clave, .Lineas = i}
            End Using
        End Function

        ''' <summary>
        ''' Las filas como diccionarios. No se usa DataTable.Load: toma la llave primaria de la consulta (NumFac) y
        ''' junta en una todas las líneas de detalle de la factura.
        ''' </summary>
        Private Shared Async Function FilasAsync(cn As MySqlConnection, comando As CommandDefinition) As Task(Of List(Of Dictionary(Of String, Object)))
            Dim filas As New List(Of Dictionary(Of String, Object))
            Using lector = Await cn.ExecuteReaderAsync(comando).ConfigureAwait(False)
                While Await lector.ReadAsync(comando.CancellationToken).ConfigureAwait(False)
                    Dim fila As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
                    For c = 0 To lector.FieldCount - 1
                        fila(lector.GetName(c)) = lector.GetValue(c)
                    Next
                    filas.Add(fila)
                End While
            End Using
            Return filas
        End Function

        ''' <summary>SD_SumTot: lo que el asociado ha comprado desde PosFecIniAcum, en la tabla actual y en la histórica.</summary>
        Private Shared Function SumarCompras(cn As MySqlConnection, cliente As String, desde As String, local As Long) As Double
            Const parte As String = "SELECT IFNULL(SUM(PagoEfectivo + PagoTarjeta + pagocheque + pagootros), 0) monto FROM {0} a INNER JOIN gacasociados b ON a.cliente = b.id" &
                                    " WHERE cliente = @cliente AND b.activo = '1' AND b.asociado = '1' AND a.fc > @desde AND a.estado = '4' AND a.local = @local"
            Dim sql = "SELECT SUM(monto) FROM (" & String.Format(Invariante, parte, "posfacturaencabezado") & " UNION ALL " & String.Format(Invariante, parte, "posfacturaencabezado_4") & ") AS S"
            Return Num(cn.ExecuteScalar(sql, New With {cliente, desde, local}))
        End Function

        Private Shared Function Izq(t As String, n As Integer) As String
            Return ImpresionFormato.Izquierda(t, n)
        End Function

        Private Shared Function Der(t As String, n As Integer) As String
            Return ImpresionFormato.Derecha(t, n)
        End Function

        Private Shared Function Si(condicion As Boolean, valor As String) As String
            Return If(condicion, valor, " ")
        End Function

    End Class

End Namespace
