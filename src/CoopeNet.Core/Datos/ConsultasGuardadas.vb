Imports System.Data
Imports System.Globalization
Imports System.Text
Imports System.Threading
Imports Dapper
Imports MySqlConnector

Namespace Datos

    ''' <summary>Una fila de posconsultas.</summary>
    Public NotInheritable Class ConsultaGuardada
        Public Property Codigo As Long
        Public Property Nombre As String = ""
        ''' <summary>"1" muestra todas las filas; cualquier otro valor limita a 1000.</summary>
        Public Property Filtro As String = ""

        Public ReadOnly Property SinLimite As Boolean
            Get
                Return Filtro.Trim() = "1"
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{Codigo} - {Nombre}"
        End Function
    End Class

    ''' <summary>
    ''' Consultas guardadas en posconsultas. Migración de SD0502\REP\frmConsultas.frm (ConsultasMod), que también
    ''' abre "Histórico utilidades" del Catálogo de Artículos con la consulta 1.
    ''' </summary>
    ''' <remarks>
    ''' VB6 corría cada consulta con CREATE TABLE posconsultastmpUID y, para que posarticuloslocal tuviera solo el
    ''' local del usuario, la copiaba a posinvent_tmp_LOCAL y cambiaba el nombre en el SQL. Aquí la consulta se
    ''' ejecuta dentro de SELECT * FROM (...), que solo admite una sentencia SELECT, y el local se separa con una
    ''' tabla TEMPORARY de la sesión que tapa a posarticuloslocal mientras corre. No quedan tablas en la base.
    ''' </remarks>
    Public NotInheritable Class ConsultasGuardadasRepositorio

        Public Const Replica As String = "posconsultas"
        Public Const LimiteFilas As Integer = 1000

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>SD_Combos: las consultas activas.</summary>
        Public Async Function ListarAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of ConsultaGuardada))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of ConsultaGuardada)(New CommandDefinition("SELECT codigo AS Codigo, nombre AS Nombre, filtro AS Filtro FROM posconsultas WHERE activo = '1' ORDER BY codigo", cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        Public Async Function SqlAsync(codigo As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT CAST(iSQL AS CHAR) FROM posconsultas WHERE codigo = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ''' <summary>
        ''' Deja una sola sentencia: quita los punto y coma del final y rechaza la consulta si queda otro fuera de
        ''' comillas o comentarios, para que nadie meta una segunda sentencia.
        ''' </summary>
        Public Shared Function UnaSentencia(sql As String) As String
            Dim texto = If(sql, "").Trim()
            While texto.EndsWith(";", StringComparison.Ordinal)
                texto = texto.Substring(0, texto.Length - 1).TrimEnd()
            End While
            If texto.Length = 0 Then Throw New ArgumentException("La consulta no tiene SQL.")
            Dim comilla As Char = Nothing
            Dim i = 0
            While i < texto.Length
                Dim c = texto(i)
                If comilla <> Nothing Then
                    If c = "\"c Then
                        i += 1
                    ElseIf c = comilla Then
                        comilla = Nothing
                    End If
                ElseIf c = "'"c OrElse c = """"c OrElse c = "`"c Then
                    comilla = c
                ElseIf c = "-"c AndAlso i + 1 < texto.Length AndAlso texto(i + 1) = "-"c OrElse c = "#"c Then
                    Dim fin = texto.IndexOf(vbLf, i, StringComparison.Ordinal)
                    i = If(fin < 0, texto.Length, fin)
                ElseIf c = "/"c AndAlso i + 1 < texto.Length AndAlso texto(i + 1) = "*"c Then
                    Dim fin = texto.IndexOf("*/", i + 2, StringComparison.Ordinal)
                    i = If(fin < 0, texto.Length, fin + 1)
                ElseIf c = ";"c Then
                    Throw New ArgumentException("La consulta debe ser una sola sentencia SELECT.")
                End If
                i += 1
            End While
            Return texto
        End Function

        ''' <summary>La consulta dentro de un SELECT, con el filtro por columna (cmdFiltrado) y el límite.</summary>
        Public Shared Function Envolver(sql As String, local As Long, columna As String, limite As Integer?) As String
            Dim interior = UnaSentencia(sql).Replace("$Local", local.ToString(CultureInfo.InvariantCulture))
            Dim sb As New StringBuilder()
            sb.Append("SELECT * FROM (").Append(vbLf).Append(interior).Append(vbLf).Append(") AS consulta")
            If Not String.IsNullOrEmpty(columna) Then sb.Append(" WHERE `").Append(columna.Replace("`", "``")).Append("` LIKE @patron")
            If limite.HasValue Then sb.Append(" LIMIT ").Append(limite.Value.ToString(CultureInfo.InvariantCulture))
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Ejecuta la consulta. Con <paramref name="soloLocal"/>, posarticuloslocal tiene solo las filas del local,
        ''' como la copia posinvent_tmp_LOCAL de VB6. Con <paramref name="columna"/>, filtra las filas cuyo valor
        ''' contiene <paramref name="texto"/>.
        ''' </summary>
        Public Async Function EjecutarAsync(sql As String, local As Long, soloLocal As Boolean, columna As String, texto As String, limite As Integer?, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Dim consulta = Envolver(sql, local, columna, limite)
            Const quitar As String = "DROP TEMPORARY TABLE IF EXISTS posarticuloslocal"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                If soloLocal Then
                    Await cn.ExecuteAsync(New CommandDefinition(quitar, cancellationToken:=ct)).ConfigureAwait(False)
                    Await cn.ExecuteAsync(New CommandDefinition("CREATE TEMPORARY TABLE posarticuloslocal SELECT * FROM posarticuloslocal WHERE CLocal = @local", New With {local}, commandTimeout:=300, cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Dim tabla As New DataTable()
                Dim fallo As Exception = Nothing
                Try
                    Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(consulta, New With {.patron = $"%{If(texto, "").Trim()}%"}, commandTimeout:=600, cancellationToken:=ct)).ConfigureAwait(False)
                        tabla.Load(lector)
                    End Using
                Catch ex As Exception
                    fallo = ex
                End Try
                ' La conexión vuelve al grupo: la tabla temporal no debe seguir tapando a la real.
                If soloLocal Then Await cn.ExecuteAsync(New CommandDefinition(quitar, cancellationToken:=CancellationToken.None)).ConfigureAwait(False)
                If fallo IsNot Nothing Then System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(fallo).Throw()
                Return tabla
            End Using
        End Function

        ' ---- Editor ----

        ''' <summary>SD_Tablas: las tablas de la base que empiezan con el prefijo.</summary>
        Public Async Function TablasAsync(prefijo As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of String))
            Dim sql = If(String.IsNullOrEmpty(prefijo), "SHOW TABLES", "SHOW TABLES LIKE @patron")
            Dim patron = If(prefijo, "").Replace("\", "\\").Replace("%", "\%").Replace("_", "\_") & "%"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of String)(New CommandDefinition(sql, New With {patron}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>lstTablas_Click: los campos como "nombre - tipo - llave", con el ancho de 20 de VB6.</summary>
        Public Async Function CamposAsync(tabla As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of String))
            Dim campos As New List(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition($"SHOW FIELDS FROM `{tabla.Replace("`", "``")}`", cancellationToken:=ct)).ConfigureAwait(False)
                    While Await lector.ReadAsync(ct).ConfigureAwait(False)
                        campos.Add($"{Convert.ToString(lector.GetValue(0)),-20} - {Convert.ToString(lector.GetValue(1)),-20} - {Convert.ToString(lector.GetValue(3))}")
                    End While
                End Using
            End Using
            Return campos
        End Function

        ''' <summary>cmdGuardar_Click: el REPLACE de VB6, con filtro vacío y activa, que se replica si gatreplicar es 1.</summary>
        Friend Shared Function SentenciaGuardar(codigo As Long, nombre As String, sql As String) As String
            Dim q = Function(t As String) t.Replace("\", "\\").Replace("'", "\'")
            Return $"REPLACE INTO posconsultas VALUES({codigo.ToString(CultureInfo.InvariantCulture)}, '{q(nombre.Trim())}', '{q(sql.Trim())}', '', NOW(), '1')"
        End Function

        Public Async Function GuardarAsync(codigo As Long, nombre As String, sql As String, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim sentencia = SentenciaGuardar(codigo, nombre, sql)
            Dim replicar = (Await New Parametros(_fabrica).ObtenerAsync(local, "gatreplicar", ct).ConfigureAwait(False)) = "1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sentencia, cancellationToken:=ct)).ConfigureAwait(False)
                If replicar Then Await Replicador.ReplicarAsync(cn, Replica, sentencia, local, ct).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
