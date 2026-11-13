Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una fila de poscodart_codbarra: un código de barras del artículo y cuántas unidades vende.</summary>
    Public NotInheritable Class CodigoBarras
        Public Property CAlterno As String = ""
        Public Property Unidades As Double = 1
    End Class

    ''' <summary>
    ''' Códigos de barras del artículo. Migración de SD0502\frmCodBarra.frm (SD_BuscaBar y SD_Guardar). Cada
    ''' sentencia se replica como poscodart_codbarra, con $Local para el local de destino.
    ''' </summary>
    Public NotInheritable Class CodigosBarrasRepositorio

        Public Const Replica As String = "poscodart_codbarra"
        Public Const LargoCodigo As Integer = 20
        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture

        Private ReadOnly _fabrica As IFabricaConexiones
        Private ReadOnly _pausaReplica As TimeSpan

        ''' <param name="pausaReplica">La pausa de VB6 entre replicar el borrado y las altas (gatreplica se procesa por Fecha).</param>
        Public Sub New(fabrica As IFabricaConexiones, Optional pausaReplica As TimeSpan? = Nothing)
            _fabrica = fabrica
            _pausaReplica = If(pausaReplica, TimeSpan.FromSeconds(1))
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        Public Async Function ListarAsync(articulo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of CodigoBarras))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of CodigoBarras)(New CommandDefinition(
                    "SELECT CAlterno, Unidades FROM poscodart_codbarra WHERE articulo = @articulo AND Local = @local ORDER BY CAlterno",
                    New With {articulo, local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>
        ''' "Artículo - Descripción" del otro artículo que ya tiene el código en el local, o vacío. VB6 lo revisaba
        ''' al guardar; aquí se revisa antes de agregar el código a la lista.
        ''' </summary>
        Public Async Function UsadoPorAsync(codigo As String, articulo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(
                    "SELECT CONCAT(A.CodigoArt, ' - ', A.Descripcion) FROM poscodart_codbarra C INNER JOIN posarticulos A ON A.CodigoArt = C.articulo " &
                    "WHERE C.CAlterno = @codigo AND C.Local = @local AND C.articulo <> @articulo LIMIT 1",
                    New With {codigo, articulo, local}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        Friend Shared Function SentenciaBorrar(articulo As String) As String
            Return $"DELETE FROM poscodart_codbarra WHERE articulo = {Q(articulo)} AND Local = $Local"
        End Function

        ''' <summary>VB6 guarda 1 si las unidades no son mayores que cero.</summary>
        Friend Shared Function SentenciaAlta(articulo As String, c As CodigoBarras) As String
            Dim unidades = If(c.Unidades > 0, c.Unidades, 1)
            Return "REPLACE INTO poscodart_codbarra (articulo, CAlterno, Unidades, FModif, Local) VALUES (" &
                $"{Q(articulo)}, {Q(c.CAlterno.Trim())}, {unidades.ToString("R", Invariante)}, NULL, $Local)"
        End Function

        Private Shared Async Function EjecutarYReplicarAsync(cn As MySqlConnection, plantilla As String, local As Long, ct As CancellationToken) As Task
            Await cn.ExecuteAsync(New CommandDefinition(plantilla.Replace("$Local", local.ToString(Invariante)), cancellationToken:=ct)).ConfigureAwait(False)
            Await Replicador.ReplicarAsync(cn, Replica, plantilla, local, ct).ConfigureAwait(False)
        End Function

        ''' <summary>Agrega o reemplaza un código, sin tocar los demás.</summary>
        Public Async Function AgregarAsync(articulo As String, local As Long, codigo As CodigoBarras, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarYReplicarAsync(cn, SentenciaAlta(articulo, codigo), local, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>SD_Guardar: borra los códigos del artículo en el local y graba los de la lista. Sin transacción: MyISAM.</summary>
        Public Async Function GuardarAsync(articulo As String, local As Long, codigos As IEnumerable(Of CodigoBarras), Optional ct As CancellationToken = Nothing) As Task
            Dim filas = codigos.Where(Function(c) Not String.IsNullOrWhiteSpace(c.CAlterno)).ToList()
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarYReplicarAsync(cn, SentenciaBorrar(articulo), local, ct).ConfigureAwait(False)
                If filas.Count = 0 Then Return
                Await Task.Delay(_pausaReplica, ct).ConfigureAwait(False)
                For Each c In filas
                    Await EjecutarYReplicarAsync(cn, SentenciaAlta(articulo, c), local, ct).ConfigureAwait(False)
                Next
            End Using
        End Function

    End Class

End Namespace
