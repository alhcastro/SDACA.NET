Imports CoopeNet.Core.Datos

Public Class ConsultasGuardadasTests

    <Theory>
    <InlineData("select * from posarticulos;", "select * from posarticulos")>
    <InlineData("  select 1 ;;  ", "select 1")>
    <InlineData("select ';' as x, `a;b` from t", "select ';' as x, `a;b` from t")>
    <InlineData("select 'it\'s;' from t", "select 'it\'s;' from t")>
    <InlineData("select 1 -- ; comentario" & vbLf & "from t", "select 1 -- ; comentario" & vbLf & "from t")>
    <InlineData("select /* ; */ 1", "select /* ; */ 1")>
    Public Sub Acepta_una_sola_sentencia(sql As String, esperado As String)
        Assert.Equal(esperado, ConsultasGuardadasRepositorio.UnaSentencia(sql))
    End Sub

    <Theory>
    <InlineData("select 1; drop table posarticulos")>
    <InlineData("select 1); delete from t; select (1")>
    <InlineData("")>
    <InlineData(" ; ")>
    Public Sub Rechaza_varias_sentencias_o_vacia(sql As String)
        Assert.Throws(Of ArgumentException)(Function() ConsultasGuardadasRepositorio.UnaSentencia(sql))
    End Sub

    <Fact>
    Public Sub Envuelve_con_el_local_el_filtro_y_el_limite()
        Dim sql = ConsultasGuardadasRepositorio.Envolver("select codigo from posrequisic where local=$Local;", 7, "Có`digo", 1000)
        Assert.Equal("SELECT * FROM (" & vbLf & "select codigo from posrequisic where local=7" & vbLf & ") AS consulta WHERE `Có``digo` LIKE @patron LIMIT 1000", sql)
        Assert.Equal("SELECT * FROM (" & vbLf & "select 1" & vbLf & ") AS consulta", ConsultasGuardadasRepositorio.Envolver("select 1", 1, "", Nothing))
    End Sub

    <Fact>
    Public Sub Guardar_es_el_REPLACE_de_VB6_con_comillas_escapadas()
        Assert.Equal("REPLACE INTO posconsultas VALUES(29, 'NUEVA', 'select * from t where a=\'x\' and l=$Local', '', NOW(), '1')",
                     ConsultasGuardadasRepositorio.SentenciaGuardar(29, " NUEVA ", "select * from t where a='x' and l=$Local "))
    End Sub

End Class
