Imports System.Data
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Catalogos

    ''' <summary>Lo que se digita en el buscador del padrón. En el jurídico solo cuentan Cedula y Nombre.</summary>
    Public NotInheritable Class CriterioPadron
        Public Property Juridico As Boolean
        Public Property Cedula As String = ""
        Public Property Apellido1 As String = ""
        Public Property Apellido2 As String = ""
        Public Property Nombre As String = ""
    End Class

    ''' <summary>
    ''' Padrón Nacional y Padrón Jurídico. Migración de SD_Padron y SD_PadronJ de SD0001\FrmBuscaPadron.frm.
    ''' Como en VB6, la cédula manda sobre los demás campos y busca por el inicio; los apellidos son exactos
    ''' y el nombre se busca en cualquier parte. Se leen hasta 501 filas para saber si hay más de 500.
    ''' </summary>
    Public NotInheritable Class PadronRepositorio

        Public Const Maximo As Integer = 500

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>Solo los dígitos: la máscara de VB6 entregaba la cédula sin guiones.</summary>
        Friend Shared Function Digitos(texto As String) As String
            Return New String(If(texto, "").Where(AddressOf Char.IsDigit).ToArray())
        End Function

        ''' <summary>Vacío si no hay nada que buscar. En VB6 eso armaba un WHERE vacío y la consulta fallaba.</summary>
        Friend Shared Function Sentencia(c As CriterioPadron, parametros As DynamicParameters) As String
            Dim filtros As New List(Of String)
            Dim cedula = Digitos(c.Cedula)
            If cedula.Length > 0 Then
                filtros.Add("Cedula LIKE @cedula")
                parametros.Add("cedula", cedula & "%")
            ElseIf c.Juridico Then
                AgregarNombre(c, filtros, parametros)
            Else
                If c.Apellido1.Trim().Length > 0 Then
                    filtros.Add("Apellido1 = @apellido1")
                    parametros.Add("apellido1", c.Apellido1.Trim())
                End If
                If c.Apellido2.Trim().Length > 0 Then
                    filtros.Add("Apellido2 = @apellido2")
                    parametros.Add("apellido2", c.Apellido2.Trim())
                End If
                AgregarNombre(c, filtros, parametros)
            End If
            If filtros.Count = 0 Then Return ""

            Dim donde = String.Join(" AND ", filtros)
            If c.Juridico Then
                Return $"SELECT Cedula AS 'Cédula', Nombre FROM padron.padronj WHERE {donde} ORDER BY Nombre LIMIT {Maximo + 1}"
            End If
            Return "SELECT Cedula AS 'Cédula', Apellido1 AS '1er apellido', Apellido2 AS '2do apellido', Nombre, Sexo, Nacimiento " &
                   $"FROM padron.padron WHERE {donde} ORDER BY Apellido1, Apellido2, Nombre LIMIT {Maximo + 1}"
        End Function

        Private Shared Sub AgregarNombre(c As CriterioPadron, filtros As List(Of String), parametros As DynamicParameters)
            If c.Nombre.Trim().Length = 0 Then Return
            filtros.Add("Nombre LIKE @nombre")
            parametros.Add("nombre", $"%{c.Nombre.Trim()}%")
        End Sub

        ''' <summary>Nothing si no hay criterio. Si el padrón trae más de 500, se dejan 500 y Excedido es True.</summary>
        Public Async Function BuscarAsync(c As CriterioPadron, Optional ct As CancellationToken = Nothing) As Task(Of (Tabla As DataTable, Excedido As Boolean))
            Dim parametros As New DynamicParameters()
            Dim sql = Sentencia(c, parametros)
            If sql.Length = 0 Then Return (Nothing, False)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, parametros, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Dim excedido = tabla.Rows.Count > Maximo
                    If excedido Then tabla.Rows.RemoveAt(Maximo)
                    Return (tabla, excedido)
                End Using
            End Using
        End Function

    End Class

End Namespace
