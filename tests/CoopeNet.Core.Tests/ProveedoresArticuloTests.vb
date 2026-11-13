Imports CoopeNet.Core.Datos
Imports CoopeNet.Core.Pos

Public Class ProveedoresArticuloTests

    Private Shared Function Destinos() As List(Of DestinoReplica)
        Return New List(Of DestinoReplica) From {
            New DestinoReplica With {.ALocal = "1", .CambiaLoc = "0"},
            New DestinoReplica With {.ALocal = "2", .CambiaLoc = "1"},
            New DestinoReplica With {.ALocal = "9", .CambiaLoc = "1"},
            New DestinoReplica With {.ALocal = "3", .CambiaLoc = "1"}}
    End Function

    <Fact>
    Public Sub La_cuenta_lleva_el_prefijo_de_cada_local()
        Dim p As New ProveedorArticulo With {.Proveedor = "3101", .CodArtProv = "AB'1", .Cuenta = "40828", .CantEmpaq = 12.5}
        Dim s = Replicador.ArmarSentencias(ProveedoresArticuloRepositorio.SentenciaAlta("0002", p), 1, Destinos())
        Assert.Equal("INSERT INTO poscodprove (proveedor, codartprov, codigoart, local, cantempaq, Cuenta) VALUES ('3101', 'AB\'1', '0002', 1, 12.5, '40828')", s(0))
        Assert.EndsWith("2, 12.5, 'R40828')", s(1))
        Assert.EndsWith("9, 12.5, 'Z40828')", s(2))
        Assert.EndsWith("3, 12.5, 'E40828')", s(3))
    End Sub

    <Theory>
    <InlineData("R40828")>
    <InlineData("E1")>
    <InlineData("Z2")>
    <InlineData("")>
    Public Sub Una_cuenta_con_prefijo_o_vacia_no_cambia(cuenta As String)
        Dim p As New ProveedorArticulo With {.Proveedor = "1", .CodArtProv = "1", .Cuenta = cuenta}
        For Each s In Replicador.ArmarSentencias(ProveedoresArticuloRepositorio.SentenciaAlta("1", p), 1, Destinos())
            Assert.EndsWith($", '{cuenta}')", s)
        Next
    End Sub

    <Fact>
    Public Sub El_borrado_es_del_local_de_destino()
        Dim s = Replicador.ArmarSentencias(ProveedoresArticuloRepositorio.SentenciaBorrar("0002"), 1, Destinos())
        Assert.Equal("DELETE FROM poscodprove WHERE codigoart = '0002' AND local = 1", s(0))
        Assert.Equal("DELETE FROM poscodprove WHERE codigoart = '0002' AND local = 2", s(1))
    End Sub

    <Theory>
    <InlineData("40828 - DISTRIBUIDORA", "40828")>
    <InlineData("R404 - A-B", "R404")>
    <InlineData("", "")>
    <InlineData(Nothing, "")>
    Public Sub La_cuenta_se_toma_antes_del_guion(opcion As String, esperado As String)
        Assert.Equal(esperado, ProveedoresArticuloRepositorio.CuentaDe(opcion))
    End Sub

End Class
