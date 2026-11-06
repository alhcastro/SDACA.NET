Imports CoopeNet.Core.Catalogos

Public Class CatalogosMonetariosTests

    <Fact>
    Public Sub Banco_nuevo_no_toca_finport_y_escapa_el_texto()
        Dim sql = CatalogosMonetarios.SentenciaBanco(New BancoCatalogo With {.Codigo = 7, .Descripcion = "O'Banco"}, 3)
        Assert.Equal("INSERT INTO gatbancos (Banco, Descripcion, uidc, FC) VALUES (7, 'O\'Banco', 3, NOW()) ON DUPLICATE KEY UPDATE Descripcion = 'O\'Banco'", sql)
        Assert.DoesNotContain("Finport", sql)
    End Sub

    <Fact>
    Public Sub Moneda_incluye_iniciales_y_conserva_la_fecha_de_creacion()
        Dim sql = CatalogosMonetarios.SentenciaMoneda(New MonedaCatalogo With {.Codigo = 2, .Descripcion = "EURO", .Simbolo = "E", .Iniciales = "EUR"}, 3)
        Assert.Contains("(Moneda, Descripcion, Simbolo, Iniciales, uidc, FC) VALUES (2, 'EURO', 'E', 'EUR', 3, NOW())", sql)
        Assert.EndsWith("ON DUPLICATE KEY UPDATE Descripcion = 'EURO', Simbolo = 'E', Iniciales = 'EUR'", sql)
    End Sub

    <Fact>
    Public Sub Tipo_de_cambio_nuevo_y_existente_sin_banco_ni_hora()
        Dim tipo As New TipoCambio With {.Moneda = 1, .Fecha = #2026-11-06#, .Compra = 505.5, .Venta = 512.25}
        Assert.Equal("INSERT INTO gattipocambio (moneda, fecha, compra, venta, uidc, fc) VALUES (1, '2026-11-06', 505.5, 512.25, 3, NOW())", CatalogosMonetarios.SentenciaTipoCambio(tipo, 3, False))
        Assert.Equal("UPDATE gattipocambio SET compra = 505.5, venta = 512.25, uide = 3, fe = NOW() WHERE moneda = 1 AND fecha = '2026-11-06'", CatalogosMonetarios.SentenciaTipoCambio(tipo, 3, True))
    End Sub

End Class
