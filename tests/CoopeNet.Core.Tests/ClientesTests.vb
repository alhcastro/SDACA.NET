Imports CoopeNet.Core.Pos

Public Class ClientesTests

    Private Shared Function Ejemplo() As ClientePos
        Return New ClientePos With {
            .Cliente = "3101008166", .PrecioFac = 3, .Exonerado = ExoneracionCliente.NoSujeto, .Vendedor = "204560965", .Descuento = 2.5,
            .NumCliente = "CCSS-12", .Zona = 4, .Horario = "L-V 8 a 5", .Visita = "Martes", .Contacto1 = "111", .Cargo1 = "Compras",
            .Contacto2 = "", .Cargo2 = "", .Notas = "Paga a 30 días", .Iva = 13, .ActividadEconomica = " 9529.0 "}
    End Function

    <Fact>
    Public Sub Insertar_es_el_INSERT_de_SD_Guardar()
        Assert.Equal("INSERT INTO posclientes(Local, Cliente, PrecioFac, Exonerado, Vendedor, Descuento, NumCliente, Zona, Horario, Visita, " &
                     "Contacto1, Cargo1, Contacto2, Cargo2, Notas, IVA, ActividadEconomica) VALUES(" &
                     "7, '3101008166', 3, '2', '204560965', 2.5, 'CCSS-12', 4, 'L-V 8 a 5', 'Martes', '111', 'Compras', '', '', 'Paga a 30 días', 13, '9529.0')",
                     ClientesRepositorio.SentenciaInsertar(Ejemplo(), 7))
    End Sub

    <Fact>
    Public Sub Actualizar_filtra_local_y_cliente()
        Assert.Equal("UPDATE posclientes SET PrecioFac=3, Exonerado='2', Vendedor='204560965', Descuento=2.5, NumCliente='CCSS-12', Zona=4, " &
                     "Horario='L-V 8 a 5', Visita='Martes', Contacto1='111', Cargo1='Compras', Contacto2='', Cargo2='', Notas='Paga a 30 días', " &
                     "IVA=13, ActividadEconomica='9529.0' WHERE local=7 AND Cliente='3101008166'",
                     ClientesRepositorio.SentenciaActualizar(Ejemplo(), 7))
    End Sub

    <Fact>
    Public Sub El_descuento_maximo_es_menos_uno_y_las_comillas_se_escapan()
        Dim c = Ejemplo()
        c.Descuento = ClientesRepositorio.DescuentoMaximo
        c.Notas = "O'Brien \ cía"
        Dim sql = ClientesRepositorio.SentenciaActualizar(c, 1)
        Assert.Contains("Descuento=-1,", sql)
        Assert.Contains("Notas='O\'Brien \\ cía'", sql)
    End Sub

    <Fact>
    Public Sub Borrar_solo_toca_el_local()
        Assert.Equal("DELETE FROM posclientes WHERE local=7 AND Cliente='3101008166'", ClientesRepositorio.SentenciaBorrar("3101008166", 7))
    End Sub

End Class
