Imports CoopeNet.Core.Pos

Public Class BancosPosTests

    <Fact>
    Public Sub Guardar_conserva_la_fecha_de_creacion()
        Dim b As New BancoPos With {.Codigo = 10, .Nombre = "bac", .Telefono = "2295-9797", .FC = New Date(2021, 11, 16, 9, 56, 16)}
        Assert.Equal("REPLACE INTO posbancos (Codigo, Nombre, Telefono, Fax, FC, Usuario) VALUES ('10', 'BAC', '2295-9797', '', '2021-11-16 09:56:16', '515')",
                     BancosPosRepositorio.SentenciaGuardar(b, 515, New Date(2026, 11, 13)))
    End Sub

    <Fact>
    Public Sub Uno_nuevo_se_crea_ahora_y_recorta_y_escapa()
        Dim b As New BancoPos With {.Codigo = 11, .Nombre = "banco d'oro", .Telefono = New String("2"c, 20)}
        Dim sql = BancosPosRepositorio.SentenciaGuardar(b, 1, New Date(2026, 11, 13, 9, 5, 0))
        Assert.Contains("'BANCO D\'ORO'", sql)
        Assert.Contains($"'{New String("2"c, 16)}'", sql)
        Assert.Contains("'2026-11-13 09:05:00'", sql)
    End Sub

End Class
