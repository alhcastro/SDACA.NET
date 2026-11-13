Imports CoopeNet.Core.Pos

Public Class TarjetasTests

    <Fact>
    Public Sub Guardar_escribe_todas_las_columnas_y_conserva_la_creacion()
        Dim t As New TarjetaPos With {
            .Codigo = 3, .Nombre = "b. de costa rica (ath)", .Telefono = "2211-1111", .Fax = "", .TelefonoConexion = "800-123",
            .TelefonoConsulta = "2287-0000", .PComision = 2.5, .FC = New Date(2005, 3, 28, 16, 3, 0)}
        Assert.Equal("REPLACE INTO postarjetas (Codigo, Nombre, Telefono, Fax, TelefonoConexion, TelefonoConsulta, PComision, FC, Usuario) VALUES (" &
                     "'3', 'B. DE COSTA RICA (ATH)', '2211-1111', '', '800-123', '2287-0000', 2.5, '2005-03-28 16:03:00', '51')",
                     TarjetasRepositorio.SentenciaGuardar(t, 51, New Date(2026, 11, 13)))
    End Sub

    <Fact>
    Public Sub Una_nueva_se_crea_ahora_y_recorta_y_escapa()
        Dim t As New TarjetaPos With {.Codigo = 4, .Nombre = "O'Neil " & New String("x"c, 40), .Fax = New String("9"c, 20)}
        Dim sql = TarjetasRepositorio.SentenciaGuardar(t, 1, New Date(2026, 11, 13, 9, 5, 0))
        Assert.Contains("'2026-11-13 09:05:00'", sql)
        Assert.Contains("'O\'NEIL " & New String("X"c, 25) & "'", sql)
        Assert.Contains($"'{New String("9"c, 16)}'", sql)
    End Sub

    <Theory>
    <InlineData("", 0.0)>
    <InlineData("2.5", 2.5)>
    <InlineData("3,75", 3.75)>
    <InlineData("100", 100.0)>
    Public Sub Lee_la_comision(texto As String, esperado As Double)
        Assert.Equal(esperado, TarjetasRepositorio.LeerComision(texto))
    End Sub

    <Theory>
    <InlineData("101")>
    <InlineData("abc")>
    <InlineData("1.2.3")>
    Public Sub Rechaza_comisiones_invalidas(texto As String)
        Assert.Null(TarjetasRepositorio.LeerComision(texto))
    End Sub

End Class
