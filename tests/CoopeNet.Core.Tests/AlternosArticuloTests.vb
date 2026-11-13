Imports CoopeNet.Core.Datos
Imports CoopeNet.Core.Pos

Public Class AlternosArticuloTests

    <Fact>
    Public Sub Guardar_es_un_replace_con_el_local_de_destino()
        Dim s = AlternosArticuloRepositorio.SentenciaGuardar("0002", "0003", " leche 'deslactosada' ", 15, New Date(2026, 11, 7, 9, 5, 3))
        Assert.Equal("REPLACE INTO posarticulosalternos (CodigoArt, CodAlterno, Detalle, Local, Usuario, FC, FM) VALUES ('0002', '0003', 'LECHE \'DESLACTOSADA\'', $Local, '15', '2026-11-07 09:05:03', NOW())", s)
        Dim destinos = New List(Of DestinoReplica) From {New DestinoReplica With {.ALocal = "2", .CambiaLoc = "1"}}
        Assert.Contains("'LECHE \'DESLACTOSADA\'', 2, '15'", Replicador.ArmarSentencias(s, 1, destinos)(0))
    End Sub

    <Fact>
    Public Sub El_borrado_compara_el_alterno_como_texto()
        Assert.Equal("DELETE FROM posarticulosalternos WHERE Local = $Local AND CodigoArt = '0002' AND CodAlterno = '007'",
                     AlternosArticuloRepositorio.SentenciaBorrar("0002", "007"))
    End Sub

    <Fact>
    Public Sub El_detalle_se_recorta_a_255()
        Assert.Equal(255, AlternosArticuloRepositorio.Detalle(New String("a"c, 300)).Length)
    End Sub

End Class
