Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>El artículo elegido y sus categorías en el local (SD_DetArtic).</summary>
    Public NotInheritable Class ArticuloMovimientos
        Public Property Codigo As String = ""
        Public Property Descripcion As String = ""
        Public Property Categ As Integer
        Public Property SCateg As Integer
        Public Property SSCateg As Integer
    End Class

    ''' <summary>Un mes del gráfico: compras y ventas acumuladas.</summary>
    Public NotInheritable Class MesMovimientos
        Public Property Mes As Date
        Public Property Compras As Double
        Public Property Ventas As Double
    End Class

    ''' <summary>Una fila del informe resumido (RPT0013): un día o un mes.</summary>
    Public NotInheritable Class FilaResumenMovimientos
        ''' <summary>F1: "09/nov/2026" en el diario; el número del mes en el mensual.</summary>
        Public Property Periodo As String = ""
        Public Property Compras As Double
        Public Property Notas As Double
        Public Property Ajustes As Double
        Public Property Requisiciones As Double
        Public Property Ventas As Double
        Public Property Total As Double
        ''' <summary>H: la existencia de posinventariohistorico el día del primer movimiento del grupo.</summary>
        Public Property Historico As Double?
        Public Property Descripcion As String = ""
    End Class

    ''' <summary>Una fila del informe detallado (RPT0017).</summary>
    Public NotInheritable Class FilaDetalleMovimientos
        Public Property Fecha As Date
        Public Property Entradas As Double
        Public Property Salidas As Double
        Public Property Documento As String = ""
        Public Property Tipo As String = ""
    End Class

    ''' <summary>
    ''' Movimientos del artículo. Migración de las consultas de SD0502\REP\frmVentDetall.frm (SD_DetArtic,
    ''' SD_CargaLike, SD_Rept, SD_ReptDetallado, chkInvInic_Click) y de SDInvInicial y SD_CalculoGraf de
    ''' REP\ModPrin.bas. Todo es lectura.
    ''' </summary>
    ''' <remarks>
    ''' VB6 copiaba la fila de posarticuloslocal a la tabla posinventariotmp_UID para leer el inventario
    ''' inicial; aquí se lee posarticuloslocal directo y no se crean tablas. Tampoco se llena posmeses
    ''' (SD_CalculoMeses), que esta pantalla no usa. En el resumido diario las devoluciones de venta se
    ''' agrupan por día como lo demás: VB6 las agrupaba por mes y salían como una fila con el nombre del mes.
    ''' </remarks>
    Public NotInheritable Class MovimientosArticuloRepositorio

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ' ---- Artículo ----

        ''' <summary>SD_DetArtic: el artículo si está en posinventario del local; Nothing si no.</summary>
        Public Async Function ArticuloAsync(codigo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of ArticuloMovimientos)
            Const sql As String =
                "SELECT A.CodigoArt AS Codigo, A.Descripcion, I.Categ, I.SCateg, I.SSCateg FROM posarticulos A " &
                "INNER JOIN posinventario I ON I.CArticulo = A.CodigoArt AND I.CLocal = @local WHERE A.CodigoArt = @codigo LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of ArticuloMovimientos)(New CommandDefinition(sql, New With {codigo, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>La lista de la izquierda: los artículos de la misma sub-subcategoría.</summary>
        Public Async Function MismaCategoriaAsync(local As Long, categ As Integer, scateg As Integer, sscateg As Integer, Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionArticulo))
            Const sql As String =
                "SELECT A.CodigoArt AS Codigo, A.Descripcion FROM posarticulos A INNER JOIN posinventario I ON I.CArticulo = A.CodigoArt AND I.CLocal = @local " &
                "WHERE I.Categ = @categ AND I.SCateg = @scateg AND I.SSCateg = @sscateg"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of OpcionArticulo)(New CommandDefinition(sql, New With {local, categ, scateg, sscateg}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>SD_CargaLike: los artículos del local cuya descripción contiene el texto.</summary>
        Public Async Function PorDescripcionAsync(local As Long, texto As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionArticulo))
            Const sql As String =
                "SELECT A.CodigoArt AS Codigo, A.Descripcion FROM posarticulos A INNER JOIN posinventario I ON I.CArticulo = A.CodigoArt AND I.CLocal = @local " &
                "WHERE A.Descripcion LIKE @patron"
            Dim patron = "%" & If(texto, "").Trim() & "%"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of OpcionArticulo)(New CommandDefinition(sql, New With {local, patron}, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>sdtCodigo_KeyDown vacío: los artículos del local para el buscador.</summary>
        Public Async Function BuscarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT A.Descripcion AS 'Descripción', A.CodigoArt AS 'Artículo' FROM posarticulos A INNER JOIN posarticuloslocal L ON L.CArticulo = A.CodigoArt AND L.CLocal = @local"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {local}, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>InvInicial e InvInicialF del artículo en el local; Nothing si no está.</summary>
        Public Async Function InventarioInicialAsync(codigo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of (Cantidad As Double, Fecha As Date)?)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim fila = Await cn.QueryFirstOrDefaultAsync(Of ExistenciaLocal)(New CommandDefinition(
                    "SELECT InvInicial, InvInicialF FROM posarticuloslocal WHERE CArticulo = @codigo AND CLocal = @local", New With {codigo, local}, cancellationToken:=ct)).ConfigureAwait(False)
                If fila Is Nothing Then Return Nothing
                Return (fila.InvInicial, fila.InvInicialF)
            End Using
        End Function

        ' ---- Gráfico (SD_CalculoGraf) ----

        ''' <summary>
        ''' Los doce meses anteriores al de <paramref name="hoy"/>, con las compras de posacumcompra_ y las ventas
        ''' de posacumventas_. Un mes sin datos queda en cero; VB6 lo saltaba y desalineaba las dos series.
        ''' </summary>
        Public Async Function GraficoAsync(codigo As String, local As Long, hoy As Date, Optional ct As CancellationToken = Nothing) As Task(Of List(Of MesMovimientos))
            Dim inicio = New Date(hoy.Year, hoy.Month, 1).AddYears(-1)
            Dim fin = New Date(hoy.Year, hoy.Month, 1).AddDays(-1)
            Dim meses = Enumerable.Range(0, 12).Select(Function(i) New MesMovimientos With {.Mes = inicio.AddMonths(i)}).ToList()
            Const filtro As String = " WHERE Local = @local AND Articulo = @codigo AND Mes BETWEEN @inicio AND @fin"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim p = New With {local, codigo, inicio, fin}
                For Each c In Await cn.QueryAsync(Of MesCantidad)(New CommandDefinition("SELECT Mes, Cantidad FROM posacumcompra_" & filtro, p, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim m = meses.Find(Function(x) x.Mes.Year = c.Mes.Year AndAlso x.Mes.Month = c.Mes.Month)
                    If m IsNot Nothing Then m.Compras += c.Cantidad
                Next
                For Each v In Await cn.QueryAsync(Of MesCantidad)(New CommandDefinition("SELECT Mes, Cantidad FROM posacumventas_" & filtro, p, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim m = meses.Find(Function(x) x.Mes.Year = v.Mes.Year AndAlso x.Mes.Month = v.Mes.Month)
                    If m IsNot Nothing Then m.Ventas += v.Cantidad
                Next
            End Using
            Return meses
        End Function

        Private NotInheritable Class MesCantidad
            Public Property Mes As Date
            Public Property Cantidad As Double
        End Class

        ' ---- Inventario inicial del informe ----

        ''' <summary>
        ''' SDInvInicial: el inventario inicial más compras, ventas (menos devoluciones), ajustes, requisiciones y
        ''' notas entre @desde y @hasta.
        ''' </summary>
        Friend Const SqlInventarioAl As String =
            "SELECT SUM(Cant) FROM (" &
            "SELECT InvInicial AS Cant FROM posarticuloslocal WHERE CArticulo = @codigo AND CLocal = @local " &
            "UNION ALL SELECT IFNULL(SUM(cantidad), 0) FROM poscompraslineas A INNER JOIN poscompras B ON A.proveedor = B.proveedor AND A.fact_comp = B.fact_comp " &
            "WHERE B.fa BETWEEN @desde AND @hasta AND articulo = @codigo AND aplicada = '1' AND B.anulada = '0' AND B.Local = @local " &
            "UNION ALL SELECT IFNULL(SUM(cantidad - devoluciones), 0) * -1 FROM posfacturadetalle A INNER JOIN posfacturaencabezado B ON B.local = A.local AND B.numfac = A.numfac " &
            "AND B.fc BETWEEN @desde AND @hasta AND A.codigoart = @codigo AND B.estado = '4' WHERE B.Local = @local " &
            "UNION ALL SELECT IFNULL(SUM(IF(movimi = 0, cantid * -1, cantid)), 0) FROM posajustesinvent WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND Local = @local " &
            "UNION ALL SELECT IFNULL(SUM(IF(movimi = 0, (cantid - devolucion) * -1, (cantid - devolucion))), 0) FROM posrequisic WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND Local = @local " &
            "UNION ALL SELECT IFNULL(SUM(IF(movimi = 0, cantid * -1, cantid)), 0) FROM posnotas WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND Local = @local) A"

        ''' <summary>
        ''' El inventario inicial del resumido (SD_Rept). Con <paramref name="desdeInicial"/> es InvInicial; si no,
        ''' es 0 cuando el informe empieza antes de InvInicialF, InvInicial si empieza justo ese momento, y si
        ''' empieza después, lo que da SDInvInicial desde InvInicialF hasta el inicio del día de
        ''' <paramref name="desde"/>.
        ''' </summary>
        Public Async Function InicialResumenAsync(codigo As String, local As Long, desde As Date, desdeInicial As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of Double)
            Dim inicial = Await InventarioInicialAsync(codigo, local, ct).ConfigureAwait(False)
            If Not inicial.HasValue Then Return 0
            Select Case DecidirInicial(desde, inicial.Value.Fecha, desdeInicial)
                Case ReglaInicial.Cero : Return 0
                Case ReglaInicial.Inicial : Return inicial.Value.Cantidad
            End Select
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim p = New With {codigo, local, .desde = inicial.Value.Fecha, .hasta = desde.Date}
                Return If(Await cn.ExecuteScalarAsync(Of Double?)(New CommandDefinition(SqlInventarioAl, p, commandTimeout:=600, cancellationToken:=ct)).ConfigureAwait(False), 0)
            End Using
        End Function

        Friend Enum ReglaInicial
            Cero
            Inicial
            Calcular
        End Enum

        ''' <summary>Compara al segundo, como las cadenas "yyyy-MM-dd HH:mm:ss" de VB6.</summary>
        Friend Shared Function DecidirInicial(desde As Date, fechaInicial As Date, desdeInicial As Boolean) As ReglaInicial
            If desdeInicial Then Return ReglaInicial.Inicial
            Dim a = desde.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            Dim b = fechaInicial.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            Dim orden = String.CompareOrdinal(a, b)
            If orden < 0 Then Return ReglaInicial.Cero
            If orden = 0 Then Return ReglaInicial.Inicial
            Return ReglaInicial.Calcular
        End Function

        ' ---- Informe resumido (SD_Rept / RPT0013) ----

        Friend Shared Function SqlResumen(diario As Boolean) As String
            Dim fb = If(diario, "DATE_FORMAT(B.fc, '%d/%b/%Y')", "DATE_FORMAT(B.fc, '%c')")
            Dim fx = If(diario, "DATE_FORMAT(fc, '%d/%b/%Y')", "DATE_FORMAT(fc, '%c')")
            Dim fd = If(diario, "DATE_FORMAT(d.fc, '%d/%b/%Y')", "DATE_FORMAT(d.fc, '%c')")
            Return "SELECT CONCAT(A.Descripcion, '  ( STOCK = ', CAST(ROUND(L.Stock, 2) AS CHAR), ' )') AS Descripcion, F1 AS Periodo, SUM(Vent) AS Ventas, SUM(Comp) AS Compras, " &
                   "SUM(Ajus) AS Ajustes, SUM(Notas) AS Notas, SUM(Requ) AS Requisiciones, SUM(Comp) + SUM(Vent) + SUM(Ajus) + SUM(Notas) + SUM(Requ) AS Total, I.Stock AS Historico FROM (" &
                   $"SELECT A.codigoart, {fb} F1, SUM(cantidad) * -1 Vent, 0 Comp, 0 Ajus, 0 Notas, 0 Requ, B.fc FROM posfacturadetalle A " &
                   "INNER JOIN posfacturaencabezado B ON A.local = B.local AND A.numfac = B.numfac " &
                   $"WHERE B.fc BETWEEN @desde AND @hasta AND A.codigoart = @codigo AND B.estado = '4' AND B.Local = @local GROUP BY {fb} " &
                   $"UNION ALL SELECT articulo, {fb} F1, 0, SUM(cantidad), 0, 0, 0, B.fc FROM poscompraslineas A " &
                   "INNER JOIN poscompras B ON A.proveedor = B.proveedor AND A.fact_comp = B.fact_comp " &
                   $"WHERE B.fa BETWEEN @desde AND @hasta AND articulo = @codigo AND B.aplicada = '1' AND B.anulada = '0' AND B.Local = @local GROUP BY {fb} " &
                   $"UNION ALL SELECT articu, {fx}, 0, 0, SUM(IF(movimi = 0, cantid * -1, cantid)), 0, 0, fc FROM posajustesinvent " &
                   $"WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND Local = @local GROUP BY {fx} " &
                   $"UNION ALL SELECT articu, {fx}, 0, 0, 0, 0, SUM(cantid * -1), fc FROM posrequisic " &
                   $"WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND (Local = @local OR aLocal = @local) AND requisic = '1' GROUP BY {fx} " &
                   $"UNION ALL SELECT articu, {fx}, 0, 0, 0, 0, SUM(IF(movimi = 'S', cantid * -1, cantid)), fc FROM postraslados " &
                   $"WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND Local = @local AND estado = '0' GROUP BY {fx} " &
                   $"UNION ALL SELECT d.articu, {fd}, 0, 0, 0, 0, SUM(d.cantid), d.fc FROM posrequisicdev d " &
                   "INNER JOIN posrequisic r ON r.codigo = d.codigo AND r.Articu = d.Articu AND r.Requisic = 1 " &
                   $"WHERE d.fc BETWEEN @desde AND @hasta AND d.articu = @codigo AND d.Local = @local GROUP BY {fd} " &
                   $"UNION ALL SELECT articu, {fx}, 0, 0, 0, SUM(IF(movimi = 0, cantid * -1, cantid)), 0, fc FROM posnotas " &
                   $"WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND Local = @local GROUP BY {fx} " &
                   $"UNION ALL SELECT articulo, {fx}, 0, 0, 0, SUM(cantidad), 0, fc FROM posdevoluciones " &
                   $"WHERE fc BETWEEN @desde AND @hasta AND articulo = @codigo AND Local = @local GROUP BY {fx}) AS S " &
                   "LEFT JOIN posarticulos A ON A.CodigoArt = S.codigoart " &
                   "LEFT JOIN posarticuloslocal L ON L.CArticulo = S.codigoart AND L.CLocal = @local " &
                   "LEFT JOIN posinventariohistorico I ON I.local = @local AND I.fc = DATE_FORMAT(S.fc, '%Y-%m-%d') AND I.codigoart = S.codigoart " &
                   "GROUP BY F1 ORDER BY S.fc"
        End Function

        ''' <summary>Las filas del resumido entre el inicio de <paramref name="desde"/> y el final de <paramref name="hasta"/>.</summary>
        Public Async Function ResumenAsync(codigo As String, local As Long, desde As Date, hasta As Date, diario As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of List(Of FilaResumenMovimientos))
            Dim p = New With {codigo, local, desde, .hasta = hasta.Date.AddDays(1).AddSeconds(-1)}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("SET lc_time_names = 'es_ES'", cancellationToken:=ct)).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of FilaResumenMovimientos)(New CommandDefinition(SqlResumen(diario), p, commandTimeout:=600, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ' ---- Informe detallado (SD_ReptDetallado / RPT0017) ----

        Friend Const SqlDetalle As String =
            "SELECT * FROM (" &
            "SELECT fc AS Fecha, 0 AS Entradas, cantidad AS Salidas, CAST(A.numfac AS CHAR) AS Documento, 'Fac.Venta' AS Tipo FROM posfacturadetalle A " &
            "INNER JOIN posfacturaencabezado B ON A.local = B.local AND A.numfac = B.numfac WHERE fc BETWEEN @desde AND @hasta AND codigoart = @codigo AND estado = '4' AND B.Local = @local " &
            "UNION ALL SELECT fa, cantidad, 0, CAST(A.fact_comp AS CHAR), 'Fac.Compra' FROM poscompraslineas A " &
            "INNER JOIN poscompras B ON A.fact_comp = B.fact_comp AND A.proveedor = B.proveedor WHERE fa BETWEEN @desde AND @hasta AND articulo = @codigo AND Aplicada = '1' AND B.anulada = '0' AND B.Local = @local " &
            "UNION ALL SELECT fc, IF(movimi = 1, cantid, 0), IF(movimi = 0, cantid, 0), CAST(Codigo AS CHAR), 'Ajuste' FROM posajustesinvent WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND Local = @local " &
            "UNION ALL SELECT fc, 0, (cantid - devolucion), CAST(Codigo AS CHAR), 'Requisición' FROM posrequisic WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND (Local = @local OR alocal = @local) AND Requisic = '1' " &
            "UNION ALL SELECT fc, IF(movimi = 'E', cantid, 0), IF(movimi = 'S', cantid, 0), CAST(Codigo AS CHAR), 'Traslado' FROM postraslados WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND Local = @local AND estado = 0 " &
            "UNION ALL SELECT fc, IF(movimi = 1, cantid, 0), IF(movimi = 0, cantid, 0), CAST(Codigo AS CHAR), IF(movimi = 0, 'N.Crédito Comp.', 'N.Débito Comp') FROM posnotas WHERE fc BETWEEN @desde AND @hasta AND articu = @codigo AND Local = @local " &
            "UNION ALL SELECT fc, cantidad, 0, CAST(consec AS CHAR), 'N.Crédito Vent.' FROM posdevoluciones WHERE fc BETWEEN @desde AND @hasta AND articulo = @codigo AND Local = @local" &
            ") A ORDER BY Fecha"

        Public Async Function DetalleAsync(codigo As String, local As Long, desde As Date, hasta As Date, Optional ct As CancellationToken = Nothing) As Task(Of List(Of FilaDetalleMovimientos))
            Dim p = New With {codigo, local, desde, .hasta = hasta.Date.AddDays(1).AddSeconds(-1)}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of FilaDetalleMovimientos)(New CommandDefinition(SqlDetalle, p, commandTimeout:=600, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

    End Class

End Namespace
