Imports CoopeNet.Core.Pos

Public Class ArticulosRepositorioTests

    <Fact>
    Public Sub El_codigo_nuevo_es_el_primer_hueco_desde_10000()
        Assert.Equal(10000L, ArticulosRepositorio.PrimerLibre({1L, 5L, 9999L}))
        Assert.Equal(10002L, ArticulosRepositorio.PrimerLibre({10000L, 10001L, 10003L, 10001L}))
        Assert.Equal(10003L, ArticulosRepositorio.PrimerLibre({10002L, 10000L, 10001L}))
    End Sub

    <Theory>
    <InlineData("2600120012345", "12")>
    <InlineData("2812340045678", "1234")>
    <InlineData("26001", "26001")>
    <InlineData("7441234567890", "7441234567890")>
    Public Sub Las_etiquetas_de_balanza_traen_el_codigo_en_medio(codigo As String, esperado As String)
        Assert.Equal(esperado, ArticulosRepositorio.CodigoDeEtiqueta(codigo))
    End Sub

    <Fact>
    Public Sub El_filtro_de_categorias_omite_los_niveles_en_cero()
        Assert.Equal("", ArticulosRepositorio.FiltroCategorias(0, 0, 0))
        Assert.Equal("I.Categ = 3 AND I.SCateg = 2", ArticulosRepositorio.FiltroCategorias(3, 2, 0))
    End Sub

    <Fact>
    Public Sub Siguiente_y_anterior_comparan_el_codigo_como_texto()
        Assert.EndsWith("WHERE A.CodigoArt > @codigo AND I.Categ = 1 ORDER BY A.CodigoArt LIMIT 1", ArticulosRepositorio.SqlVecino(True, "I.Categ = 1"))
        Assert.EndsWith("WHERE A.CodigoArt < @codigo ORDER BY A.CodigoArt DESC LIMIT 1", ArticulosRepositorio.SqlVecino(False, ""))
    End Sub

    <Fact>
    Public Sub El_buscador_filtra_las_categorias_en_el_where()
        Dim sql = ArticulosRepositorio.SqlBuscar("I.Categ = 1")
        Assert.Contains("F.articulo = L.CArticulo WHERE I.Categ = 1 ORDER BY", sql)
        Assert.DoesNotContain("WHERE", ArticulosRepositorio.SqlBuscar(""))
    End Sub

    <Fact>
    Public Sub Guardar_tiene_tantos_valores_como_columnas_y_escapa_el_texto()
        Dim a As New Articulo With {.CodigoArt = "10001", .Descripcion = "O'BRIEN \ X", .Moneda = "¢", .FC = #2026-01-02 03:04:05#, .Precio1 = 1234.5678}
        Dim sql = ArticulosRepositorio.SentenciaGuardar(a)
        Dim columnas = sql.Substring(sql.IndexOf("("c) + 1, sql.IndexOf(")"c) - sql.IndexOf("("c) - 1).Split(","c).Length
        Dim valores = sql.Substring(sql.IndexOf("VALUES (", StringComparison.Ordinal) + 8).TrimEnd(")"c)
        Dim sinTextos = System.Text.RegularExpressions.Regex.Replace(valores, "'(\\.|[^'\\])*'", "x")
        Assert.Equal(57, columnas)
        Assert.Equal(57, sinTextos.Split(","c).Length)
        Assert.Contains("'O\'BRIEN \\ X'", sql)
        Assert.Contains("'2026-01-02 03:04:05', NOW()", sql)
        Assert.Contains("1234.5678", sql)
        Assert.Contains("'¢'", sql)
        Assert.StartsWith("'10001', ''", valores)
    End Sub

    <Fact>
    Public Sub Las_altas_del_local_dejan_el_local_para_la_replica()
        Assert.Contains("VALUES ($Local, '10001', 0, 0, 0, 0, NOW(), NOW(), '5', 0, NOW(), 0, 1, 0)", ArticulosRepositorio.SentenciaAltaLocal("10001", 5))
        Assert.Contains("VALUES ($Local, '10001', 0, 0, 0, 0, 1, 2, 3, NOW()", ArticulosRepositorio.SentenciaAltaInventario("10001", 1, 2, 3, 5))
        Assert.Equal("UPDATE posinventario SET Categ = 1, SCateg = 2, SSCateg = 3, FM = NOW() WHERE CLocal = $Local AND CArticulo = '10001'", ArticulosRepositorio.SentenciaCategorias("10001", 1, 2, 3))
    End Sub

    <Fact>
    Public Sub Comparar_lista_solo_los_campos_que_cambiaron()
        Dim antes As New Dictionary(Of String, String) From {{"Precio1", "10"}, {"Descripcion", "A "}}
        Dim ahora As New Dictionary(Of String, String) From {{"Precio1", "12"}, {"Descripcion", "A"}, {"Nuevo", "x"}}
        Dim cambios = ArticulosRepositorio.Comparar(antes, ahora)
        Dim c = Assert.Single(cambios)
        Assert.Equal("Precio1", c.Campo)
        Assert.Equal("10", c.Anterior)
        Assert.Empty(ArticulosRepositorio.Comparar(Nothing, ahora))
    End Sub

    <Theory>
    <InlineData(True, "0", "0", True)>
    <InlineData(False, "1", "0", True)>
    <InlineData(False, "0", "1", True)>
    <InlineData(False, "0", "0", False)>
    Public Sub Solo_se_muestra_si_esta_en_el_local_o_es_servicio_o_precio_libre(registrado As Boolean, servicio As String, sinPrecio As String, esperado As Boolean)
        Assert.Equal(esperado, ArticulosRepositorio.SePuedeMostrar(New Articulo With {.Registrado = registrado, .Servicio = servicio, .SinPrecio = sinPrecio}))
    End Sub

End Class
