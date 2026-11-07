Imports CoopeNet.Core.Datos
Imports CoopeNet.Core.Pos

Public Class CodigosBarrasTests

    Private Shared Function Destinos() As List(Of DestinoReplica)
        Return New List(Of DestinoReplica) From {
            New DestinoReplica With {.ALocal = "2", .CambiaLoc = "1"},
            New DestinoReplica With {.ALocal = "9", .CambiaLoc = "1"}}
    End Function

    <Fact>
    Public Sub El_alta_va_al_local_de_destino()
        Dim c As New CodigoBarras With {.CAlterno = " 7441'01 ", .Unidades = 12.5}
        Dim s = Replicador.ArmarSentencias(CodigosBarrasRepositorio.SentenciaAlta("0002", c), 1, Destinos())
        Assert.Equal("REPLACE INTO poscodart_codbarra (articulo, CAlterno, Unidades, FModif, Local) VALUES ('0002', '7441\'01', 12.5, NULL, 2)", s(0))
        Assert.EndsWith("NULL, 9)", s(1))
    End Sub

    <Theory>
    <InlineData(0)>
    <InlineData(-3)>
    Public Sub Sin_unidades_se_guarda_una(unidades As Double)
        Dim s = CodigosBarrasRepositorio.SentenciaAlta("1", New CodigoBarras With {.CAlterno = "1", .Unidades = unidades})
        Assert.Contains("'1', 1, NULL", s)
    End Sub

    <Fact>
    Public Sub El_borrado_es_del_articulo_en_el_local()
        Assert.Equal("DELETE FROM poscodart_codbarra WHERE articulo = '0002' AND Local = 2",
                     Replicador.ArmarSentencias(CodigosBarrasRepositorio.SentenciaBorrar("0002"), 1, Destinos())(0))
    End Sub

End Class
