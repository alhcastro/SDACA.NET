Imports Dapper
Imports CoopeNet.Core.Catalogos

Public Class PadronRepositorioTests

    Private Shared Function Armar(c As CriterioPadron, ByRef parametros As DynamicParameters) As String
        parametros = New DynamicParameters()
        Return PadronRepositorio.Sentencia(c, parametros)
    End Function

    <Fact>
    Public Sub Sin_criterio_no_hay_consulta()
        Dim p As DynamicParameters = Nothing
        Assert.Equal("", Armar(New CriterioPadron With {.Apellido1 = "  ", .Cedula = "-"}, p))
        Assert.Equal("", Armar(New CriterioPadron With {.Juridico = True, .Apellido1 = "PEREZ"}, p))
    End Sub

    <Fact>
    Public Sub La_cedula_manda_y_busca_por_el_inicio_sin_guiones()
        Dim p As DynamicParameters = Nothing
        Dim sql = Armar(New CriterioPadron With {.Cedula = "1-0234-05", .Apellido1 = "PEREZ", .Nombre = "ANA"}, p)
        Assert.Contains("WHERE Cedula LIKE @cedula ORDER BY Apellido1, Apellido2, Nombre LIMIT 501", sql)
        Assert.Equal("1023405%", p.Get(Of String)("cedula"))
        Assert.DoesNotContain("@nombre", sql)
    End Sub

    <Fact>
    Public Sub Los_apellidos_son_exactos_y_el_nombre_en_cualquier_parte()
        Dim p As DynamicParameters = Nothing
        Dim sql = Armar(New CriterioPadron With {.Apellido1 = " PEREZ ", .Apellido2 = "O'NEIL", .Nombre = "ANA"}, p)
        Assert.Contains("FROM padron.padron WHERE Apellido1 = @apellido1 AND Apellido2 = @apellido2 AND Nombre LIKE @nombre", sql)
        Assert.Equal("PEREZ", p.Get(Of String)("apellido1"))
        Assert.Equal("O'NEIL", p.Get(Of String)("apellido2"))
        Assert.Equal("%ANA%", p.Get(Of String)("nombre"))
    End Sub

    <Fact>
    Public Sub El_juridico_busca_por_nombre_en_padronj()
        Dim p As DynamicParameters = Nothing
        Dim sql = Armar(New CriterioPadron With {.Juridico = True, .Nombre = "COOPE"}, p)
        Assert.Equal("SELECT Cedula AS 'Cédula', Nombre FROM padron.padronj WHERE Nombre LIKE @nombre ORDER BY Nombre LIMIT 501", sql)
    End Sub

    <Fact>
    Public Sub Buscar_sin_criterio_no_toca_la_base()
        Dim repositorio As New PadronRepositorio(Nothing)
        Dim resultado = repositorio.BuscarAsync(New CriterioPadron()).GetAwaiter().GetResult()
        Assert.Null(resultado.Tabla)
        Assert.False(resultado.Excedido)
    End Sub

End Class
