Imports SDACA.Core.Bodegas

Public Class TipoPrestamoTests

    <Theory>
    <InlineData("1", True)>
    <InlineData("0", False)>
    <InlineData("", False)>
    <InlineData(Nothing, False)>
    Public Sub RequiereAprobacion_solo_con_estado_1(estado As String, esperado As Boolean)
        Dim tipo As New TipoPrestamo With {.Estado = estado}
        Assert.Equal(esperado, tipo.RequiereAprobacion)
    End Sub

End Class
