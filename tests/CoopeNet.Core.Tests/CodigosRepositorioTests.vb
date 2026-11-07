Imports CoopeNet.Core.Catalogos

Public Class CodigosRepositorioTests

    <Fact>
    Public Sub Tipo_nuevo_graba_el_usuario_y_escapa_el_texto()
        Dim tipo As New TipoCodigo With {.Tipo = "ABC", .Descripcion = "O'TIPO"}
        Assert.Equal("INSERT INTO gactiposcodigos (TIPO, DESCRIPCION, SISTEMA, ACTIVO, UIDC, FE, FC) VALUES ('ABC', 'O\'TIPO', '0', '1', 3, NOW(), NOW())", CodigosRepositorio.SentenciaNuevoTipo(tipo, 3))
    End Sub

    <Fact>
    Public Sub Editar_un_tipo_no_cambia_el_campo_sistema()
        Dim tipo As New TipoCodigo With {.Tipo = "ABC", .Descripcion = "TIPO", .Sistema = TipoCodigo.Si, .Activo = TipoCodigo.No}
        Dim sql = CodigosRepositorio.SentenciaEditarTipo(tipo)
        Assert.Equal("UPDATE gactiposcodigos SET DESCRIPCION = 'TIPO', ACTIVO = '0' WHERE TIPO = 'ABC'", sql)
        Assert.DoesNotContain("SISTEMA", sql)
    End Sub

    <Theory>
    <InlineData(IrATipo.Primero, "MIN(TIPO)", False)>
    <InlineData(IrATipo.Anterior, "TIPO < @tipo ORDER BY TIPO DESC", True)>
    <InlineData(IrATipo.Siguiente, "TIPO > @tipo ORDER BY TIPO LIMIT", True)>
    <InlineData(IrATipo.Ultimo, "MAX(TIPO)", False)>
    Public Sub Navegacion_por_tipos(destino As IrATipo, esperado As String, usaTipo As Boolean)
        Dim sql = CodigosRepositorio.SqlIrA(destino)
        Assert.Contains(esperado, sql)
        Assert.Equal(usaTipo, sql.Contains("@tipo"))
    End Sub

    <Fact>
    Public Async Function Borrar_un_tipo_vacio_no_toca_la_base() As Task
        ' Sin fábrica: si intentara conectarse fallaría. Un LEFT(CODIGO, 0) contaría cualquier código.
        Dim repositorio As New CodigosRepositorio(Nothing)
        Assert.False(Await repositorio.BorrarTipoAsync(""))
    End Function

    <Fact>
    Public Sub La_busqueda_por_coincidencias_solo_toma_asociados_activos()
        Assert.Contains("A.ACTIVO = '1' AND A.CODIGO LIKE @patron", CodigosRepositorio.SqlCoincidencias)
    End Sub

    <Fact>
    Public Sub El_codigo_exacto_usa_la_funcion_con_el_local_de_VB6()
        Assert.Equal("SELECT gac_fun_asociados(0, @clave)", CodigosRepositorio.SqlCodigoExacto)
    End Sub

    <Fact>
    Public Sub La_primera_columna_de_la_busqueda_es_la_identificacion()
        Assert.StartsWith("SELECT C.ID AS 'Identificación'", CodigosRepositorio.SqlCoincidencias)
        Using tabla = CodigosRepositorio.TablaDeIdentificacion("999999901")
            Assert.Equal(1, tabla.Rows.Count)
            Assert.Equal("999999901", tabla.Rows(0)(0))
        End Using
    End Sub

    <Theory>
    <InlineData("Estado", "INACTIVO", "         ESTADO: INACTIVO")>
    <InlineData("Correo electronico", "x", "CORREO ELECTRON: x")>
    Public Sub Linea_de_detalle_alinea_la_etiqueta(etiqueta As String, texto As String, esperado As String)
        Assert.Equal(esperado, CodigosRepositorio.LineaDetalle(etiqueta, texto))
    End Sub

    <Fact>
    Public Sub Fecha_del_detalle_en_espanol()
        Assert.Equal("06 de noviembre de 2026", CodigosRepositorio.FechaDetalle(#2026-11-06#))
    End Sub

End Class
