Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Catalogos
Imports CoopeNet.Core.Datos

Namespace Impresion

    ''' <summary>Fila de gatimpresion para un documento. Original es Nothing en una fila nueva.</summary>
    Public NotInheritable Class FilaImpresion
        ''' <summary>-1 es todos los departamentos.</summary>
        Public Property Local As Long = -1
        ''' <summary>"-1" es todas las estaciones.</summary>
        Public Property Estacion As String = "-1"
        Public Property Impresora As String = ""
        Public Property LlevaConsecutivo As Boolean
        ''' <summary>El último número usado.</summary>
        Public Property Consecutivo As Long
        ''' <summary>0 imprime con PrintApi, 1 con Active Report.</summary>
        Public Property Tipo As Integer
        Public Property Original As FilaImpresion
        Public Property Borrar As Boolean

        Public Function Copia() As FilaImpresion
            Return New FilaImpresion With {.Local = Local, .Estacion = Estacion, .Impresora = Impresora, .LlevaConsecutivo = LlevaConsecutivo, .Consecutivo = Consecutivo, .Tipo = Tipo}
        End Function

        Public Function MismosDatos(otra As FilaImpresion) As Boolean
            Return otra IsNot Nothing AndAlso Local = otra.Local AndAlso Estacion = otra.Estacion AndAlso Impresora = otra.Impresora AndAlso
                   LlevaConsecutivo = otra.LlevaConsecutivo AndAlso Consecutivo = otra.Consecutivo AndAlso Tipo = otra.Tipo
        End Function
    End Class

    ''' <summary>Una sentencia de Aplicar, o el error de la fila.</summary>
    Public NotInheritable Class ResultadoImpresion
        Public Property Fila As FilaImpresion
        Public Property Mensaje As String = ""
    End Class

    ''' <summary>
    ''' Configuración de impresión. Migración de Comun\FrmManImp.frm, que SD0001 abre con la opción 048: la
    ''' impresora y el consecutivo de cada documento por departamento y estación, en gatimpresion. Igual que
    ''' en VB6, no se replica. Difiere en que el consecutivo se ve en todos los documentos (VB6 solo lo
    ''' mostraba en CHEQUES, aunque los recibos de CXC y CXP y algunas cuentas de cheques lo usan) y en que
    ''' el UPDATE graba la fila completa: en VB6 cada campo pisaba al anterior y con dos cambios fallaba.
    ''' </summary>
    Public NotInheritable Class ConfiguracionImpresion

        Public Const Todos As String = "-1"

        ''' <summary>Load_Tipos_Documentos: los que aparecen aunque todavía no tengan filas.</summary>
        Public Shared ReadOnly DocumentosFijos As IReadOnlyList(Of String) = {
            "GAC - RETIRO DE PRENDA", "GAC - FORMALIZACION DE PRENDA",
            "GPC - SOLICITUD DE ANTICIPOS", "GPC - RETIRO DE ANTICIPOS",
            "GAR - FACTURA", "GAR - RECIBO"}

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Shared Function UnirDocumentos(enLaBase As IEnumerable(Of String)) As List(Of String)
            Return enLaBase.Concat(DocumentosFijos).Where(Function(d) Not String.IsNullOrWhiteSpace(d)).
                Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Function(d) d, StringComparer.OrdinalIgnoreCase).ToList()
        End Function

        Public Async Function DocumentosAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of String))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return UnirDocumentos(Await cn.QueryAsync(Of String)(New CommandDefinition("SELECT DISTINCT documento FROM gatimpresion", cancellationToken:=ct)).ConfigureAwait(False))
            End Using
        End Function

        Public Async Function LocalesAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of ItemCatalogo))
            Const sql As String = "SELECT CAST(local AS CHAR) AS Codigo, CONCAT(CAST(local AS CHAR), ' => ', descripcion) AS Descripcion FROM gatlocales ORDER BY local"
            Dim lista As New List(Of ItemCatalogo) From {New ItemCatalogo With {.Codigo = Todos, .Descripcion = "(TODOS LOS DEPARTAMENTOS)"}}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                lista.AddRange(Await cn.QueryAsync(Of ItemCatalogo)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False))
            End Using
            Return lista
        End Function

        Public Async Function EstacionesAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of ItemCatalogo))
            Const sql As String = "SELECT Estacion AS Codigo, CONCAT(Descripcion, ' (', IP, ')') AS Descripcion FROM gatestaciones WHERE Activo = '1' ORDER BY Descripcion"
            Dim lista As New List(Of ItemCatalogo) From {New ItemCatalogo With {.Codigo = Todos, .Descripcion = "(TODAS LAS ESTACIONES)"}}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                lista.AddRange(Await cn.QueryAsync(Of ItemCatalogo)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False))
            End Using
            Return lista
        End Function

        ''' <summary>Las impresoras de red ya configuradas, para sugerirlas.</summary>
        Public Async Function ImpresorasAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of String))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of String)(New CommandDefinition("SELECT DISTINCT impresora FROM gatimpresion WHERE TRIM(impresora) <> '' ORDER BY impresora", cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        Public Async Function FilasAsync(documento As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of FilaImpresion))
            Const sql As String = "SELECT local AS Local, estacion AS Estacion, impresora AS Impresora, llevaconsecutivo = '1' AS LlevaConsecutivo, " &
                                  "CAST(consecutivo AS SIGNED) AS Consecutivo, Type AS Tipo FROM gatimpresion WHERE documento = @documento ORDER BY local, estacion"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = (Await cn.QueryAsync(Of FilaImpresion)(New CommandDefinition(sql, New With {documento}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                For Each f In filas
                    f.Original = f.Copia()
                Next
                Return filas
            End Using
        End Function

        ''' <summary>La sentencia de SD_Guardar para una fila, o Nothing si no hay nada que hacer.</summary>
        Public Shared Function Sentencia(documento As String, f As FilaImpresion) As CommandDefinition?
            Dim datos As New DynamicParameters()
            datos.Add("documento", documento)
            datos.Add("documentoNuevo", documento.ToUpperInvariant())
            datos.Add("localOrg", If(f.Original?.Local, 0L))
            datos.Add("estacionOrg", If(f.Original?.Estacion, ""))
            datos.Add("Local", f.Local)
            datos.Add("Estacion", f.Estacion)
            datos.Add("Impresora", If(f.Impresora, "").Trim())
            datos.Add("Lleva", If(f.LlevaConsecutivo, "1", "0"))
            datos.Add("Consecutivo", f.Consecutivo)
            datos.Add("Tipo", f.Tipo)
            If f.Original Is Nothing Then
                If f.Borrar OrElse String.IsNullOrWhiteSpace(f.Estacion) Then Return Nothing
                Return New CommandDefinition("INSERT INTO gatimpresion (local, estacion, documento, impresora, llevaconsecutivo, consecutivo, Type) " &
                                             "VALUES (@Local, @Estacion, @documentoNuevo, @Impresora, @Lleva, @Consecutivo, @Tipo)", datos)
            End If
            If f.Borrar Then Return New CommandDefinition("DELETE FROM gatimpresion WHERE local = @localOrg AND estacion = @estacionOrg AND documento = @documento", datos)
            If f.MismosDatos(f.Original) Then Return Nothing
            Return New CommandDefinition("UPDATE gatimpresion SET local = @Local, estacion = @Estacion, impresora = @Impresora, llevaconsecutivo = @Lleva, consecutivo = @Consecutivo, Type = @Tipo " &
                                         "WHERE local = @localOrg AND estacion = @estacionOrg AND documento = @documento", datos)
        End Function

        ''' <summary>
        ''' Aplica fila por fila, sin transacción porque la tabla es MyISAM. Primero borra, para que una fila nueva
        ''' pueda tomar la llave de una borrada. Devuelve las que fallaron, por ejemplo por llave repetida.
        ''' </summary>
        Public Async Function AplicarAsync(documento As String, filas As IEnumerable(Of FilaImpresion), Optional ct As CancellationToken = Nothing) As Task(Of List(Of ResultadoImpresion))
            Dim errores As New List(Of ResultadoImpresion)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                For Each f In filas.OrderBy(Function(x) If(x.Borrar, 0, 1))
                    Dim comando = Sentencia(documento, f)
                    If comando Is Nothing Then Continue For
                    Try
                        Await cn.ExecuteAsync(comando.Value).ConfigureAwait(False)
                    Catch ex As MySqlConnector.MySqlException
                        errores.Add(New ResultadoImpresion With {.Fila = f, .Mensaje = ex.Message})
                    End Try
                Next
            End Using
            Return errores
        End Function

    End Class

End Namespace
