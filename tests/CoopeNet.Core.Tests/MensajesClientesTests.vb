Imports CoopeNet.Core.Pos

Public Class MensajesClientesTests

    <Fact>
    Public Sub Guardar_es_el_REPLACE_de_SD_Guardar_con_Local_para_la_replica()
        Dim m As New MensajeCliente With {.Codigo = 26, .Titulo1 = "Coopepalmares le da la Bienvenida", .Titulo2 = "¡Gracias por su visita!", .NombreLocal = "Palmares"}
        Assert.Equal("REPLACE INTO posmensajes (Codigo, titulo1, titulo2, titulo3, Nlocal, local) VALUES (" &
                     "'26', 'Coopepalmares le da la Bienvenida', '¡Gracias por su visita!', '', 'Palmares', $Local)",
                     MensajesClientesRepositorio.SentenciaGuardar(m))
    End Sub

    <Fact>
    Public Sub Un_mensaje_nuevo_va_con_codigo_cero_para_el_AUTO_INCREMENT()
        Assert.StartsWith("REPLACE INTO posmensajes (Codigo, titulo1, titulo2, titulo3, Nlocal, local) VALUES ('0', ",
                          MensajesClientesRepositorio.SentenciaGuardar(New MensajeCliente()))
    End Sub

    <Fact>
    Public Sub Escapa_comillas_y_recorta_a_255()
        Dim m As New MensajeCliente With {.Titulo1 = "  Café de O'Higgins  ", .Titulo3 = New String("x"c, 300)}
        Dim sql = MensajesClientesRepositorio.SentenciaGuardar(m)
        Assert.Contains("'Café de O\'Higgins'", sql)
        Assert.Contains($"'{New String("x"c, 255)}'", sql)
        Assert.DoesNotContain(New String("x"c, 256), sql)
    End Sub

    <Fact>
    Public Sub Borrar_no_mira_el_local_y_la_imagen_usa_el_codigo_como_id()
        Assert.Equal("DELETE FROM posmensajes WHERE codigo = 26", MensajesClientesRepositorio.SentenciaBorrar(26))
        Assert.Equal("DELETE FROM posimagenes WHERE id = '26' AND LOCAL = $Local", MensajesClientesRepositorio.SentenciaBorrarImagen(26))
    End Sub

    <Theory>
    <InlineData(0L, 1)>
    <InlineData(2L, 2)>
    <InlineData(10L, 10)>
    <InlineData(25L, 10)>
    Public Sub La_velocidad_queda_dentro_del_control(valor As Long, esperado As Integer)
        Assert.Equal(esperado, MensajesClientesRepositorio.AjustarVelocidad(valor))
    End Sub

End Class
