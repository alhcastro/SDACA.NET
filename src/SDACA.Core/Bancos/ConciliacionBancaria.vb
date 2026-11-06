Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports SDACA.Core.Datos

Namespace Bancos

    ''' <summary>Tabla de donde salen los movimientos de los pasos 2 y 3.</summary>
    Public Enum OrigenPendiente
        ''' <summary>Paso 2: están en el banco y no en libros. Van a ccblibro, ya conciliados.</summary>
        Banco
        ''' <summary>Paso 3: su fecha cayó fuera del rango. Vuelven a ccblibrobanco.</summary>
        Errores
    End Enum

    Public Enum FiltroPendiente
        Documento
        Fecha
        Detalle
    End Enum

    Public Enum OrdenInforme
        CreditosDocumento
        FechaLibrosCreditosDocumento
        FechaBancoCreditosDocumento
        DebitosDocumento
        FechaLibrosDebitosDocumento
        FechaBancoDebitosDocumento
    End Enum

    ''' <summary>Fila de los pasos 2, 3 y 4.</summary>
    Public NotInheritable Class MovimientoPendiente
        Public Property Fecha As Date
        Public Property Documento As String = ""
        Public Property Tipo As Integer
        Public Property Monto As Double
        Public Property Detalle As String = ""
    End Class

    Public NotInheritable Class ResultadoImportacion
        Public Property Insertados As Integer
        Public Property Omitidos As Integer
    End Class

    ''' <summary>Paso 4: lo que el usuario digitó para un movimiento de libros.</summary>
    Public NotInheritable Class ConciliacionManual
        Public Property Movimiento As MovimientoPendiente
        Public Property BancoFecha As Date
        Public Property BancoDetalle As String = ""
    End Class

    ''' <summary>
    ''' Conciliación bancaria. Migración de SD1401\FrmConciliacion.frm, FrmConciliacion2.frm,
    ''' FrmMovBancoaLibros.frm, FrmMovBancoaMal.frm, FrmConciliarManual.frm y REP\FrmConciliacionRep.frm.
    ''' Como en VB6 no se replica. A diferencia de VB6, cada paso que cambia ccblibro rehace los cierres.
    ''' </summary>
    Public NotInheritable Class ConciliacionBancaria

        Private Const ClaveDuplicada As Integer = 1062

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>Las transferencias entre cuentas van a ccblibrobancort y no se concilian.</summary>
        Public Shared Function Tabla(fecha As Date, inicio As Date, fin As Date, detalle As String) As String
            Dim inicial = detalle.Trim()
            If inicial.StartsWith("RECIBE DE", StringComparison.Ordinal) OrElse inicial.StartsWith("TRANSF. A", StringComparison.Ordinal) Then Return "ccblibrobancort"
            Return If(fecha >= inicio AndAlso fecha <= fin, "ccblibrobanco", "ccblibrobancomal")
        End Function

        Public Async Function ObtenerColumnasAsync(banco As Long, Optional ct As CancellationToken = Nothing) As Task(Of ColumnasCsv)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim texto = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT finport FROM gatbancos WHERE banco = @banco", New With {banco}, cancellationToken:=ct)).ConfigureAwait(False)
                Return ColumnasCsv.DesdeFinport(texto)
            End Using
        End Function

        Public Async Function GuardarColumnasAsync(banco As Long, columnas As ColumnasCsv, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("UPDATE gatbancos SET finport = @finport WHERE banco = @banco", New With {banco, .finport = columnas.AFinport()}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' CCBCopyExcel. Un movimiento repetido se omite, como en VB6. Con
        ''' <paramref name="renombrarRepetidos"/>, como el CSV de VB6, se reintenta agregando al documento
        ''' el número de la fecha.
        ''' </summary>
        Public Async Function ImportarAsync(movimientos As IEnumerable(Of MovimientoArchivo), local As Long, banco As Long, cuenta As String, inicio As Date, fin As Date, renombrarRepetidos As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of ResultadoImportacion)
            Const borrar As String = "DELETE FROM ccblibrobancort WHERE banco = @banco AND cuenta = @cuenta AND fecha BETWEEN @inicio AND @fin"
            Dim resultado As New ResultadoImportacion()
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(borrar, New With {banco, cuenta, inicio, fin}, cancellationToken:=ct)).ConfigureAwait(False)
                For Each m In movimientos
                    Dim sql = $"INSERT INTO {Tabla(m.Fecha, inicio, fin, m.Detalle)} VALUES (@local, @banco, @cuenta, @Fecha, @documento, @Tipo, @Monto, @Detalle, @TipoMov)"
                    Dim fila = New With {local, banco, cuenta, m.Fecha, .documento = m.Documento, m.Tipo, m.Monto, m.Detalle, m.TipoMov}
                    Try
                        Await cn.ExecuteAsync(New CommandDefinition(sql, fila, cancellationToken:=ct)).ConfigureAwait(False)
                        resultado.Insertados += 1
                    Catch ex As MySqlException When ex.Number = ClaveDuplicada
                        If Not renombrarRepetidos Then
                            resultado.Omitidos += 1
                            Continue For
                        End If
                        Dim otra = New With {local, banco, cuenta, m.Fecha, .documento = m.Documento & CLng(m.Fecha.ToOADate()), m.Tipo, m.Monto, m.Detalle, m.TipoMov}
                        Try
                            cn.Execute(New CommandDefinition(sql, otra, cancellationToken:=ct))
                            resultado.Insertados += 1
                        Catch ex2 As MySqlException When ex2.Number = ClaveDuplicada
                            resultado.Omitidos += 1
                        End Try
                    End Try
                Next
            End Using
            Return resultado
        End Function

        ''' <summary>
        ''' CCBConciliar. Cruza por documento, tipo y monto. Lo que ya estaba conciliado solo sale de
        ''' ccblibrobanco. TEC compara con sdredondear y CSV con ROUND, como en cada pantalla de VB6.
        ''' </summary>
        ''' <param name="migrar">chkmigbanco: lo que quede en el banco pasa a libros como conciliado.</param>
        ''' <returns>Movimientos de libros conciliados.</returns>
        Public Async Function ConciliarAsync(local As Long, banco As Long, cuenta As String, inicio As Date, hoy As Date, redondearConFuncion As Boolean, migrar As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Const actualizar As String = "UPDATE ccblibro SET conciliado = '1', bancofecha = @Fecha, bancodetalle = @Detalle WHERE banco = @banco AND cuenta = @cuenta AND documento = @Documento AND ROUND(monto,2) = ROUND(@Monto,2) AND conciliado = '0'"
            Const borrar As String = "DELETE FROM ccblibrobanco WHERE banco = @banco AND cuenta = @cuenta AND documento = @Documento AND monto = @Monto AND fecha = @Fecha"
            Const migrarSql As String = "INSERT INTO ccblibro SELECT NULL, local, banco, cuenta, fecha, documento, tipomov, tipo, monto, detalle, 1, fecha, detalle, 0, NOW() FROM ccblibrobanco WHERE banco = @banco AND cuenta = @cuenta"
            Const vaciar As String = "DELETE FROM ccblibrobanco WHERE banco = @banco AND cuenta = @cuenta"
            Const primera As String = "SELECT MIN(fecha) FROM ccblibrobanco WHERE banco = @banco AND cuenta = @cuenta"
            Dim llave = New With {banco, cuenta}
            Dim conciliados = 0
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim desde = inicio
                Dim masVieja = Await cn.ExecuteScalarAsync(Of Date?)(New CommandDefinition(primera, llave, cancellationToken:=ct)).ConfigureAwait(False)
                If masVieja.HasValue AndAlso masVieja.Value < desde Then desde = masVieja.Value

                For Each m In Await CoincidenciasAsync(cn, banco, cuenta, "0", redondearConFuncion, ct).ConfigureAwait(False)
                    Dim fila = New With {banco, cuenta, m.Documento, m.Fecha, m.Detalle, m.Monto}
                    conciliados += Await cn.ExecuteAsync(New CommandDefinition(actualizar, fila, cancellationToken:=ct)).ConfigureAwait(False)
                    Await cn.ExecuteAsync(New CommandDefinition(borrar, fila, cancellationToken:=ct)).ConfigureAwait(False)
                Next
                For Each m In Await CoincidenciasAsync(cn, banco, cuenta, "1", redondearConFuncion, ct).ConfigureAwait(False)
                    Await cn.ExecuteAsync(New CommandDefinition(borrar, New With {banco, cuenta, m.Documento, m.Monto, m.Fecha}, cancellationToken:=ct)).ConfigureAwait(False)
                Next
                If migrar Then
                    Await cn.ExecuteAsync(New CommandDefinition(migrarSql, llave, cancellationToken:=ct)).ConfigureAwait(False)
                    Await cn.ExecuteAsync(New CommandDefinition(vaciar, llave, cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Await CierresDiarios.RecalcularAsync(cn, local, banco, cuenta, desde, hoy, ct).ConfigureAwait(False)
            End Using
            Return conciliados
        End Function

        Private Shared Async Function CoincidenciasAsync(cn As MySqlConnection, banco As Long, cuenta As String, conciliado As String, redondearConFuncion As Boolean, ct As CancellationToken) As Task(Of List(Of MovimientoPendiente))
            Dim monto = If(redondearConFuncion, "ROUND(b.monto,2) = ROUND(sdredondear(l.monto,0.01,0),2)", "ROUND(l.monto,2) = ROUND(b.monto,2)")
            Dim sql = "SELECT l.documento AS Documento, b.fecha AS Fecha, b.detalle AS Detalle, b.monto AS Monto FROM ccblibro l " &
                      "INNER JOIN ccblibrobanco b ON (l.banco = b.banco AND l.cuenta = b.cuenta AND l.documento = b.documento AND l.tipo = b.tipo) " &
                      $"WHERE l.banco = @banco AND l.cuenta = @cuenta AND l.conciliado = @conciliado AND {monto}"
            Return (Await cn.QueryAsync(Of MovimientoPendiente)(New CommandDefinition(sql, New With {banco, cuenta, conciliado}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
        End Function

        Private Shared Function Filtro(campo As FiltroPendiente, valor As String) As String
            Select Case campo
                Case FiltroPendiente.Fecha
                    Return " AND fecha = @valor"
                Case FiltroPendiente.Detalle
                    Return " AND detalle LIKE CONCAT('%',@valor,'%')"
                Case Else
                    Return If(String.IsNullOrEmpty(valor), "", " AND documento = @valor")
            End Select
        End Function

        Private Shared Function NombreTabla(origen As OrigenPendiente) As String
            Return If(origen = OrigenPendiente.Banco, "ccblibrobanco", "ccblibrobancomal")
        End Function

        Public Async Function ListarPendientesAsync(origen As OrigenPendiente, banco As Long, cuenta As String, campo As FiltroPendiente, valor As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of MovimientoPendiente))
            Dim sql = "SELECT fecha AS Fecha, documento AS Documento, CAST(tipo AS UNSIGNED) AS Tipo, monto AS Monto, detalle AS Detalle " &
                      $"FROM {NombreTabla(origen)} WHERE banco = @banco AND cuenta = @cuenta{Filtro(campo, valor)} ORDER BY fecha DESC"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of MovimientoPendiente)(New CommandDefinition(sql, New With {banco, cuenta, valor}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>
        ''' CCBSalvarLibro de los pasos 2 y 3. El paso 3 de VB6 manda el tipo dos veces y corre las columnas:
        ''' el monto queda en 0 o 1. Aquí sigue la estructura de la tabla.
        ''' </summary>
        ''' <param name="original">Movimiento como está guardado; <paramref name="editado"/> trae lo que se aplica.</param>
        Public Async Function AplicarAsync(origen As OrigenPendiente, original As MovimientoPendiente, editado As MovimientoPendiente, local As Long, banco As Long, cuenta As String, uid As Long, hoy As Date, Optional ct As CancellationToken = Nothing) As Task
            Const libro As String = "INSERT INTO ccblibro VALUES (NULL, @local, @banco, @cuenta, @Fecha, @Documento, @Tipo, @Tipo, @Monto, @Detalle, '1', @Fecha, @Detalle, @uid, NOW())"
            Const bancoSql As String = "INSERT INTO ccblibrobanco VALUES (@local, @banco, @cuenta, @Fecha, @Documento, @Tipo, @Monto, @Detalle, @Tipo)"
            Dim fila = New With {local, banco, cuenta, editado.Fecha, editado.Documento, editado.Tipo, editado.Monto, editado.Detalle, uid}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(If(origen = OrigenPendiente.Banco, libro, bancoSql), fila, cancellationToken:=ct)).ConfigureAwait(False)
                Await QuitarAsync(cn, origen, original, banco, cuenta, ct).ConfigureAwait(False)
                If origen = OrigenPendiente.Banco Then Await CierresDiarios.RecalcularAsync(cn, local, banco, cuenta, editado.Fecha, hoy, ct).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function BorrarPendienteAsync(origen As OrigenPendiente, movimiento As MovimientoPendiente, banco As Long, cuenta As String, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await QuitarAsync(cn, origen, movimiento, banco, cuenta, ct).ConfigureAwait(False)
            End Using
        End Function

        Private Shared Async Function QuitarAsync(cn As MySqlConnection, origen As OrigenPendiente, m As MovimientoPendiente, banco As Long, cuenta As String, ct As CancellationToken) As Task
            Dim sql = $"DELETE FROM {NombreTabla(origen)} WHERE banco = @banco AND cuenta = @cuenta AND documento = @Documento AND fecha = @Fecha AND monto = @Monto"
            Await cn.ExecuteAsync(New CommandDefinition(sql, New With {banco, cuenta, m.Documento, m.Fecha, m.Monto}, cancellationToken:=ct)).ConfigureAwait(False)
        End Function

        Public Async Function ListarSinConciliarAsync(banco As Long, cuenta As String, campo As FiltroPendiente, valor As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of MovimientoPendiente))
            Dim sql = "SELECT fecha AS Fecha, documento AS Documento, CAST(tipo AS UNSIGNED) AS Tipo, monto AS Monto, detalle AS Detalle " &
                      $"FROM ccblibro WHERE conciliado = '0' AND banco = @banco AND cuenta = @cuenta{Filtro(campo, valor)} ORDER BY fecha DESC"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of MovimientoPendiente)(New CommandDefinition(sql, New With {banco, cuenta, valor}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>Paso 4. Conciliar no cambia el saldo, pero sí lo que está en tránsito.</summary>
        Public Async Function ConciliarManualAsync(lineas As IReadOnlyList(Of ConciliacionManual), local As Long, banco As Long, cuenta As String, hoy As Date, Optional ct As CancellationToken = Nothing) As Task
            Const sql As String = "UPDATE ccblibro SET bancofecha = @BancoFecha, bancodetalle = @BancoDetalle, conciliado = '1' WHERE banco = @banco AND cuenta = @cuenta AND documento = @Documento AND fecha = @Fecha AND monto = @Monto AND conciliado = '0'"
            If lineas.Count = 0 Then Return
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                For Each l In lineas
                    Dim fila = New With {banco, cuenta, l.BancoFecha, l.BancoDetalle, l.Movimiento.Documento, l.Movimiento.Fecha, l.Movimiento.Monto}
                    Await cn.ExecuteAsync(New CommandDefinition(sql, fila, cancellationToken:=ct)).ConfigureAwait(False)
                Next
                Dim desde = lineas.Min(Function(l) If(l.BancoFecha < l.Movimiento.Fecha, l.BancoFecha, l.Movimiento.Fecha))
                Await CierresDiarios.RecalcularAsync(cn, local, banco, cuenta, desde, hoy, ct).ConfigureAwait(False)
            End Using
        End Function

        Friend Shared Function OrdenSql(orden As OrdenInforme) As String
            Select Case orden
                Case OrdenInforme.FechaLibrosCreditosDocumento : Return "fecha, tipo DESC, CAST(documento AS UNSIGNED)"
                Case OrdenInforme.FechaBancoCreditosDocumento : Return "bancofecha, tipo DESC, CAST(documento AS UNSIGNED)"
                Case OrdenInforme.DebitosDocumento : Return "tipo, CAST(documento AS UNSIGNED)"
                Case OrdenInforme.FechaLibrosDebitosDocumento : Return "fecha, tipo, CAST(documento AS UNSIGNED)"
                Case OrdenInforme.FechaBancoDebitosDocumento : Return "bancofecha, tipo, CAST(documento AS UNSIGNED)"
                Case Else : Return "tipo DESC, CAST(documento AS UNSIGNED)"
            End Select
        End Function

        ''' <summary>Datos de RPT0004 y sus dos subreportes RPT0005.</summary>
        Public Async Function InformeAsync(banco As Long, cuenta As String, inicio As Date, fin As Date, orden As OrdenInforme, soloSinConciliar As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of InformeConciliacion)
            Dim filas = "SELECT fecha AS Fecha, documento AS Documento, CAST(tipo AS UNSIGNED) AS Tipo, monto AS Monto, IFNULL(detalle,'') AS Detalle, conciliado = '1' AS Conciliado, bancofecha AS BancoFecha, IFNULL(bancodetalle,'') AS BancoDetalle " &
                        "FROM ccblibro WHERE banco = @banco AND cuenta = @cuenta AND ((fecha BETWEEN @inicio AND @fin OR bancofecha BETWEEN @inicio AND @fin)" & If(soloSinConciliar, " AND conciliado = '0'", "") & ") " &
                        "UNION ALL SELECT fecha, documento, CAST(tipo AS UNSIGNED), monto, '', 0, fecha, detalle FROM ccblibrobanco WHERE banco = @banco AND cuenta = @cuenta AND fecha BETWEEN @inicio AND @fin " &
                        $"ORDER BY {OrdenSql(orden)}"
            Const anterior As String = "SELECT IFNULL((SELECT ccd.{0} FROM ccbcierrediario ccd WHERE ccd.banco = b.banco AND ccd.cuenta = b.cuenta AND ccd.fecha < @inicio ORDER BY ccd.fecha DESC LIMIT 1), b.{1}) FROM ccbcuentas b WHERE b.banco = @banco AND b.cuenta = @cuenta"
            Const aplicados As String = "SELECT IFNULL(SUM(monto),0) * -1 FROM ccblibro WHERE banco = @banco AND cuenta = @cuenta AND fecha < @inicio AND bancofecha BETWEEN @inicio AND @fin AND tipo = @tipo"
            Const actuales As String = "SELECT IFNULL(SUM(monto),0) FROM ccblibro WHERE banco = @banco AND cuenta = @cuenta AND fecha BETWEEN @inicio AND @fin AND (bancofecha > @fin OR bancofecha = '1900-01-01') AND tipo = @tipo"
            Const transito As String = "SELECT fecha AS Fecha, documento AS Documento, CAST(tipo AS UNSIGNED) AS Tipo, monto AS Monto, IFNULL(detalle,'') AS Detalle, conciliado = '1' AS Conciliado, bancofecha AS BancoFecha, IFNULL(bancodetalle,'') AS BancoDetalle " &
                                       "FROM ccblibro WHERE banco = @banco AND cuenta = @cuenta AND fecha <= @fin AND (bancofecha > @fin OR bancofecha = '1900-01-01') AND tipo = @tipo ORDER BY fecha, documento"
            Dim llave = New With {banco, cuenta, inicio, fin}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim uno = Async Function(sql As String, tipo As String) As Task(Of Double)
                              Return Await cn.ExecuteScalarAsync(Of Double)(New CommandDefinition(sql, New With {banco, cuenta, inicio, fin, tipo}, cancellationToken:=ct)).ConfigureAwait(False)
                          End Function
                Dim datos As New DatosInforme With {
                    .Inicio = inicio, .Fin = fin,
                    .MontoInicial = Await uno(String.Format(anterior, "monto", "montoinicial"), "0").ConfigureAwait(False),
                    .ChequesAnteriores = Await uno(String.Format(anterior, "transito", "montoinicialtransito"), "0").ConfigureAwait(False),
                    .DepositosAnteriores = Await uno(String.Format(anterior, "deptransito", "montoinicialdeptransito"), "0").ConfigureAwait(False),
                    .ChequesAplicados = Await uno(aplicados, "0").ConfigureAwait(False),
                    .DepositosAplicados = Await uno(aplicados, "1").ConfigureAwait(False),
                    .ChequesActuales = Await uno(actuales, "0").ConfigureAwait(False),
                    .DepositosActuales = Await uno(actuales, "1").ConfigureAwait(False),
                    .Filas = (Await cn.QueryAsync(Of FilaConciliacion)(New CommandDefinition(filas, llave, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                }
                Dim informe = InformeConciliacion.Calcular(datos)
                informe.ChequesTransito = (Await cn.QueryAsync(Of FilaConciliacion)(New CommandDefinition(transito, New With {banco, cuenta, fin, .tipo = "0"}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                informe.DepositosTransito = (Await cn.QueryAsync(Of FilaConciliacion)(New CommandDefinition(transito, New With {banco, cuenta, fin, .tipo = "1"}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                Return informe
            End Using
        End Function

    End Class

    Public NotInheritable Class FilaConciliacion
        Public Property Fecha As Date
        Public Property Documento As String = ""
        Public Property Tipo As Integer
        Public Property Monto As Double
        Public Property Detalle As String = ""
        Public Property Conciliado As Boolean
        Public Property BancoFecha As Date
        Public Property BancoDetalle As String = ""
    End Class

    ''' <summary>Lo que el informe lee de la base antes de calcular.</summary>
    Public NotInheritable Class DatosInforme
        Public Property Inicio As Date
        Public Property Fin As Date
        Public Property MontoInicial As Double
        Public Property ChequesAnteriores As Double
        Public Property DepositosAnteriores As Double
        Public Property ChequesAplicados As Double
        Public Property DepositosAplicados As Double
        Public Property ChequesActuales As Double
        Public Property DepositosActuales As Double
        Public Property Filas As New List(Of FilaConciliacion)
    End Class

    ''' <summary>Totales de RPT0004. Ver GroupFooter2_Format y Detail_Format.</summary>
    Public NotInheritable Class InformeConciliacion
        Public Property Datos As DatosInforme
        Public Property TotalDebitos As Double
        Public Property TotalCreditos As Double
        Public Property CreditosConciliados As Double
        Public Property DebitosConciliados As Double
        Public Property SaldoLibros As Double
        Public Property ChequesTotal As Double
        Public Property DepositosTotal As Double
        Public Property SaldoBanco As Double
        Public Property ChequesTransito As New List(Of FilaConciliacion)
        Public Property DepositosTransito As New List(Of FilaConciliacion)

        Public Shared Function Calcular(d As DatosInforme) As InformeConciliacion
            Dim debitos = d.Filas.Where(Function(f) f.Tipo = 0).Sum(Function(f) f.Monto)
            Dim creditos = d.Filas.Where(Function(f) f.Tipo <> 0).Sum(Function(f) f.Monto)
            ' Lo que el banco registró este mes pero es de meses anteriores ya está en el saldo inicial.
            Dim hayCheques = d.Filas.Any(Function(f) f.Tipo = 0 AndAlso f.Conciliado AndAlso f.Fecha < d.Inicio)
            Dim hayDepositos = d.Filas.Any(Function(f) f.Tipo <> 0 AndAlso f.Conciliado AndAlso f.Fecha < d.Inicio)
            Dim creditosLibro = If(hayDepositos, creditos + d.DepositosAplicados, creditos)
            Dim debitosLibro = If(hayCheques, debitos + d.ChequesAplicados, debitos)
            Dim saldoLibros = d.MontoInicial + creditosLibro - debitosLibro
            Dim cheques = d.ChequesAnteriores + d.ChequesAplicados + d.ChequesActuales
            Dim depositos = d.DepositosAnteriores + d.DepositosAplicados + d.DepositosActuales
            Return New InformeConciliacion With {
                .Datos = d, .TotalDebitos = debitos, .TotalCreditos = creditos,
                .CreditosConciliados = creditos - d.DepositosActuales, .DebitosConciliados = debitos - d.ChequesActuales,
                .SaldoLibros = saldoLibros, .ChequesTotal = cheques, .DepositosTotal = depositos,
                .SaldoBanco = saldoLibros + cheques - depositos
            }
        End Function
    End Class

End Namespace
