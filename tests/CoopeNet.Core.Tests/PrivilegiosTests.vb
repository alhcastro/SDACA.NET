Imports CoopeNet.Core.Seguridad

Public Class PrivilegiosTests

    Private Shared ReadOnly Usuario As ISet(Of String) = New HashSet(Of String)({"6000", "6003", "13000"}, StringComparer.OrdinalIgnoreCase)

    <Theory>
    <InlineData("6000", True)>
    <InlineData("6001", False)>
    <InlineData("6001;6003", True)>   ' OR: basta uno
    <InlineData("6001;6002", False)>
    <InlineData("6003;", True)>
    <InlineData("", False)>
    <InlineData(Nothing, False)>
    Public Sub Validar_con_OR_como_SD_ValidarPriv(requeridos As String, esperado As Boolean)
        Assert.Equal(esperado, Privilegios.Validar(Usuario, requeridos))
    End Sub

    <Theory>
    <InlineData("6000;6003", True)>   ' AND: hacen falta todos
    <InlineData("6000;6001", False)>
    <InlineData("6001;6000", False)>
    Public Sub Validar_con_AND(requeridos As String, esperado As Boolean)
        Assert.Equal(esperado, Privilegios.Validar(Usuario, requeridos, ";", cualquiera:=False))
    End Sub

    <Fact>
    Public Sub Validar_con_separador_distinto()
        Assert.True(Privilegios.Validar(Usuario, "1|13000", "|"))
    End Sub

    <Theory>
    <InlineData("Administrador", True)>
    <InlineData("ROOT", True)>
    <InlineData("mrodriguez", False)>
    Public Sub Administrador_y_root_reciben_todos(login As String, esperado As Boolean)
        Assert.Equal(esperado, Privilegios.EsAdministrador(login))
    End Sub

End Class
