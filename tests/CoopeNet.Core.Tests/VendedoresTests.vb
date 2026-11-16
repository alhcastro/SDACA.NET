Imports CoopeNet.Core.Pos

Public Class VendedoresTests

    <Fact>
    Public Sub La_fecha_cabe_en_el_int_de_FC()
        Assert.Equal(20261116, VendedoresRepositorio.FechaEntera(New Date(2026, 11, 16, 23, 59, 0)))
        Assert.True(VendedoresRepositorio.FechaEntera(New Date(9999, 12, 31)) < Integer.MaxValue)
    End Sub

End Class
