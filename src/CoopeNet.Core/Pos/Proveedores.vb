Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una cuenta del proveedor en cxpcuenprov, más el estado de la pantalla (nueva, modificada o por borrar).</summary>
    Public NotInheritable Class CuentaProveedor
        ''' <summary>CTAPROV: único en todos los locales.</summary>
        Public Property Codigo As String = ""
        ''' <summary>El CTAPROV con que se cargó; vacío en una cuenta nueva. Si cambia, también se cambia en cxp.</summary>
        Public Property CodigoOriginal As String = ""
        Public Property Descripcion As String = ""
        Public Property TipoPago As Integer = 1
        ''' <summary>Con cheque, la cuenta bancaria de la empresa (ccbcuentas); si no, la del proveedor.</summary>
        Public Property Cuenta As String = ""
        Public Property Banco As Integer
        Public Property Confirmacion As String = ""
        ''' <summary>Días de pago: bit 0 domingo, bit 1 lunes... bit 6 sábado.</summary>
        Public Property DiaPago As Integer
        ''' <summary>Periodicidad de visita, en días.</summary>
        Public Property DiaVisita As Integer = 7
        Public Property Credito As Double
        Public Property EncargadoCompras As String = ""
        Public Property Agente As String = ""
        ''' <summary>El repartidor.</summary>
        Public Property Vendedor As String = ""
        Public Property Moneda As Integer
        Public Property DiasPlazo As Integer = 30
        ''' <summary>Nothing si nunca se le ha pagado (1900-01-01).</summary>
        Public Property FUltPago As Date?
        Public Property Observaciones As String = ""
        Public Property DiasEntrega As Integer = 7
        ''' <summary>Cuenta contable (código completo de sgccatalogo).</summary>
        Public Property CC As String = ""
        ''' <summary>A bueno, B regular, C malo o vacío.</summary>
        Public Property Categ As String = ""
        Public Property Devoluciones As Boolean
        Public Property MontoPromocional As Double

        Public ReadOnly Property Nueva As Boolean
            Get
                Return CodigoOriginal.Length = 0
            End Get
        End Property

        Public Property Modificada As Boolean
        Public Property Borrar As Boolean

        Public Function Copiar() As CuentaProveedor
            Return CType(MemberwiseClone(), CuentaProveedor)
        End Function
    End Class

    ''' <summary>Elemento de los combos de la pantalla: "código - descripción".</summary>
    Public NotInheritable Class OpcionProveedor
        Public Property Codigo As String = ""
        Public Property Descripcion As String = ""
        Public Overrides Function ToString() As String
            Return $"{Codigo} - {Descripcion}"
        End Function
    End Class

    Public NotInheritable Class CatalogosProveedor
        Public Property Locales As New List(Of OpcionProveedor)
        Public Property Bancos As New List(Of OpcionProveedor)
        Public Property Monedas As New List(Of OpcionProveedor)
        Public Property TiposPago As New List(Of OpcionProveedor)
    End Class

    ''' <summary>La persona o empresa de Datos personales que se registra como proveedor, con su ubicación.</summary>
    Public NotInheritable Class PersonaProveedor
        Public Property Id As String = ""
        Public Property Nombre As String = ""
        Public Property Provincia As String = ""
        Public Property Canton As String = ""
        Public Property Distrito As String = ""
        Public Property Sector As String = ""
    End Class

    Public NotInheritable Class ContactoProveedor
        Public Property Tipo As String = ""
        Public Property Dato As String = ""
    End Class

    Public NotInheritable Class CuentaContableProveedor
        Public Property Codigo As String = ""
        Public Property Descripcion As String = ""
        Public Property Auxiliar As String = ""
    End Class

    ''' <summary>
    ''' Proveedores y sus cuentas de pago. Migración de SD0502\FrmMantProveedores.frm (Cargar_Informacion,
    ''' Cargar_Cuentas, SD_Move, CargarDatosDP, CXP_Cargar_CuentasProv, CargarCuenta, sd_ObtCodigoDisponible y
    ''' Save_Proveedor).
    ''' </summary>
    ''' <remarks>
    ''' Las sentencias van como texto con $Local porque se replican con el identificador CXP, como en VB6. A
    ''' diferencia de VB6, el UPDATE de la cuenta también usa $Local, para que en los locales que cambian de número
    ''' alcance la misma fila que creó el INSERT.
    ''' </remarks>
    Public NotInheritable Class ProveedoresRepositorio

        Public Const Replica As String = "CXP"
        Private Const NombreSql As String = "TRIM(UCASE(CONCAT(D.Apellido1, ' ', D.Apellido2, ' ', D.Nombre)))"
        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        Private Shared Function Numero(valor As Double) As String
            Return valor.ToString("0.####", Invariante)
        End Function

        Private Shared Async Function TablaAsync(cn As MySqlConnection, sql As String, parametros As Object, ct As CancellationToken) As Task(Of DataTable)
            Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                Dim tabla As New DataTable()
                tabla.Load(lector)
                Return tabla
            End Using
        End Function

        ''' <summary>Cargar_Informacion: locales, bancos, monedas y tipos de pago activos.</summary>
        Public Async Function CatalogosAsync(Optional ct As CancellationToken = Nothing) As Task(Of CatalogosProveedor)
            Dim c As New CatalogosProveedor()
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                c.Locales = Await OpcionesAsync(cn, "SELECT CAST(Local AS CHAR) AS Codigo, UCASE(Descripcion) AS Descripcion FROM gatlocales ORDER BY Local", ct).ConfigureAwait(False)
                c.Bancos = Await OpcionesAsync(cn, "SELECT CAST(Banco AS CHAR) AS Codigo, UCASE(descripcion) AS Descripcion FROM gatbancos ORDER BY Banco", ct).ConfigureAwait(False)
                c.Monedas = Await OpcionesAsync(cn, "SELECT CAST(moneda AS CHAR) AS Codigo, UCASE(CONCAT(Simbolo, ' - ', descripcion)) AS Descripcion FROM gatmonedas ORDER BY moneda", ct).ConfigureAwait(False)
                c.TiposPago = Await OpcionesAsync(cn, "SELECT CAST(TIPOPAGO AS CHAR) AS Codigo, Descripcion FROM cxptipospago WHERE ACTIVO = '1' ORDER BY TIPOPAGO", ct).ConfigureAwait(False)
            End Using
            Return c
        End Function

        Private Shared Async Function OpcionesAsync(cn As MySqlConnection, sql As String, ct As CancellationToken) As Task(Of List(Of OpcionProveedor))
            Return (Await cn.QueryAsync(Of OpcionProveedor)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
        End Function

        ''' <summary>Cargar_Cuentas: las cuentas bancarias de cheques del local, el banco y la moneda.</summary>
        Public Async Function CuentasBancariasAsync(local As Long, banco As Integer, moneda As Integer, Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionProveedor))
            Const sql As String = "SELECT cuenta AS Codigo, Descripcion FROM ccbcuentas WHERE local = @local AND banco = @banco AND moneda = @moneda AND cheques = '1' ORDER BY cuenta"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of OpcionProveedor)(New CommandDefinition(sql, New With {local, banco, moneda}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>
        ''' SD_Move: el primero, el anterior, el siguiente o el último proveedor del local por identificación. Se
        ''' salta la fila sin identificación que hay en varios locales: en VB6 dejaba a "Primero" sin resultado.
        ''' </summary>
        Public Async Function VecinoAsync(local As Long, actual As String, direccion As DireccionNavegacion, Optional ct As CancellationToken = Nothing) As Task(Of String)
            actual = If(actual, "").Trim()
            Dim sql As String
            Select Case direccion
                Case DireccionNavegacion.Anterior : sql = "SELECT id FROM cxpproveedores WHERE id < @actual AND id <> '' AND local = @local ORDER BY id DESC LIMIT 1"
                Case DireccionNavegacion.Siguiente : sql = "SELECT id FROM cxpproveedores WHERE id > @actual AND local = @local ORDER BY id LIMIT 1"
                Case DireccionNavegacion.Ultimo : sql = "SELECT MAX(id) FROM cxpproveedores WHERE local = @local AND id <> ''"
                Case Else : sql = "SELECT MIN(id) FROM cxpproveedores WHERE local = @local AND id <> ''"
            End Select
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {local, actual}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ''' <summary>
        ''' CargarDatosDP: la persona activa y viva, con los nombres de su provincia, cantón, distrito y sector.
        ''' Nothing si no está registrada.
        ''' </summary>
        Public Async Function PersonaAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of PersonaProveedor)
            Dim sql = $"SELECT D.ID AS Id, {NombreSql} AS Nombre, IFNULL(P.Descripcion, '') AS Provincia, IFNULL(C.Descripcion, '') AS Canton, " &
                      "IFNULL(T.Descripcion, '') AS Distrito, IFNULL(S.Descripcion, '') AS Sector FROM gacdp D " &
                      "LEFT JOIN gatprovincias P ON P.Codigo = D.prov " &
                      "LEFT JOIN gatcantones C ON C.Provincia = D.prov AND C.Codigo = D.cant " &
                      "LEFT JOIN gatdistritos T ON T.Provincia = D.prov AND T.Canton = D.cant AND T.Codigo = D.dist " &
                      "LEFT JOIN gatsectores S ON S.Provincia = D.prov AND S.Canton = D.cant AND S.Distrito = D.dist AND S.Codigo = D.caserio " &
                      "WHERE D.ID = @id AND D.activo = '1' AND D.fdef = '1900-01-01' LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of PersonaProveedor)(New CommandDefinition(sql, New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Teléfonos y correos de la persona (lo que muestra NumLoc en la pestaña Localización).</summary>
        Public Async Function ContactosAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of ContactoProveedor))
            Const sql As String = "SELECT IFNULL(T.descripcion, CAST(C.tipo AS CHAR)) AS Tipo, C.dato AS Dato FROM gacdptel C LEFT JOIN gacdpteltipos T ON T.tipo = C.tipo WHERE C.id = @id ORDER BY C.pos"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of ContactoProveedor)(New CommandDefinition(sql, New With {id}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        Public Async Function NombreAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            If String.IsNullOrWhiteSpace(id) Then Return ""
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition($"SELECT {NombreSql} FROM gacdp D WHERE D.ID = @id", New With {id}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        Public Async Function EsProveedorAsync(local As Long, id As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition("SELECT COUNT(*) FROM cxpproveedores WHERE local = @local AND id = @id", New With {local, id}, cancellationToken:=ct)).ConfigureAwait(False) > 0
            End Using
        End Function

        Private NotInheritable Class FilaCuenta
            Public Property CTAPROV As String
            Public Property DESCRIPCION As String
            Public Property TIPOPAGO As Integer
            Public Property cuenta As String
            Public Property banco As Integer
            Public Property CONFIRMACION As String
            Public Property DIAPAGO As Integer
            Public Property DIAVISITA As Integer
            Public Property CREDITO As Double
            Public Property ENCARGADOCOMPRAS As String
            Public Property AGENTE As String
            Public Property VENDEDOR As String
            Public Property MONEDA As Integer
            Public Property DIASPLAZO As Integer
            Public Property FULTPAGO As Date?
            Public Property Observaciones As String
            Public Property DiasEntrega As Integer
            Public Property CC As String
            Public Property Categ As String
            Public Property Devol As String
            Public Property MontoPromocional As Double
        End Class

        ''' <summary>CXP_Cargar_CuentasProv: las cuentas del proveedor en el local.</summary>
        Public Async Function CuentasAsync(local As Long, id As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of CuentaProveedor))
            Const sql As String =
                "SELECT CTAPROV, DESCRIPCION, TIPOPAGO, cuenta, banco, CONFIRMACION, DIAPAGO, DIAVISITA, CREDITO, ENCARGADOCOMPRAS, AGENTE, VENDEDOR, " &
                "MONEDA, DIASPLAZO, FULTPAGO, Observaciones, DiasEntrega, CC, Categ, Devol, MontoPromocional FROM cxpcuenprov WHERE id = @id AND local = @local ORDER BY CTAPROV"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of FilaCuenta)(New CommandDefinition(sql, New With {id, local}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.Select(Function(f) New CuentaProveedor With {
                    .Codigo = If(f.CTAPROV, ""), .CodigoOriginal = If(f.CTAPROV, ""), .Descripcion = If(f.DESCRIPCION, ""), .TipoPago = f.TIPOPAGO,
                    .Cuenta = If(f.cuenta, ""), .Banco = f.banco, .Confirmacion = If(f.CONFIRMACION, ""), .DiaPago = f.DIAPAGO, .DiaVisita = f.DIAVISITA,
                    .Credito = f.CREDITO, .EncargadoCompras = If(f.ENCARGADOCOMPRAS, ""), .Agente = If(f.AGENTE, ""), .Vendedor = If(f.VENDEDOR, ""),
                    .Moneda = f.MONEDA, .DiasPlazo = f.DIASPLAZO,
                    .FUltPago = If(f.FULTPAGO.HasValue AndAlso f.FULTPAGO.Value.Year > 1900, f.FULTPAGO, Nothing),
                    .Observaciones = If(f.Observaciones, ""), .DiasEntrega = f.DiasEntrega, .CC = If(f.CC, ""), .Categ = If(f.Categ, "").Trim(),
                    .Devoluciones = f.Devol = "1", .MontoPromocional = f.MontoPromocional}).ToList()
            End Using
        End Function

        ''' <summary>Buscar de la barra: los proveedores activos del local. Columnas Nombre e Identificación.</summary>
        Public Async Function BuscarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Dim sql = $"SELECT {NombreSql} AS Nombre, CAST(D.ID AS CHAR) AS `Identificación` FROM cxpproveedores P INNER JOIN gacdp D ON P.ID = D.ID WHERE P.LOCAL = @local AND P.ESTADO = '1'"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await TablaAsync(cn, sql, New With {local}, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>CmdBuscarDP_Click: las personas activas. Columnas Nombre e Identificación.</summary>
        Public Async Function PersonasAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Dim sql = $"SELECT {NombreSql} AS Nombre, CAST(D.ID AS CHAR) AS `Identificación` FROM gacdp D WHERE D.ACTIVO = '1'"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await TablaAsync(cn, sql, Nothing, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>CmdBuscarContacto_Click: las personas físicas. Columnas Identificación y Nombre.</summary>
        Public Async Function ContactosBuscarAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Dim sql = $"SELECT CAST(D.ID AS CHAR) AS `Identificación`, {NombreSql} AS Nombre FROM gacdp D WHERE D.tipo = 0"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await TablaAsync(cn, sql, Nothing, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' sd_ObtCodigoDisponible con pos_fun_obt_codigo: el primer número libre entre los CTAPROV numéricos, o el
        ''' mayor más uno. VB6 arma una tabla temporal en la base; aquí se calcula sin escribir.
        ''' </summary>
        Public Async Function CodigoDisponibleAsync(Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim usados = Await cn.QueryAsync(Of Long)(New CommandDefinition(
                    "SELECT CAST(ctaprov AS SIGNED) FROM cxpcuenprov WHERE IsNumeric(ctaprov)", cancellationToken:=ct)).ConfigureAwait(False)
                Return PrimerLibre(usados).ToString(Invariante)
            End Using
        End Function

        Friend Shared Function PrimerLibre(usados As IEnumerable(Of Long)) As Long
            Dim siguiente As Long = 1
            For Each n In usados.Where(Function(x) x > 0).Distinct().OrderBy(Function(x) x)
                If n <> siguiente Then Exit For
                siguiente += 1
            Next
            Return siguiente
        End Function

        ''' <summary>El proveedor que tiene ese CTAPROV, o vacío si está libre (la llave es única en todos los locales).</summary>
        Public Async Function DuenoCodigoAsync(codigo As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT id FROM cxpcuenprov WHERE ctaprov = @codigo LIMIT 1", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ' ---- Catálogo contable ----

        ''' <summary>TxtCContable_KeyPress: las cuentas auxiliares del catálogo. Columnas Descripción, Cuenta (como se ve) y Codigo.</summary>
        Public Async Function BuscarCuentasContablesAsync(formato As FormatoCuenta, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim catalogo = Await EmisionCheques.CatalogoAsync(cn, cedula, ct).ConfigureAwait(False)
                Dim sql = $"SELECT Descripcion AS `Descripción`, {formato.ExpresionSql("codigo")} AS Cuenta, Codigo FROM {catalogo}sgccatalogo WHERE Auxiliar = '1' ORDER BY descripcion"
                Return Await TablaAsync(cn, sql, Nothing, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>CargarCuenta: la cuenta como la ve el usuario. Nothing si no existe.</summary>
        Public Async Function CuentaContableAsync(cuenta As String, formato As FormatoCuenta, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of CuentaContableProveedor)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim catalogo = Await EmisionCheques.CatalogoAsync(cn, cedula, ct).ConfigureAwait(False)
                Dim sql = $"SELECT codigo AS Codigo, Descripcion, CAST(auxiliar AS CHAR) AS Auxiliar FROM {catalogo}sgccatalogo WHERE {formato.ExpresionSql("codigo")} = @cuenta LIMIT 1"
                Return Await cn.QueryFirstOrDefaultAsync(Of CuentaContableProveedor)(New CommandDefinition(sql, New With {cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Descripción de una cuenta por su código completo (para mostrar la de una cuenta cargada).</summary>
        Public Async Function DescripcionContableAsync(codigo As String, cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim catalogo = Await EmisionCheques.CatalogoAsync(cn, cedula, ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition($"SELECT Descripcion FROM {catalogo}sgccatalogo WHERE codigo = @codigo LIMIT 1", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ' ---- Guardar ----

        Friend Shared Function SentenciaProveedor(id As String) As String
            Return $"INSERT INTO cxpproveedores (LOCAL, ID, ESTADO, FC, FE) VALUES ($Local, {Q(id.Trim())}, '1', NOW(), NULL)"
        End Function

        Friend Shared Function SentenciaInsertar(c As CuentaProveedor, id As String, uid As Long) As String
            Return "INSERT INTO cxpcuenprov (CTAPROV, LOCAL, ID, DESCRIPCION, TIPOPAGO, cuenta, banco, CONFIRMACION, DIAPAGO, DIAVISITA, CREDITO, " &
                   "ENCARGADOCOMPRAS, AGENTE, VENDEDOR, MONEDA, DIASPLAZO, FULTPAGO, Observaciones, ACTIVO, DiasEntrega, Categ, MontoPromocional, Devol, UIDC, FC, FE, CC) VALUES (" &
                   String.Join(", ", {Q(c.Codigo), "$Local", Q(id.Trim()), Q(c.Descripcion), c.TipoPago.ToString(Invariante), Q(c.Cuenta), c.Banco.ToString(Invariante),
                                      Q(c.Confirmacion), c.DiaPago.ToString(Invariante), c.DiaVisita.ToString(Invariante), Numero(c.Credito),
                                      Q(c.EncargadoCompras), Q(c.Agente), Q(c.Vendedor), c.Moneda.ToString(Invariante), c.DiasPlazo.ToString(Invariante),
                                      "'1900-01-01'", Q(c.Observaciones), "'1'", c.DiasEntrega.ToString(Invariante), Q(c.Categ), Numero(c.MontoPromocional),
                                      If(c.Devoluciones, "'1'", "'0'"), uid.ToString(Invariante), "NOW()", "NULL", Q(c.CC)}) & ")"
        End Function

        Friend Shared Function SentenciaActualizar(c As CuentaProveedor) As String
            Return $"UPDATE cxpcuenprov SET CTAPROV={Q(c.Codigo)}, DESCRIPCION={Q(c.Descripcion)}, TIPOPAGO={c.TipoPago.ToString(Invariante)}, " &
                   $"cuenta={Q(c.Cuenta)}, banco={c.Banco.ToString(Invariante)}, CONFIRMACION={Q(c.Confirmacion)}, DIAPAGO={c.DiaPago.ToString(Invariante)}, " &
                   $"DIAVISITA={c.DiaVisita.ToString(Invariante)}, CREDITO={Numero(c.Credito)}, ENCARGADOCOMPRAS={Q(c.EncargadoCompras)}, AGENTE={Q(c.Agente)}, " &
                   $"VENDEDOR={Q(c.Vendedor)}, MONEDA={c.Moneda.ToString(Invariante)}, DIASPLAZO={c.DiasPlazo.ToString(Invariante)}, " &
                   $"Observaciones={Q(c.Observaciones)}, DiasEntrega={c.DiasEntrega.ToString(Invariante)}, CC={Q(c.CC)}, Categ={Q(c.Categ)}, " &
                   $"MontoPromocional={Numero(c.MontoPromocional)}, Devol={If(c.Devoluciones, "'1'", "'0'")} WHERE CTAPROV={Q(c.CodigoOriginal)} AND LOCAL=$Local"
        End Function

        Friend Shared Function SentenciaCambiarCxp(anterior As String, nuevo As String) As String
            Return $"UPDATE cxp SET ctaprov={Q(nuevo)} WHERE ctaprov={Q(anterior)}"
        End Function

        Friend Shared Function SentenciaBorrar(codigo As String) As String
            Return $"DELETE FROM cxpcuenprov WHERE ctaprov={Q(codigo)}"
        End Function

        ''' <summary>
        ''' Save_Proveedor: registra al proveedor en el local si no lo está y aplica las cuentas nuevas, modificadas
        ''' y por borrar. Una cuenta con CXP no se borra: se devuelve en la lista. Ejecuta con el local elegido y
        ''' replica desde el local de la sesión, como VB6.
        ''' </summary>
        Public Async Function GuardarAsync(local As Long, id As String, cuentas As IEnumerable(Of CuentaProveedor), uid As Long, localSesion As Long,
                                           Optional ct As CancellationToken = Nothing) As Task(Of List(Of String))
            Dim noBorradas As New List(Of String)
            Dim lit = local.ToString(Invariante)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim ejecutar = Async Function(sql As String) As Task
                                   Await cn.ExecuteAsync(New CommandDefinition(sql.Replace("$Local", lit), cancellationToken:=ct)).ConfigureAwait(False)
                                   Await Replicador.ReplicarAsync(cn, Replica, sql, localSesion, ct).ConfigureAwait(False)
                               End Function
                If Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition("SELECT COUNT(*) FROM cxpproveedores WHERE local = @local AND id = @id", New With {local, .id = id.Trim()}, cancellationToken:=ct)).ConfigureAwait(False) = 0 Then
                    Await ejecutar(SentenciaProveedor(id)).ConfigureAwait(False)
                End If
                For Each c In cuentas
                    If c.Borrar Then
                        If c.Nueva Then Continue For
                        Dim usos = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition("SELECT COUNT(*) FROM cxp WHERE CTAPROV = @codigo", New With {.codigo = c.CodigoOriginal}, cancellationToken:=ct)).ConfigureAwait(False)
                        If usos > 0 Then
                            noBorradas.Add(c.CodigoOriginal)
                        Else
                            Await ejecutar(SentenciaBorrar(c.CodigoOriginal)).ConfigureAwait(False)
                        End If
                    ElseIf c.Nueva Then
                        Await ejecutar(SentenciaInsertar(c, id, uid)).ConfigureAwait(False)
                    ElseIf c.Modificada Then
                        Await ejecutar(SentenciaActualizar(c)).ConfigureAwait(False)
                        If c.Codigo.Trim() <> c.CodigoOriginal.Trim() Then Await ejecutar(SentenciaCambiarCxp(c.CodigoOriginal, c.Codigo)).ConfigureAwait(False)
                    End If
                Next
            End Using
            Return noBorradas
        End Function

    End Class

End Namespace
