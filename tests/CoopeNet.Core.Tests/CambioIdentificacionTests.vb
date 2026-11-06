Imports CoopeNet.Core.Catalogos

Public Class CambioIdentificacionTests

    <Fact>
    Public Sub Sin_condicion_cambia_la_columna_y_escapa_las_identificaciones()
        Dim fila As New TablaCambioId With {.Tabla = "gacdp", .Campo = "id"}
        Assert.Equal("UPDATE IGNORE gacdp SET id = 'B\'2' WHERE id = 'A\\1'", CambioIdentificacionRepositorio.Sentencia(fila, "A\1", "B'2"))
    End Sub

    <Theory>
    <InlineData(Nothing)>
    <InlineData("")>
    <InlineData("   ")>
    Public Sub Una_condicion_vacia_no_agrega_nada(condicion As String)
        Dim fila As New TablaCambioId With {.Tabla = "t", .Campo = "c", .Condicion = condicion}
        Assert.Equal("UPDATE IGNORE t SET c = 'N' WHERE c = 'V'", CambioIdentificacionRepositorio.Sentencia(fila, "V", "N"))
    End Sub

    <Fact>
    Public Sub La_condicion_de_gatcambioid_se_agrega_al_where()
        Dim fila As New TablaCambioId With {.Tabla = "t", .Campo = "c", .Condicion = "tipo = 1"}
        Assert.Equal("UPDATE IGNORE t SET c = 'N' WHERE c = 'V' AND tipo = 1", CambioIdentificacionRepositorio.Sentencia(fila, "V", "N"))
    End Sub

    <Fact>
    Public Sub El_resumen_cuenta_los_exitos_y_no_los_errores()
        Dim resultados = {
            New ResultadoCambioId With {.Tabla = "a"},
            New ResultadoCambioId With {.Tabla = "b"},
            New ResultadoCambioId With {.Tabla = "c", .Estado = EstadoCambioId.NoExiste},
            New ResultadoCambioId With {.Tabla = "d", .Estado = EstadoCambioId.Fallo}
        }
        Assert.Equal("2 de 4 fueron realizados exitosamente.", CambioIdentificacionRepositorio.Resumen(resultados))
    End Sub

    <Theory>
    <InlineData(EstadoCambioId.Actualizada, "Se actualizó correctamente t...")>
    <InlineData(EstadoCambioId.NoExiste, "No se encontró en t...")>
    <InlineData(EstadoCambioId.Fallo, "Error en la tabla t...")>
    Public Sub Mensajes_de_cada_tabla(estado As EstadoCambioId, esperado As String)
        Assert.Equal(esperado, New ResultadoCambioId With {.Tabla = "t", .Estado = estado}.Mensaje)
    End Sub

    <Fact>
    Public Sub La_descripcion_solo_menciona_el_alias_si_lo_hay()
        Dim p As New Persona With {.Nombre = "ANA", .Apellido1 = "UNO", .Apellido2 = "DOS", .FNac = #1980-02-03#}
        Assert.StartsWith("Pertenece a: UNO DOS ANA" & Environment.NewLine, CambioIdentificacionRepositorio.Descripcion(p))
        p.Alias = "ANITA"
        Assert.StartsWith("Pertenece a: UNO DOS ANA, conocido como ANITA" & Environment.NewLine, CambioIdentificacionRepositorio.Descripcion(p))
    End Sub

End Class

Public Class FallecidosTests

    Private Shared Function Persona1() As Persona
        Return New Persona With {.Id = "999999901", .Nombre = "ana", .Apellido1 = "UNO", .Apellido2 = "DOS", .Genero = "1", .FNac = #1980-01-03#}
    End Function

    <Fact>
    Public Sub Registrar_marca_la_fecha_e_inactiva()
        Assert.Equal("UPDATE gacdp SET fdef = '2026-11-05', activo = '0' WHERE id = 'A\'1'", DatosPersonalesRepositorio.SentenciaFallecimiento("A'1", #2026-11-05#))
    End Sub

    <Fact>
    Public Sub Anular_vuelve_a_la_fecha_vacia_y_activa()
        Assert.Equal("UPDATE gacdp SET fdef = '1900-01-01', activo = '1' WHERE id = 'A1'", DatosPersonalesRepositorio.SentenciaAnularFallecimiento("A1"))
    End Sub

    <Fact>
    Public Sub La_ficha_de_una_persona_viva_no_trae_defuncion()
        Dim ficha = DatosPersonalesRepositorio.FichaFallecido(Persona1(), "SOLTERA")
        Assert.Contains("Nombre: UNO DOS ANA", ficha)
        Assert.Contains("Género: FEMENINO", ficha)
        Assert.Contains("Estado civil: SOLTERA", ficha)
        Assert.Contains("Estado: ACTIVO", ficha)
        Assert.Contains("Fecha de nacimiento: 03/ENE/1980", ficha)
        Assert.DoesNotContain("defunción", ficha)
    End Sub

    <Fact>
    Public Sub La_ficha_de_una_persona_fallecida_trae_la_fecha_y_el_alias()
        Dim p = Persona1()
        p.Alias = "ani"
        p.Activo = "0"
        p.FDef = #2026-11-05#
        Dim ficha = DatosPersonalesRepositorio.FichaFallecido(p, "")
        Assert.Contains("Nombre: UNO DOS ANA (CC:ANI)", ficha)
        Assert.Contains("Estado: BLOQUEADO", ficha)
        Assert.Contains("Fecha de defunción: 05/NOV/2026", ficha)
    End Sub

    <Theory>
    <InlineData("0", "MASCULINO")>
    <InlineData("", "MASCULINO")>
    <InlineData("1", "FEMENINO")>
    <InlineData("2", "JURIDICA")>
    Public Sub Genero_de_la_ficha(genero As String, esperado As String)
        Dim p = Persona1()
        p.Genero = genero
        Assert.Contains($"Género: {esperado}", DatosPersonalesRepositorio.FichaFallecido(p, ""))
    End Sub

End Class
