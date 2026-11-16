Imports CoopeNet.Core.Pos

Public Class ClavesSupervisorTests

    <Theory>
    <InlineData(3, 100, 999)>
    <InlineData(4, 1000, 9999)>
    <InlineData(5, 10000, 99999)>
    Public Sub La_clave_tiene_los_digitos_pedidos(digitos As Integer, minimo As Integer, maximo As Integer)
        For i = 1 To 200
            Dim clave = ClavesSupervisorRepositorio.Generar(digitos)
            Assert.Equal(digitos, clave.Length)
            Assert.InRange(Integer.Parse(clave), minimo, maximo)
        Next
    End Sub

    <Fact>
    Public Sub Solo_de_3_a_5_digitos()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() ClavesSupervisorRepositorio.Generar(6))
    End Sub

End Class
