Imports SDACA.Core.Catalogos

Public Class DatosPersonalesTests

    Private Shared Function Fisica() As Persona
        Return New Persona With {
            .Id = "999999901", .Tipo = "0", .Nombre = " ANA ", .Apellido1 = "O'NEIL", .Apellido2 = "PRUEBA", .Genero = "1",
            .FNac = #1980-02-03#, .Prov = "01", .Cant = "07", .Dist = "01", .Caserio = "02", .Dir = "UNO" & vbCrLf & "DOS",
            .Pres = "CR", .Nac = "CR", .EstCiv = "0"
        }
    End Function

    <Fact>
    Public Sub Actualizar_escapa_el_texto_y_no_toca_numloc_ni_el_creador()
        Dim p = Fisica()
        DatosPersonalesRepositorio.Normalizar(p)
        Dim sql = DatosPersonalesRepositorio.SentenciaActualizar(p)
        Assert.Contains("nombre = 'ANA'", sql)
        Assert.Contains("apellido1 = 'O\'NEIL'", sql)
        Assert.Contains("fnac = '1980-02-03'", sql)
        Assert.Contains("dir = 'UNODOS'", sql)
        Assert.EndsWith("WHERE id = '999999901'", sql)
        Assert.DoesNotContain("numloc", sql)
        Assert.DoesNotContain("uidc", sql)
    End Sub

    <Fact>
    Public Sub Insertar_deja_numloc_vacio_y_registra_el_usuario()
        Dim sql = DatosPersonalesRepositorio.SentenciaInsertar(Fisica(), 3)
        Assert.StartsWith("INSERT INTO gacdp (id, tipo, nombre,", sql)
        Assert.Contains("'1980-02-03', '', '01', '07', '01', '02'", sql)
        Assert.EndsWith("'300', '1', 3, NOW())", sql)
    End Sub

    <Fact>
    Public Sub Persona_juridica_no_lleva_apellidos_y_su_genero_es_2()
        Dim p = Fisica()
        p.Tipo = Persona.TipoJuridica
        DatosPersonalesRepositorio.Normalizar(p)
        Assert.Equal("", p.Apellido1)
        Assert.Equal("", p.Apellido2)
        Assert.Equal("2", p.Genero)
    End Sub

    <Fact>
    Public Sub Representantes_actualizan_los_existentes_y_borran_los_marcados()
        Dim lista = {
            New RepresentanteLegal With {.IdRep = "111", .Personeria = "TOMO 1", .Fecha = #2020-01-02#, .Borrar = True},
            New RepresentanteLegal With {.IdRep = "222", .Personeria = "D'A", .Fecha = #2021-03-04#}
        }
        Dim sql = DatosPersonalesRepositorio.SentenciasRepresentantes("3101", lista, 3)
        Assert.Equal(2, sql.Count)
        Assert.Equal("DELETE FROM gacdprepresentante2 WHERE id = '3101' AND idrep NOT IN ('222')", sql(0))
        Assert.Contains("VALUES ('3101', '222', 1, 'D\'A', '2021-03-04', 3, NOW())", sql(1))
        Assert.EndsWith("ON DUPLICATE KEY UPDATE orden = 1, numpersoneria = 'D\'A', fechapersoneria = '2021-03-04'", sql(1))
    End Sub

    <Fact>
    Public Sub Sin_representantes_vigentes_se_borran_todos()
        Dim lista = {New RepresentanteLegal With {.IdRep = "111", .Borrar = True}}
        Dim sql = DatosPersonalesRepositorio.SentenciasRepresentantes("3101", lista, 3)
        Assert.Equal({"DELETE FROM gacdprepresentante2 WHERE id = '3101'"}, sql)
    End Sub

    <Fact>
    Public Sub Contactos_ignoran_filas_sin_dato_y_numeran_las_demas()
        Dim lista = {
            New ContactoPersona With {.Tipo = 3, .Dato = " 8888-0000 "},
            New ContactoPersona With {.Tipo = 1, .Dato = "  "},
            New ContactoPersona With {.Tipo = 4, .Dato = "a@b.cr"}
        }
        Dim sql = DatosPersonalesRepositorio.SentenciasContactos("999", lista, 3)
        Assert.Equal("DELETE FROM gacdptel WHERE id = '999'", sql(0))
        Assert.Equal("INSERT INTO gacdptel (id, pos, tipo, dato, uidc, fc) VALUES ('999', 1, 3, '8888-0000', 3, NOW()), ('999', 2, 4, 'a@b.cr', 3, NOW())", sql(1))
    End Sub

    <Fact>
    Public Sub Sin_contactos_solo_se_borra()
        Assert.Single(DatosPersonalesRepositorio.SentenciasContactos("999", Array.Empty(Of ContactoPersona)(), 3))
    End Sub

    <Theory>
    <InlineData("", "")>
    <InlineData(" ab ", "")>
    <InlineData("ana", "%ana%")>
    <InlineData(" ana  maria ", "%ana%%maria%")>
    Public Sub Filtro_pide_tres_caracteres_y_los_espacios_valen_por_cualquier_texto(filtro As String, esperado As String)
        Assert.Equal(esperado, DatosPersonalesRepositorio.PatronFiltro(filtro))
    End Sub

    <Fact>
    Public Sub Identificacion_se_valida_por_el_tamano_del_tipo()
        Dim tipo As New TipoIdentificacion With {.Detalle = "DIMEX", .Formato = "############", .Minimo = 11, .Maximo = 12}
        Assert.Equal("", DatosPersonalesRepositorio.ValidarIdentificacion("12345678901", tipo))
        Assert.Contains("tiene 3", DatosPersonalesRepositorio.ValidarIdentificacion("123", tipo))
        Assert.NotEqual("", DatosPersonalesRepositorio.ValidarIdentificacion("123", Nothing))
    End Sub

    <Fact>
    Public Sub Solo_los_formatos_de_numeral_exigen_digitos()
        Assert.True(New TipoIdentificacion With {.Formato = "#########"}.SoloDigitos)
        Assert.False(New TipoIdentificacion With {.Formato = "&&&&&&&&&&"}.SoloDigitos)
    End Sub

    <Fact>
    Public Sub Fechas_en_cero_se_muestran_como_1900()
        Dim p As New Persona With {.FNac = Date.MinValue, .FDef = Date.MinValue}
        DatosPersonalesRepositorio.NormalizarFechas(p)
        Assert.Equal(Persona.SinFecha, p.FNac)
        Assert.False(p.Fallecido)
    End Sub

    <Fact>
    Public Sub Codigos_se_rellenan_a_cuatro_digitos_y_replican_el_local()
        Assert.Equal("0007", DatosPersonalesRepositorio.RellenarCodigo(7))
        Assert.Equal("12345", DatosPersonalesRepositorio.RellenarCodigo(12345))
        Assert.Equal("INSERT INTO gaccodclientes (LOCAL, ID, CODIGO, UIDC, FE, FC) VALUES (7, '999', 'PRO0001', 3, NOW(), NOW())",
                     DatosPersonalesRepositorio.SentenciaAgregarCodigo("999", "PRO0001", 3, 7))
        Assert.Equal("DELETE FROM gaccodclientes WHERE ID = '999' AND CODIGO = 'PRO0001'", DatosPersonalesRepositorio.SentenciaEliminarCodigo("999", "PRO0001"))
    End Sub

End Class
