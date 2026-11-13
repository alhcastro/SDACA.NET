Imports CoopeNet.Core.Pos

Public Class CajasTests

    Private Shared Function Ejemplo() As Caja
        Return New Caja With {
            .Estacion = "af308297a89deabed04b686cba21ec65", .Numero = "86", .Consecutivo = 86000123, .Facturacion = ModoFacturacion.Tactil,
            .Activa = True, .Uid = 565, .FC = New Date(2025, 6, 17, 8, 9, 40), .Consola = False, .LineaRapida = True}
    End Function

    <Fact>
    Public Sub Guardar_es_el_REPLACE_de_SD_Guardar()
        Assert.Equal("REPLACE INTO poscajas (Estacion, Caja, Consecutivo, Facturacion, FC, FM, Local, PCom1, LineaRap) VALUES (" &
                     "'af308297a89deabed04b686cba21ec65','86','86000123','3','2025-06-17 08:09:40',NOW(),'7','0','1')",
                     CajasRepositorio.SentenciaCaja(Ejemplo(), 7))
        Assert.Equal("REPLACE INTO poscajasusuarios(Estaci,CajaUs,Activo,UIDC,Local) VALUES('af308297a89deabed04b686cba21ec65','86','1','565',7)",
                     CajasRepositorio.SentenciaUsuario(Ejemplo(), 7))
    End Sub

    <Fact>
    Public Sub Una_caja_nueva_se_crea_hoy_y_escapa_las_comillas()
        Dim c = Ejemplo()
        c.FC = Nothing
        c.Estacion = "O'Brien"
        Dim sql = CajasRepositorio.SentenciaCaja(c, 7)
        Assert.Contains($"'{Date.Now:yyyy-MM-dd}", sql)
        Assert.DoesNotContain("'O'Brien'", sql)
    End Sub

    <Theory>
    <InlineData(86000123.0, "86", "000123")>
    <InlineData(4000000.0, "4", "000000")>
    <InlineData(120999999.0, "120", "999999")>
    Public Sub Parte_el_consecutivo_como_frmModConsec(consecutivo As Double, caja As String, numero As String)
        Dim partes = CajasRepositorio.PartirConsecutivo(consecutivo)
        Assert.True(partes.HasValue)
        Assert.Equal(caja, partes.Value.Caja)
        Assert.Equal(numero, partes.Value.Numero)
    End Sub

    <Fact>
    Public Sub Un_consecutivo_sin_caja_no_se_puede_modificar()
        Assert.False(CajasRepositorio.PartirConsecutivo(0).HasValue)
        Assert.False(CajasRepositorio.PartirConsecutivo(668).HasValue)
    End Sub

    <Fact>
    Public Sub El_consecutivo_inicial_es_la_caja_por_un_millon()
        Assert.Equal(86000000.0, CajasRepositorio.ConsecutivoInicial("86"))
    End Sub

End Class
