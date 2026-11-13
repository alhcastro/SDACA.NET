Imports CoopeNet.Core.Pos

Public Class JustificacionesTests

    <Fact>
    Public Sub Los_movimientos_siguen_el_valor_de_Accion()
        Assert.Equal(6, JustificacionesRepositorio.Movimientos.Count)
        For Each m As MovimientoJustificado In [Enum].GetValues(GetType(MovimientoJustificado))
            Assert.StartsWith($"{CInt(m)} - ", JustificacionesRepositorio.Movimientos(CInt(m)))
        Next
    End Sub

    <Theory>
    <InlineData("  error del cajero ", "ERROR DEL CAJERO")>
    <InlineData("decisión del cliente", "DECISIÓN DEL CLIENTE")>
    <InlineData(Nothing, "")>
    Public Sub La_descripcion_va_en_mayusculas_como_SDTDescri_LostFocus(entrada As String, esperado As String)
        Assert.Equal(esperado, JustificacionesRepositorio.Normalizar(entrada))
    End Sub

    <Fact>
    Public Sub La_descripcion_se_recorta_al_largo_de_Descri()
        Assert.Equal(JustificacionesRepositorio.LargoDescripcion, JustificacionesRepositorio.Normalizar(New String("a"c, 200)).Length)
    End Sub

End Class
