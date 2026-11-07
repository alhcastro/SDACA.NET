Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una fila de poscodprove: el código con que un proveedor vende el artículo.</summary>
    Public NotInheritable Class ProveedorArticulo
        Public Property Proveedor As String = ""
        Public Property Nombre As String = ""
        Public Property Cuenta As String = ""
        Public Property CodArtProv As String = ""
        Public Property CantEmpaq As Double = 1
    End Class

    ''' <summary>
    ''' Proveedores del artículo (botón Agregar de SD0502\frmMantArticulos.frm): CargarProv, GuardaProv,
    ''' CargaCuentas y la búsqueda de sdtID_KeyPress. Guardar borra las filas del artículo en el local y las
    ''' vuelve a insertar, y replica cada sentencia como posarticuloslocal. La cuenta lleva $Prefijolocal
    ''' (R, E o Z en los locales 2, 3 y 9) salvo que ya tenga uno de esos prefijos.
    ''' </summary>
    Public NotInheritable Class ProveedoresArticuloRepositorio

        Public Const LargoCodigo As Integer = 16
        Private Const LargoCodigoAsociado As Integer = 8
        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture

        Private ReadOnly _fabrica As IFabricaConexiones
        Private ReadOnly _pausaReplica As TimeSpan

        ''' <param name="pausaReplica">
        ''' VB6 espera un segundo entre replicar el borrado y las altas: gatreplica no tiene id y se procesa por
        ''' Fecha, así que con la misma fecha las altas podrían llegar antes que el borrado.
        ''' </param>
        Public Sub New(fabrica As IFabricaConexiones, Optional pausaReplica As TimeSpan? = Nothing)
            _fabrica = fabrica
            _pausaReplica = If(pausaReplica, TimeSpan.FromSeconds(1))
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        Public Async Function ListarAsync(articulo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of ProveedorArticulo))
            Const sql As String = "SELECT p.PROVEEDOR AS Proveedor, IFNULL(b.nombre, '') AS Nombre, p.Cuenta, p.CODARTPROV AS CodArtProv, p.cantempaq AS CantEmpaq " &
                "FROM poscodprove p LEFT JOIN gacdp b ON p.PROVEEDOR = b.id WHERE p.CODIGOART = @articulo AND p.Local = @local ORDER BY p.PROVEEDOR, p.CODARTPROV"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of ProveedorArticulo)(New CommandDefinition(sql, New With {articulo, local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>
        ''' SD_Obtener_ID sin búsqueda parcial: la identificación exacta, el código de asociado (clave corta) o un
        ''' código de cliente único. Vacío si no se resuelve; entonces la pantalla abre la lista de proveedores.
        ''' </summary>
        Public Async Function ResolverIdAsync(clave As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            clave = If(clave, "").Trim()
            If clave.Length = 0 Then Return ""
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim id = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT id FROM gacdp WHERE id = @clave", New With {clave}, cancellationToken:=ct)).ConfigureAwait(False)
                If Not String.IsNullOrEmpty(id) Then Return id
                If clave.Length <= LargoCodigoAsociado Then
                    id = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT gac_fun_asociados(0, @clave)", New With {clave}, cancellationToken:=ct)).ConfigureAwait(False)
                    If Not String.IsNullOrEmpty(id) Then Return id
                End If
                Dim ids = (Await cn.QueryAsync(Of String)(New CommandDefinition(
                    "SELECT ID FROM gacasociados WHERE Activo = '1' AND CODIGO = @clave UNION SELECT ID FROM gaccodclientes WHERE CODIGO = @clave",
                    New With {clave}, cancellationToken:=ct)).ConfigureAwait(False)).Distinct().ToList()
                Return If(ids.Count = 1, ids(0), "")
            End Using
        End Function

        ''' <summary>Nombre de la persona como lo muestra VB6, o vacío si no existe.</summary>
        Public Async Function NombreAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(
                    "SELECT TRIM(UCASE(CONCAT(APELLIDO1, ' ', APELLIDO2, ' ', NOMBRE))) FROM gacdp WHERE id = @id", New With {id}, cancellationToken:=ct)).ConfigureAwait(False), "").Trim()
            End Using
        End Function

        ''' <summary>POSBProveedor: proveedores activos con cuenta activa en el local. Columnas Nombre e ID primero.</summary>
        Public Async Function ProveedoresAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT TRIM(UCASE(CONCAT(D.APELLIDO1, ' ', D.APELLIDO2, ' ', D.NOMBRE))) AS Nombre, D.ID, MAX(P.DIAVISITA) AS Dias, MAX(V.LineasOC) AS Lineas " &
                "FROM gacdp D INNER JOIN cxpcuenprov P ON P.ID = D.ID INNER JOIN cxpproveedores V ON V.ID = D.ID " &
                "WHERE P.LOCAL = @local AND D.ACTIVO = '1' AND D.FDEF = '1900-01-01' AND P.ACTIVO = '1' " &
                "GROUP BY D.ID ORDER BY D.APELLIDO1, D.NOMBRE"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>CargaCuentas: "cuenta - descripción" de las cuentas activas del proveedor en el local.</summary>
        Public Async Function CuentasAsync(id As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of String))
            Const sql As String = "SELECT DISTINCT CAST(CONCAT(P.CTAPROV, ' - ', P.DESCRIPCION) AS CHAR) " &
                "FROM gacdp D INNER JOIN cxpcuenprov P ON P.ID = D.ID INNER JOIN cxpproveedores V ON V.ID = D.ID " &
                "WHERE D.ID = @id AND P.LOCAL = @local AND D.ACTIVO = '1' AND D.FDEF = '1900-01-01' AND P.ACTIVO = '1' ORDER BY P.CTAPROV"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of String)(New CommandDefinition(sql, New With {id, local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>Str_GetLateral(texto, "-"): la cuenta de "cuenta - descripción".</summary>
        Public Shared Function CuentaDe(opcion As String) As String
            Dim t = If(opcion, "")
            Dim i = t.IndexOf("-"c)
            Return If(i < 0, t, t.Substring(0, i)).Trim()
        End Function

        Friend Shared Function SentenciaBorrar(articulo As String) As String
            Return $"DELETE FROM poscodprove WHERE codigoart = {Q(articulo)} AND local = $Local"
        End Function

        ''' <summary>
        ''' La cuenta del local 1 lleva el prefijo del local de destino. Una cuenta vacía queda vacía: VB6 le ponía
        ''' el prefijo y en los locales 2, 3 y 9 quedaba "R", "E" o "Z".
        ''' </summary>
        Friend Shared Function SentenciaAlta(articulo As String, p As ProveedorArticulo) As String
            Dim cuenta = If(p.Cuenta, "").Trim()
            Dim prefijo = If(cuenta.Length = 0 OrElse "REZ".Contains(cuenta(0)), "", "$Prefijolocal")
            Return "INSERT INTO poscodprove (proveedor, codartprov, codigoart, local, cantempaq, Cuenta) VALUES (" &
                $"{Q(p.Proveedor)}, {Q(p.CodArtProv)}, {Q(articulo)}, $Local, {p.CantEmpaq.ToString("R", Invariante)}, '{prefijo}{LibroBancos.Literal(cuenta)}')"
        End Function

        Private Shared Async Function EjecutarYReplicarAsync(cn As MySqlConnection, plantilla As String, local As Long, ct As CancellationToken) As Task
            Dim sql = plantilla.Replace("$Prefijolocal", "").Replace("$Local", local.ToString(Invariante))
            Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
            Await Replicador.ReplicarAsync(cn, ArticulosRepositorio.ReplicaLocal, plantilla, local, ct).ConfigureAwait(False)
        End Function

        ''' <summary>GuardaProv. Sin transacción: poscodprove es MyISAM.</summary>
        Public Async Function GuardarAsync(articulo As String, local As Long, proveedores As IEnumerable(Of ProveedorArticulo), Optional ct As CancellationToken = Nothing) As Task
            Dim filas = proveedores.Where(Function(p) Not String.IsNullOrWhiteSpace(p.Proveedor)).ToList()
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarYReplicarAsync(cn, SentenciaBorrar(articulo), local, ct).ConfigureAwait(False)
                If filas.Count = 0 Then Return
                Await Task.Delay(_pausaReplica, ct).ConfigureAwait(False)
                For Each p In filas
                    Await EjecutarYReplicarAsync(cn, SentenciaAlta(articulo, p), local, ct).ConfigureAwait(False)
                Next
            End Using
        End Function

    End Class

End Namespace
