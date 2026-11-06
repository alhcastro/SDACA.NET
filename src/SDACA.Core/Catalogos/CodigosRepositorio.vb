Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports SDACA.Core.Bancos
Imports SDACA.Core.Datos

Namespace Catalogos

    ''' <summary>Fila de <c>gactiposcodigos</c>. Sistema y Activo valen "1" o "0".</summary>
    Public NotInheritable Class TipoCodigo
        Public Const Si As String = "1"
        Public Const No As String = "0"

        Public Property Tipo As String = ""
        Public Property Descripcion As String = ""
        Public Property Sistema As String = No
        Public Property Activo As String = Si
    End Class

    ''' <summary>Botones de navegación del mantenimiento de tipos.</summary>
    Public Enum IrATipo
        Primero
        Anterior
        Siguiente
        Ultimo
    End Enum

    ''' <summary>
    ''' Administración de Códigos. Migración de SD0001\FrmAdminCodigos.frm y FrmMantCodigos.frm. Aquí los
    ''' códigos se replican como DP, no como posclientes igual que en Datos Personales: así lo hace VB6, y
    ''' cada identificador tiene destinos distintos. Los tipos se replican como GATMANT.
    ''' </summary>
    Public NotInheritable Class CodigosRepositorio

        Private Const ReplicaCodigos As String = "DP"
        Private Const ReplicaTipos As String = "GATMANT"
        Private Const LargoCodigoAsociado As Integer = 8
        Private Const AnchoEtiqueta As Integer = 15
        Private Const NombreSql As String = "IFNULL(UCASE(TRIM(CONCAT(D.apellido1, ' ', D.apellido2, ' ', D.nombre))), '')"
        Private Const CamposTipo As String = "TIPO AS Tipo, DESCRIPCION AS Descripcion, IF(SISTEMA = '1', '1', '0') AS Sistema, IF(ACTIVO = '1', '1', '0') AS Activo"

        Friend Const SqlAsociadoExacto As String =
            "SELECT A.ID AS 'Identificación', A.CODIGO AS 'Código', 'ASOC' AS 'Tipo', " & NombreSql & " AS 'Nombre' " &
            "FROM gacasociados A LEFT JOIN gacdp D ON D.id = A.ID WHERE A.ACTIVO = '1' AND A.CODIGO = @clave"

        Friend Const SqlCoincidencias As String =
            "SELECT C.ID AS 'Identificación', C.CODIGO AS 'Código', 'INTE' AS 'Tipo', " & NombreSql & " AS 'Nombre' " &
            "FROM gaccodclientes C LEFT JOIN gacdp D ON D.id = C.ID WHERE C.CODIGO LIKE @patron " &
            "UNION ALL SELECT D.id, D.id, 'IDEN', " & NombreSql & " FROM gacdp D WHERE D.id LIKE @patron " &
            "UNION ALL SELECT A.ID, A.CODIGO, 'ASOC', " & NombreSql & " " &
            "FROM gacasociados A LEFT JOIN gacdp D ON D.id = A.ID WHERE A.ACTIVO = '1' AND A.CODIGO LIKE @patron"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(texto)}'"
        End Function

        Private Shared Async Function EjecutarReplicadoAsync(cn As MySqlConnection, identificador As String, sql As String, local As Long, ct As CancellationToken) As Task
            Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
            Await Replicador.ReplicarAsync(cn, identificador, sql, local, ct).ConfigureAwait(False)
        End Function

        Private Shared Async Function TablaAsync(cn As MySqlConnection, sql As String, parametros As Object, ct As CancellationToken) As Task(Of DataTable)
            Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                Dim tabla As New DataTable()
                tabla.Load(lector)
                Return tabla
            End Using
        End Function

        ' ---- Personas y sus códigos ----

        ''' <summary>
        ''' Equivale a SD_Obtener_ID: la primera columna es la identificación. Una clave corta que es
        ''' exactamente un código de asociado gana; si no, se busca en códigos, identificaciones y asociados.
        ''' </summary>
        Public Async Function CoincidenciasAsync(clave As String, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            clave = If(clave, "").Trim()
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                If clave.Length <= LargoCodigoAsociado Then
                    Dim exacto = Await TablaAsync(cn, SqlAsociadoExacto, New With {clave}, ct).ConfigureAwait(False)
                    If exacto.Rows.Count > 0 Then Return exacto
                    exacto.Dispose()
                End If
                Return Await TablaAsync(cn, SqlCoincidencias, New With {.patron = $"%{clave}%"}, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Código de asociado para el detalle, aunque esté inactivo. Vacío si no tiene.</summary>
        Public Async Function CodigoAsociadoAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT CODIGO FROM gacasociados WHERE ID = @id LIMIT 1", New With {id}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        Public Async Function AgregarCodigoAsync(id As String, codigo As String, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarReplicadoAsync(cn, ReplicaCodigos, DatosPersonalesRepositorio.SentenciaAgregarCodigo(id, codigo, uid, local), local, ct).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function EliminarCodigoAsync(id As String, codigo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarReplicadoAsync(cn, ReplicaCodigos, DatosPersonalesRepositorio.SentenciaEliminarCodigo(id, codigo), local, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Equivale a SD_ADD_TXTDET: etiqueta en mayúscula y alineada a la derecha, para fuente fija.</summary>
        Public Shared Function LineaDetalle(etiqueta As String, texto As String) As String
            Dim titulo = If(etiqueta, "").ToUpperInvariant()
            If titulo.Length > AnchoEtiqueta Then titulo = titulo.Substring(0, AnchoEtiqueta)
            Return $"{titulo.PadLeft(AnchoEtiqueta)}: {texto}"
        End Function

        Public Shared Function FechaDetalle(fecha As Date) As String
            Return fecha.ToString("dd 'de' MMMM 'de' yyyy", CultureInfo.GetCultureInfo("es-CR"))
        End Function

        ' ---- Tipos de códigos ----

        Friend Shared Function SqlIrA(destino As IrATipo) As String
            Select Case destino
                Case IrATipo.Primero
                    Return "SELECT MIN(TIPO) FROM gactiposcodigos"
                Case IrATipo.Anterior
                    Return "SELECT TIPO FROM gactiposcodigos WHERE TIPO < @tipo ORDER BY TIPO DESC LIMIT 1"
                Case IrATipo.Siguiente
                    Return "SELECT TIPO FROM gactiposcodigos WHERE TIPO > @tipo ORDER BY TIPO LIMIT 1"
                Case Else
                    Return "SELECT MAX(TIPO) FROM gactiposcodigos"
            End Select
        End Function

        ''' <summary>El tipo al que lleva el botón, o Nothing si no hay más en esa dirección.</summary>
        Public Async Function IrAAsync(destino As IrATipo, tipo As String, Optional ct As CancellationToken = Nothing) As Task(Of TipoCodigo)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim encontrado = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(SqlIrA(destino), New With {tipo}, cancellationToken:=ct)).ConfigureAwait(False)
                If encontrado Is Nothing Then Return Nothing
                Return Await cn.QueryFirstOrDefaultAsync(Of TipoCodigo)(New CommandDefinition($"SELECT {CamposTipo} FROM gactiposcodigos WHERE TIPO = @encontrado", New With {encontrado}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function ObtenerTipoAsync(tipo As String, Optional ct As CancellationToken = Nothing) As Task(Of TipoCodigo)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of TipoCodigo)(New CommandDefinition($"SELECT {CamposTipo} FROM gactiposcodigos WHERE TIPO = @tipo", New With {tipo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function BuscarTiposAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT TIPO AS 'Código', DESCRIPCION AS 'Descripción', IF(SISTEMA = '1', 'SISTEMA', 'USUARIO') AS 'Tipo', IF(ACTIVO = '1', 'SI', 'NO') AS 'Activo' FROM gactiposcodigos ORDER BY TIPO"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await TablaAsync(cn, sql, Nothing, ct).ConfigureAwait(False)
            End Using
        End Function

        Friend Shared Function SentenciaNuevoTipo(t As TipoCodigo, uid As Long) As String
            Return $"INSERT INTO gactiposcodigos (TIPO, DESCRIPCION, SISTEMA, ACTIVO, UIDC, FE, FC) VALUES ({Q(t.Tipo)}, {Q(t.Descripcion)}, {Q(t.Sistema)}, {Q(t.Activo)}, {uid}, NOW(), NOW())"
        End Function

        ''' <summary>El campo Sistema no se puede cambiar, igual que en VB6.</summary>
        Friend Shared Function SentenciaEditarTipo(t As TipoCodigo) As String
            Return $"UPDATE gactiposcodigos SET DESCRIPCION = {Q(t.Descripcion)}, ACTIVO = {Q(t.Activo)} WHERE TIPO = {Q(t.Tipo)}"
        End Function

        Public Async Function GuardarTipoAsync(t As TipoCodigo, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim existe = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition("SELECT COUNT(TIPO) FROM gactiposcodigos WHERE TIPO = @Tipo", New With {t.Tipo}, cancellationToken:=ct)).ConfigureAwait(False) > 0
                Await EjecutarReplicadoAsync(cn, ReplicaTipos, If(existe, SentenciaEditarTipo(t), SentenciaNuevoTipo(t, uid)), local, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>False si algún código usa el tipo. Como en VB6, borrar un tipo no se replica.</summary>
        Public Async Function BorrarTipoAsync(tipo As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            If String.IsNullOrEmpty(tipo) Then Return False
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim usos = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition($"SELECT COUNT(CODIGO) FROM gaccodclientes WHERE LEFT(CODIGO, {tipo.Length}) = @tipo", New With {tipo}, cancellationToken:=ct)).ConfigureAwait(False)
                If usos > 0 Then Return False
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM gactiposcodigos WHERE TIPO = @tipo", New With {tipo}, cancellationToken:=ct)).ConfigureAwait(False)
                Return True
            End Using
        End Function

    End Class

End Namespace
