Imports System.Threading
Imports Dapper

Namespace Datos

    ''' <summary>
    ''' Catálogos del sistema en un servidor. Migración de <c>Command1_Click</c> (FrmConfigurarServidor)
    ''' y <c>SD_Validar_Catalogo</c> (Module01.bas): una base es del sistema si tiene al menos 22 de
    ''' estas 24 tablas.
    ''' </summary>
    ''' <remarks>
    ''' La lista va tal cual el original, con "gatmensagedest" y "gattiposmomeda" mal escritas: por eso
    ''' alcanzan 22 de 24.
    ''' </remarks>
    Public NotInheritable Class CatalogosServidor

        Public Const MinimoTablas As Integer = 22

        Public Shared ReadOnly TablasSistema As IReadOnlyList(Of String) = {
            "gatbancos", "gatbitacora", "gatcantones", "gatdistritos", "gatestaciones", "gatestadociv",
            "gatimagenes", "gatlocales", "gatmensagedest", "gatmensajes", "gatmodulos", "gatmoduloslocal",
            "gatpais", "gatparametros", "gatparentescos", "gatprivilegios", "gatprovincias", "gatrecursos",
            "gatredondeo", "gatsectores", "gatsysusuarios", "gattiposmomeda", "gatusuarios", "gatusuariospriv2"
        }

        Private Sub New()
        End Sub

        ''' <summary>Catálogos visibles para el usuario de la conexión que tienen las tablas del sistema.</summary>
        Public Shared Async Function ListarAsync(fabrica As IFabricaConexiones, Optional ct As CancellationToken = Nothing) As Task(Of IReadOnlyList(Of String))
            Const sql As String =
                "SELECT table_schema FROM information_schema.tables WHERE LOWER(table_name) IN @tablas " &
                "GROUP BY table_schema HAVING COUNT(DISTINCT LOWER(table_name)) >= @minimo ORDER BY table_schema;"
            Using cn = Await fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of String)(New CommandDefinition(sql, New With {.tablas = TablasSistema, .minimo = MinimoTablas}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

    End Class

End Namespace
