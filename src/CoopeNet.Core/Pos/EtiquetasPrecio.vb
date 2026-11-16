Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>
    ''' Nombres de los seis precios de venta (pos_etiquetaprecio0 a 5 en gatparametros). Migración de
    ''' SD0502\FrmEtiquetasPrecios.frm. El Catálogo de Artículos y Clientes los muestran junto a cada precio.
    ''' </summary>
    ''' <remarks>
    ''' Se leen del local o, si no los tiene, del local 1000 (SD_LoadFromParametros). Se guardan en el local con el
    ''' REPLACE de SD_SaveInParametros, que también anota quién y cuándo; no se replican.
    ''' </remarks>
    Public NotInheritable Class EtiquetasPrecioRepositorio

        Public Const Cantidad As Integer = 6
        Public Const LargoValor As Integer = 255

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Shared Function Parametro(indice As Integer) As String
            Return $"pos_etiquetaprecio{indice}"
        End Function

        ''' <summary>Form_Load: las seis etiquetas, vacías las que no existan.</summary>
        Public Async Function LeerAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of IReadOnlyList(Of String))
            Dim parametros As New Parametros(_fabrica)
            Dim lista As New List(Of String)
            For i = 0 To Cantidad - 1
                lista.Add(Await parametros.ObtenerAsync(local, Parametro(i), ct).ConfigureAwait(False))
            Next
            Return lista
        End Function

        ''' <summary>cmdAceptar: las seis, en una sola conexión.</summary>
        Public Async Function GuardarAsync(local As Long, etiquetas As IReadOnlyList(Of String), uid As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                For i = 0 To Cantidad - 1
                    Dim valor = If(i < etiquetas.Count, If(etiquetas(i), ""), "").Trim()
                    If valor.Length > LargoValor Then valor = valor.Substring(0, LargoValor)
                    Await cn.ExecuteAsync(New CommandDefinition(
                        "REPLACE INTO gatparametros (Local, Parametro, Valor, UIDC, FC) VALUES (@local, @parametro, @valor, @uid, NOW())",
                        New With {local, .parametro = Parametro(i), valor, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Next
            End Using
        End Function

    End Class

End Namespace
