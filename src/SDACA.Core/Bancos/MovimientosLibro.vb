Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports SDACA.Core.Cheques
Imports SDACA.Core.Datos

Namespace Bancos

    ''' <summary>Fila de <c>ccblibro</c>.</summary>
    Public NotInheritable Class MovimientoBanco
        Public Const FechaSinRegistro As String = "1900-01-01"

        ''' <summary>0 mientras el movimiento no se ha guardado.</summary>
        Public Property Ids As Long
        Public Property Local As Long
        Public Property Banco As Long
        Public Property Cuenta As String = ""
        Public Property Fecha As Date
        Public Property Documento As String = ""
        Public Property TipoMov As Long
        ''' <summary><see cref="TipoMovimiento.Resta"/> o <see cref="TipoMovimiento.Suma"/>.</summary>
        Public Property Tipo As Integer
        Public Property Monto As Decimal
        Public Property Detalle As String = ""
        Public Property Conciliado As Boolean
        Public Property BancoFecha As Date
        Public Property BancoDetalle As String = ""
    End Class

    ''' <summary>Línea del asiento en el comprobante.</summary>
    Public NotInheritable Class LineaComprobante
        Public Property Cuenta As String = ""
        Public Property Descripcion As String = ""
        Public Property Debe As Decimal
        Public Property Haber As Decimal
    End Class

    ''' <summary>Datos de RPT0010, el Comprobante de Movimiento Bancario.</summary>
    Public NotInheritable Class ComprobanteMovimiento
        Public Property Ids As Long
        Public Property Fecha As Date
        Public Property Tipo As String = ""
        Public Property Departamento As String = ""
        Public Property Banco As String = ""
        Public Property Cuenta As String = ""
        Public Property Documento As String = ""
        Public Property Monto As Decimal
        Public Property Detalle As String = ""
        Public Property Lineas As New List(Of LineaComprobante)

        Public ReadOnly Property MontoLetras As String
            Get
                Return MontoEnLetras.Convertir(CDbl(Monto), True, True)
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Movimientos del libro. Migración de SD1401\FrmLibro.frm, con el esquema real: la base no tiene
    ''' ccbconceptos ni la columna ccbconta.mo que espera Fuentes. No se migra el Registro Contable:
    ''' ccbconta no recibe asientos con montos desde 2009, y al editar se conservan los que ya tenga.
    ''' </summary>
    Public NotInheritable Class MovimientosLibro

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>Consulta del botón Buscar. La fecha evita traer los cientos de miles de filas de una cuenta.</summary>
        Public Async Function BuscarAsync(banco As Long, cuenta As String, desde As Date, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT a.ids, a.local, a.documento AS Documento, b.detalle AS Tipo, a.fecha AS Fecha, a.monto AS Monto, a.detalle AS Detalle, " &
                                  "IF(a.conciliado = 0,'NO','SI') AS Conciliado, " &
                                  "IF(IFNULL((SELECT IFNULL(IF(SUM(md) = SUM(mh) AND SUM(md) != 0 AND SUM(mh) != 0,1,0),0) FROM ccbconta WHERE ids = a.ids GROUP BY ids),0) > 0 OR a.tipomov = 2,'SI','NO') AS Conta " &
                                  "FROM ccblibro a INNER JOIN ccbtiposmov b ON (b.tipo = a.tipomov) " &
                                  "WHERE a.banco = @banco AND a.cuenta = @cuenta AND a.fecha >= @desde ORDER BY a.fecha DESC, a.ids DESC"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {banco, cuenta, desde}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim datos As New DataTable()
                    datos.Load(lector)
                    Return datos
                End Using
            End Using
        End Function

        Public Async Function ObtenerAsync(local As Long, ids As Long, banco As Long, cuenta As String, Optional ct As CancellationToken = Nothing) As Task(Of MovimientoBanco)
            Const sql As String = "SELECT ids AS Ids, local AS Local, banco AS Banco, cuenta AS Cuenta, fecha AS Fecha, documento AS Documento, tipomov AS TipoMov, " &
                                  "CAST(tipo AS UNSIGNED) AS Tipo, monto AS Monto, IFNULL(detalle,'') AS Detalle, conciliado = '1' AS Conciliado, " &
                                  "IF(bancofecha < '1900-01-01','1900-01-01',bancofecha) AS BancoFecha, IFNULL(bancodetalle,'') AS BancoDetalle " &
                                  "FROM ccblibro WHERE local = @local AND ids = @ids AND banco = @banco AND cuenta = @cuenta"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of MovimientoBanco)(New CommandDefinition(sql, New With {local, ids, banco, cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Consulta de SD_PrintReport. Allá el asiento se busca solo por ids; aquí también por banco y cuenta.</summary>
        Public Async Function ObtenerComprobanteAsync(local As Long, ids As Long, banco As Long, cuenta As String, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of ComprobanteMovimiento)
            Const datos As String = "SELECT a.ids AS Ids, a.fecha AS Fecha, IFNULL(b.detalle,'') AS Tipo, IFNULL(l.Descripcion,'') AS Departamento, IFNULL(e.Descripcion,'') AS Banco, " &
                                    "CONCAT(a.cuenta,' - ',IFNULL(d.Descripcion,'')) AS Cuenta, a.documento AS Documento, a.monto AS Monto, IFNULL(a.detalle,'') AS Detalle " &
                                    "FROM ccblibro a LEFT JOIN ccbcuentas d ON (d.banco = a.banco AND d.cuenta = a.cuenta) LEFT JOIN gatbancos e ON (e.Banco = a.banco) " &
                                    "LEFT JOIN gatlocales l ON (l.Local = a.local) LEFT JOIN ccbtiposmov b ON (b.tipo = a.tipomov) " &
                                    "WHERE a.local = @local AND a.ids = @ids AND a.banco = @banco AND a.cuenta = @cuenta"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim parametros = New With {local, ids, banco, cuenta}
                Dim comprobante = Await cn.QueryFirstOrDefaultAsync(Of ComprobanteMovimiento)(New CommandDefinition(datos, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                If comprobante Is Nothing Then Return Nothing
                Dim catalogo = Await EmisionCheques.CatalogoAsync(cn, cedula, ct).ConfigureAwait(False)
                Dim asiento = "SELECT c.ccontable AS Cuenta, IFNULL(s.descripcion,'') AS Descripcion, c.md AS Debe, c.mh AS Haber FROM ccbconta c " &
                              $"LEFT JOIN {catalogo}sgccatalogo s ON (s.codigo = c.ccontable) WHERE c.ids = @ids AND c.banco = @banco AND c.cuenta = @cuenta ORDER BY c.System"
                comprobante.Lineas = (Await cn.QueryAsync(Of LineaComprobante)(New CommandDefinition(asiento, parametros, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                Return comprobante
            End Using
        End Function

        ''' <summary>True si ya hay un movimiento igual. El usuario decide si lo agrega de todos modos.</summary>
        Public Async Function ExisteAsync(movimiento As MovimientoBanco, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Const sql As String = "SELECT COUNT(*) FROM ccblibro WHERE banco = @Banco AND cuenta = @Cuenta AND fecha = @Fecha AND documento = @Documento AND tipomov = @TipoMov AND monto = @Monto"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(sql, movimiento, cancellationToken:=ct)).ConfigureAwait(False) > 0
            End Using
        End Function

        ''' <summary>
        ''' CCBSaveLibro. Las sentencias van como texto porque el mismo texto se replica. Al cambiar la
        ''' fecha, los cierres se rehacen desde la más vieja de las dos: VB6 solo usa la nueva.
        ''' </summary>
        ''' <param name="fechaAnterior">Fecha guardada del movimiento que se edita.</param>
        Public Async Function GuardarAsync(movimiento As MovimientoBanco, localSesion As Long, uid As Long, hoy As Date, Optional fechaAnterior As Date? = Nothing, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim sql As String
                If movimiento.Ids = 0 Then
                    movimiento.Ids = CLng(Await Consecutivo.SiguienteAsync(cn, "ccblibro", "ids", localSesion, ct:=ct).ConfigureAwait(False))
                    movimiento.Local = localSesion
                    sql = SentenciaInsertar(movimiento, uid)
                Else
                    sql = SentenciaActualizar(movimiento)
                End If
                Await LibroBancos.EjecutarAsync(cn, sql, localSesion, ct).ConfigureAwait(False)
                Dim desde = If(fechaAnterior.HasValue AndAlso fechaAnterior.Value < movimiento.Fecha, fechaAnterior.Value, movimiento.Fecha)
                Await CierresDiarios.RecalcularAsync(cn, localSesion, movimiento.Banco, movimiento.Cuenta, desde, hoy, ct).ConfigureAwait(False)
                Return movimiento.Ids
            End Using
        End Function

        ''' <summary>
        ''' Con el mes abierto borra el movimiento y con el mes cerrado lo revierte, igual que al anular
        ''' un cheque. CCBBorrarLibro lo borra siempre, sin replicar ni rehacer los cierres.
        ''' </summary>
        Public Async Function BorrarAsync(movimiento As MovimientoBanco, localSesion As Long, uid As Long, hoy As Date, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim borrado = Await LibroBancos.AnularAsync(cn, movimiento.Banco, movimiento.Cuenta, movimiento.Ids, localSesion, uid, ct).ConfigureAwait(False)
                If borrado Then Await CierresDiarios.RecalcularAsync(cn, localSesion, movimiento.Banco, movimiento.Cuenta, movimiento.Fecha, hoy, ct).ConfigureAwait(False)
                Return borrado
            End Using
        End Function

        Friend Shared Function SentenciaInsertar(m As MovimientoBanco, uid As Long) As String
            Return "INSERT INTO ccblibro VALUES (" &
                   $"{m.Ids},{m.Local},{m.Banco},'{LibroBancos.Literal(m.Cuenta)}','{Fecha(m.Fecha)}','{LibroBancos.Literal(m.Documento)}',{m.TipoMov},{m.Tipo},{Monto(m.Monto)}," &
                   $"'{LibroBancos.Literal(m.Detalle)}',{Conciliacion(m)},{uid},NOW())"
        End Function

        Friend Shared Function SentenciaActualizar(m As MovimientoBanco) As String
            Dim bancoFecha = If(m.Conciliado, Fecha(m.BancoFecha), MovimientoBanco.FechaSinRegistro)
            Dim bancoDetalle = If(m.Conciliado, LibroBancos.Literal(m.BancoDetalle), "")
            Return $"UPDATE ccblibro SET fecha = '{Fecha(m.Fecha)}', documento = '{LibroBancos.Literal(m.Documento)}', tipomov = {m.TipoMov}, tipo = {m.Tipo}, monto = {Monto(m.Monto)}, " &
                   $"detalle = '{LibroBancos.Literal(m.Detalle)}', conciliado = {If(m.Conciliado, 1, 0)}, bancofecha = '{bancoFecha}', bancodetalle = '{bancoDetalle}' " &
                   $"WHERE local = {m.Local} AND ids = {m.Ids} AND banco = {m.Banco} AND cuenta = '{LibroBancos.Literal(m.Cuenta)}'"
        End Function

        Private Shared Function Conciliacion(m As MovimientoBanco) As String
            If Not m.Conciliado Then Return $"'0','{MovimientoBanco.FechaSinRegistro}',''"
            Return $"'1','{Fecha(m.BancoFecha)}','{LibroBancos.Literal(m.BancoDetalle)}'"
        End Function

        Private Shared Function Fecha(valor As Date) As String
            Return valor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        End Function

        Private Shared Function Monto(valor As Decimal) As String
            Return valor.ToString(CultureInfo.InvariantCulture)
        End Function

    End Class

End Namespace
