Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>CmbExo de frmClientes: el índice de la lista es el valor de posclientes.Exonerado.</summary>
    Public Enum ExoneracionCliente
        No = 0
        Si = 1
        ''' <summary>"No Suj.(Muni, CCSS)": no sujeto, como la municipalidad o la CCSS.</summary>
        NoSujeto = 2
    End Enum

    ''' <summary>Los botones &lt;&lt; &lt; &gt; &gt;&gt; de frmClientes (CmdMandos).</summary>
    Public Enum DireccionCliente
        Primero
        Anterior
        Siguiente
        Ultimo
    End Enum

    ''' <summary>Las condiciones de venta de un cliente en un local (posclientes).</summary>
    Public NotInheritable Class ClientePos
        ''' <summary>posclientes.Cliente: la identificación de gacdp.</summary>
        Public Property Cliente As String = ""
        ''' <summary>0 es el precio que asigna el sistema; 1 a 6, los precios del artículo.</summary>
        Public Property PrecioFac As Integer
        Public Property Exonerado As ExoneracionCliente = ExoneracionCliente.No
        ''' <summary>Identificación del agente o vendedor.</summary>
        Public Property Vendedor As String = ""
        ''' <summary>0 sin descuento; -1 el descuento máximo del artículo; si no, el porcentaje fijo.</summary>
        Public Property Descuento As Double
        ''' <summary>Referencia o código del cliente, p. ej. el número de proveedor ante la CCSS.</summary>
        Public Property NumCliente As String = ""
        ''' <summary>Ruta predeterminada (posrutas.ruta); 0 si no coincide en ninguna.</summary>
        Public Property Zona As Long
        Public Property Horario As String = ""
        Public Property Visita As String = ""
        Public Property Contacto1 As String = ""
        Public Property Cargo1 As String = ""
        Public Property Contacto2 As String = ""
        Public Property Cargo2 As String = ""
        Public Property Notas As String = ""
        ''' <summary>posclientes.IVA (int): uno de los valores de posiva_sujetos.porcentaje.</summary>
        Public Property Iva As Integer
        Public Property ActividadEconomica As String = ""
    End Class

    ''' <summary>Una ruta de posrutas donde coincide la dirección del cliente.</summary>
    Public NotInheritable Class RutaCliente
        Public Property Ruta As Long
        Public Property Nombre As String = ""
        Public Overrides Function ToString() As String
            Return $"{Ruta} - {Nombre}"
        End Function
    End Class

    Public NotInheritable Class CatalogosCliente
        ''' <summary>Las seis etiquetas de pos_etiquetaprecio0 a 5.</summary>
        Public Property EtiquetasPrecio As New List(Of String)
        ''' <summary>posiva_sujetos.porcentaje (VB6 lee la primera columna).</summary>
        Public Property SujetosIva As New List(Of Integer)
    End Class

    ''' <summary>
    ''' Clientes del POS. Migración de SD0502\frmClientes.frm (CmdMandos_Click, SD_Cargar_Cliente, SD_Guardar,
    ''' SD_Cargar_Rutas, SD_Cargar_IVA y los buscadores de CmdBuscId y SDCMD1).
    ''' </summary>
    ''' <remarks>
    ''' Las sentencias de Guardar y Borrar van como texto porque se replican (identificador posclientes). VB6
    ''' borra al cliente sin filtrar el local; aquí se borra solo en el local de la pantalla, el mismo que usan
    ''' la carga, la navegación y Guardar.
    ''' </remarks>
    Public NotInheritable Class ClientesRepositorio

        Public Const Replica As String = "posclientes"
        Public Const DescuentoMaximo As Double = -1

        Private Const LargoCodigoAsociado As Integer = 8
        Private Const NombreSql As String = "UCASE(TRIM(CONCAT(TRIM(D.apellido1), ' ', TRIM(D.apellido2), ' ', TRIM(D.nombre))))"
        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        Private Shared Async Function TablaAsync(cn As MySqlConnection, sql As String, parametros As Object, ct As CancellationToken) As Task(Of DataTable)
            Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                Dim tabla As New DataTable()
                tabla.Load(lector)
                Return tabla
            End Using
        End Function

        ''' <summary>SD_Limpiar (etiquetas de precio) y SD_Cargar_IVA.</summary>
        Public Async Function CatalogosAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of CatalogosCliente)
            Dim c As New CatalogosCliente()
            Dim parametros As New Parametros(_fabrica)
            For i = 0 To 5
                c.EtiquetasPrecio.Add(Await parametros.ObtenerAsync(local, $"pos_etiquetaprecio{i}", ct).ConfigureAwait(False))
            Next
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using tabla = Await TablaAsync(cn, "SELECT * FROM posiva_sujetos", Nothing, ct).ConfigureAwait(False)
                    For Each fila As DataRow In tabla.Rows
                        c.SujetosIva.Add(CInt(Vb6.Val(Convert.ToString(fila(0), Invariante))))
                    Next
                End Using
            End Using
            Return c
        End Function

        ''' <summary>
        ''' CmdMandos_Click: el primero, el anterior, el siguiente o el último cliente del local, por orden de
        ''' identificación. Sin cliente actual, Anterior va al primero y Siguiente al último, como en VB6. Vacío si
        ''' no hay.
        ''' </summary>
        Public Async Function VecinoAsync(local As Long, actual As String, direccion As DireccionCliente, Optional ct As CancellationToken = Nothing) As Task(Of String)
            actual = If(actual, "").Trim()
            Dim sql As String
            Select Case direccion
                Case DireccionCliente.Anterior
                    sql = If(actual.Length = 0, "SELECT MIN(Cliente) FROM posclientes WHERE local = @local",
                             "SELECT MAX(Cliente) FROM posclientes WHERE local = @local AND Cliente < @actual")
                Case DireccionCliente.Siguiente
                    sql = If(actual.Length = 0, "SELECT MAX(Cliente) FROM posclientes WHERE local = @local",
                             "SELECT MIN(Cliente) FROM posclientes WHERE local = @local AND Cliente > @actual")
                Case DireccionCliente.Ultimo
                    sql = "SELECT MAX(Cliente) FROM posclientes WHERE local = @local"
                Case Else
                    sql = "SELECT MIN(Cliente) FROM posclientes WHERE local = @local"
            End Select
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {local, actual}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        Private NotInheritable Class FilaCliente
            Public Property Cliente As String
            Public Property PrecioFac As String
            Public Property Exonerado As String
            Public Property Vendedor As String
            Public Property Descuento As String
            Public Property NumCliente As String
            Public Property Zona As String
            Public Property Horario As String
            Public Property Visita As String
            Public Property Contacto1 As String
            Public Property Cargo1 As String
            Public Property Contacto2 As String
            Public Property Cargo2 As String
            Public Property Notas As String
            Public Property Iva As String
            Public Property ActividadEconomica As String
        End Class

        ''' <summary>SD_Cargar_Cliente. Nothing si el cliente no está registrado en el local.</summary>
        Public Async Function CargarAsync(local As Long, cliente As String, Optional ct As CancellationToken = Nothing) As Task(Of ClientePos)
            ' Todo como texto, igual que lo lee VB6 (iRS(n).Value & ""), sin depender del tipo de cada columna.
            Const sql As String =
                "SELECT CAST(Cliente AS CHAR) AS Cliente, CAST(PrecioFac AS CHAR) AS PrecioFac, CAST(Exonerado AS CHAR) AS Exonerado, " &
                "CAST(Vendedor AS CHAR) AS Vendedor, CAST(Descuento AS CHAR) AS Descuento, CAST(NumCliente AS CHAR) AS NumCliente, " &
                "CAST(Zona AS CHAR) AS Zona, CAST(Horario AS CHAR) AS Horario, CAST(Visita AS CHAR) AS Visita, " &
                "CAST(Contacto1 AS CHAR) AS Contacto1, CAST(Cargo1 AS CHAR) AS Cargo1, CAST(Contacto2 AS CHAR) AS Contacto2, " &
                "CAST(Cargo2 AS CHAR) AS Cargo2, CAST(Notas AS CHAR) AS Notas, CAST(IVA AS CHAR) AS Iva, " &
                "CAST(ActividadEconomica AS CHAR) AS ActividadEconomica " &
                "FROM posclientes WHERE local = @local AND Cliente = @cliente LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim f = Await cn.QueryFirstOrDefaultAsync(Of FilaCliente)(New CommandDefinition(sql, New With {local, cliente}, cancellationToken:=ct)).ConfigureAwait(False)
                Return If(f Is Nothing, Nothing, Convertir(f))
            End Using
        End Function

        Private Shared Function Convertir(f As FilaCliente) As ClientePos
            Dim exonerado = CInt(Vb6.Val(If(f.Exonerado, "")))
            Return New ClientePos With {
                .Cliente = If(f.Cliente, ""), .PrecioFac = CInt(Vb6.Val(If(f.PrecioFac, ""))),
                .Exonerado = If([Enum].IsDefined(GetType(ExoneracionCliente), exonerado), CType(exonerado, ExoneracionCliente), ExoneracionCliente.No),
                .Vendedor = If(f.Vendedor, ""), .Descuento = Conversion.Val(If(f.Descuento, "")), .NumCliente = If(f.NumCliente, ""),
                .Zona = Vb6.Val(If(f.Zona, "")), .Horario = If(f.Horario, ""), .Visita = If(f.Visita, ""),
                .Contacto1 = If(f.Contacto1, ""), .Cargo1 = If(f.Cargo1, ""), .Contacto2 = If(f.Contacto2, ""), .Cargo2 = If(f.Cargo2, ""),
                .Notas = If(f.Notas, ""), .Iva = CInt(Vb6.Val(If(f.Iva, ""))), .ActividadEconomica = If(f.ActividadEconomica, "")
            }
        End Function

        ''' <summary>
        ''' SD_Cargar_Rutas: las rutas del local cuyo sector (provincia, cantón, distrito y caserío) es el de la
        ''' dirección de la persona, esté o no registrada como cliente.
        ''' </summary>
        Public Async Function RutasAsync(local As Long, id As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of RutaCliente))
            Const sql As String =
                "SELECT DISTINCT A.ruta AS Ruta, IFNULL(CAST(R.descripcion AS CHAR), '') AS Nombre FROM posrutaspcds A " &
                "LEFT JOIN posrutas R ON R.local = A.local AND R.ruta = A.ruta " &
                "WHERE A.local = @local AND (A.pro, A.can, A.dis, A.sec) IN (SELECT prov, cant, dist, caserio FROM gacdp WHERE id = @id)"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of RutaCliente)(New CommandDefinition(sql, New With {local, id}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>Nombre de la persona en mayúsculas, o vacío si no existe.</summary>
        Public Async Function NombreAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(
                    $"SELECT {NombreSql} FROM gacdp D WHERE D.id = @id", New With {id}, cancellationToken:=ct)).ConfigureAwait(False), "").Trim()
            End Using
        End Function

        ''' <summary>
        ''' SD_Obtener_ID tal como lo llama frmClientes (siempre sin LIKE): un código de asociado exacto (clave de
        ''' hasta ocho caracteres) gana; si no, las identificaciones, los códigos de asociados activos y los
        ''' códigos de clientes iguales a la clave. Columnas: ID, Código, Tipo y Nombre. Con una sola fila no hace
        ''' falta el buscador.
        ''' </summary>
        Public Async Function CoincidenciasAsync(clave As String, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            clave = If(clave, "").Trim()
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                If clave.Length = 0 Then Return Await TablaAsync(cn, SqlCoincidencias & " LIMIT 0", New With {clave}, ct).ConfigureAwait(False)
                If clave.Length <= LargoCodigoAsociado Then
                    Dim id = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT gac_fun_asociados(0, @clave)", New With {clave}, cancellationToken:=ct)).ConfigureAwait(False)
                    If Not String.IsNullOrEmpty(id) Then
                        Dim tabla = Await TablaAsync(cn, SqlCoincidencias & " LIMIT 0", New With {clave}, ct).ConfigureAwait(False)
                        tabla.Rows.Add(id, clave, "ASOC", "")
                        Return tabla
                    End If
                End If
                Return Await TablaAsync(cn, SqlCoincidencias, New With {clave}, ct).ConfigureAwait(False)
            End Using
        End Function

        Private Const SqlCoincidencias As String =
            "SELECT * FROM (" &
            "SELECT CAST(A.ID AS CHAR) AS ID, CAST(A.CODIGO AS CHAR) AS `Código`, 'INTE' AS Tipo, " & NombreSql & " AS Nombre " &
            "FROM gaccodclientes A INNER JOIN gacdp D ON D.ID = A.ID WHERE A.CODIGO = @clave " &
            "UNION ALL SELECT CAST(D.ID AS CHAR), CAST(D.ID AS CHAR), 'IDEN', " & NombreSql & " FROM gacdp D WHERE D.ID = @clave " &
            "UNION ALL SELECT CAST(A.ID AS CHAR), CAST(A.CODIGO AS CHAR), 'ASOC', " & NombreSql & " " &
            "FROM gacasociados A INNER JOIN gacdp D ON D.ID = A.ID WHERE A.ACTIVO = '1' AND A.CODIGO = @clave) X"

        ''' <summary>Buscar de la barra: los clientes del local. Columnas Cliente, Identificación y Vendedor.</summary>
        Public Async Function BuscarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String =
                "SELECT UCASE(TRIM(CONCAT(TRIM(D.apellido1), ' ', TRIM(D.apellido2), ' ', TRIM(D.nombre)))) AS Cliente, CAST(C.Cliente AS CHAR) AS `Identificación`, " &
                "UCASE(TRIM(CONCAT(C.Vendedor, ' - ', TRIM(IFNULL(E.apellido1, '')), ' ', TRIM(IFNULL(E.apellido2, '')), ' ', TRIM(IFNULL(E.nombre, ''))))) AS Vendedor " &
                "FROM posclientes C LEFT JOIN gacdp D ON C.Cliente = D.id LEFT JOIN gacdp E ON C.Vendedor = E.id WHERE C.local = @local"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await TablaAsync(cn, sql, New With {local}, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' CmdBuscId 0, 2 y 3: todas las personas o solo las físicas (tipo 0), para los contactos. Columnas Nombre,
        ''' CC (alias) e Identificación.
        ''' </summary>
        Public Async Function PersonasAsync(soloFisicas As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Dim sql = $"SELECT {NombreSql} AS Nombre, CAST(D.alias AS CHAR) AS CC, CAST(D.id AS CHAR) AS `Identificación` FROM gacdp D" &
                      If(soloFisicas, " WHERE D.tipo = 0", "")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await TablaAsync(cn, sql, Nothing, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>CmdBuscId 1: los usuarios del sistema, para el agente o vendedor. Mismas columnas.</summary>
        Public Async Function VendedoresAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Dim sql = $"SELECT {NombreSql} AS Nombre, CAST(D.alias AS CHAR) AS CC, CAST(A.id AS CHAR) AS `Identificación` FROM gatusuarios A LEFT JOIN gacdp D ON A.id = D.id"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await TablaAsync(cn, sql, Nothing, ct).ConfigureAwait(False)
            End Using
        End Function

        Private Shared Function Numero(valor As Double) As String
            Return valor.ToString("0.####", Invariante)
        End Function

        ''' <summary>El INSERT de SD_Guardar, para un cliente que aún no está en el local.</summary>
        Friend Shared Function SentenciaInsertar(c As ClientePos, local As Long) As String
            Return "INSERT INTO posclientes(Local, Cliente, PrecioFac, Exonerado, Vendedor, Descuento, NumCliente, Zona, Horario, Visita, " &
                   "Contacto1, Cargo1, Contacto2, Cargo2, Notas, IVA, ActividadEconomica) VALUES(" &
                   String.Join(", ", {local.ToString(Invariante), Q(c.Cliente), c.PrecioFac.ToString(Invariante), Q(CInt(c.Exonerado).ToString(Invariante)),
                                      Q(c.Vendedor), Numero(c.Descuento), Q(c.NumCliente), c.Zona.ToString(Invariante), Q(c.Horario), Q(c.Visita),
                                      Q(c.Contacto1), Q(c.Cargo1), Q(c.Contacto2), Q(c.Cargo2), Q(c.Notas), c.Iva.ToString(Invariante), Q(c.ActividadEconomica.Trim())}) & ")"
        End Function

        ''' <summary>El UPDATE de SD_Guardar.</summary>
        Friend Shared Function SentenciaActualizar(c As ClientePos, local As Long) As String
            Return $"UPDATE posclientes SET PrecioFac={c.PrecioFac.ToString(Invariante)}, Exonerado={Q(CInt(c.Exonerado).ToString(Invariante))}, " &
                   $"Vendedor={Q(c.Vendedor)}, Descuento={Numero(c.Descuento)}, NumCliente={Q(c.NumCliente)}, Zona={c.Zona.ToString(Invariante)}, " &
                   $"Horario={Q(c.Horario)}, Visita={Q(c.Visita)}, Contacto1={Q(c.Contacto1)}, Cargo1={Q(c.Cargo1)}, Contacto2={Q(c.Contacto2)}, " &
                   $"Cargo2={Q(c.Cargo2)}, Notas={Q(c.Notas)}, IVA={c.Iva.ToString(Invariante)}, ActividadEconomica={Q(c.ActividadEconomica.Trim())} " &
                   $"WHERE local={local.ToString(Invariante)} AND Cliente={Q(c.Cliente)}"
        End Function

        ''' <summary>El DELETE de SDBoton_Borrar, limitado al local (ver la nota de la clase).</summary>
        Friend Shared Function SentenciaBorrar(cliente As String, local As Long) As String
            Return $"DELETE FROM posclientes WHERE local={local.ToString(Invariante)} AND Cliente={Q(cliente)}"
        End Function

        ''' <summary>SD_Guardar: inserta o actualiza según el cliente ya esté en el local, y replica.</summary>
        Public Async Function GuardarAsync(c As ClientePos, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim existe = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(
                    "SELECT COUNT(*) FROM posclientes WHERE local = @local AND Cliente = @cliente", New With {local, .cliente = c.Cliente}, cancellationToken:=ct)).ConfigureAwait(False) > 0
                Dim sql = If(existe, SentenciaActualizar(c, local), SentenciaInsertar(c, local))
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, Replica, sql, local, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Borra las condiciones del cliente en el local (no su ficha de datos personales) y replica.</summary>
        Public Async Function BorrarAsync(cliente As String, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim sql = SentenciaBorrar(cliente, local)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, Replica, sql, local, ct).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
