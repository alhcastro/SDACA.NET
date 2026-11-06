Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports CoopeNet.Core.Licencias

Public Class LicenciaCoopepalmaresTests

    Private Const Fuentes As String = "C:\Sistemas\Fuentes"
    Private ReadOnly _lic As New LicenciaCoopepalmares()

    <Fact>
    Public Sub Datos_del_registro_iguales_a_reg_cls()
        Assert.Equal("3-004-045003", _lic.Registro.Cedula)
        Assert.Equal("3004045003", _lic.Registro.CedulaSinGuiones)
        Assert.Equal(7L, _lic.Registro.NumeroCliente)
        Assert.Equal("COOPEPALMARES R.L.", _lic.Registro.NombreCorto)
        Assert.True(_lic.Registro.Asociados)
        Assert.True(_lic.Registro.UnaEmpresa)
    End Sub

    <Fact>
    Public Sub Tiene_18_modulos_sin_SD0401()
        Assert.Equal(18, _lic.Modulos.Count)
        Assert.False(_lic.TieneModulo("SD0401"))
        Assert.Equal(_lic.Registro.ModulosCount, _lic.Modulos.Count)
        Assert.Equal(_lic.Modulos.Count, _lic.Modulos.Select(Function(m) m.Codigo).Distinct().Count())
        Assert.True(_lic.TieneModulo("sd0502"))
        Assert.False(_lic.TieneModulo("SD1901"))
    End Sub

    <Fact>
    Public Sub Es_perpetua()
        Assert.True(_lic.EstaVigente(New Date(2030, 1, 1)))
    End Sub

    <Fact>
    Public Sub Coincide_con_reg_cls_de_las_fuentes()
        Dim reg = Path.Combine(Fuentes, "registro", "07 - Coopepalmares", "reg.cls")
        If Not File.Exists(reg) Then Return ' Solo en la máquina que tiene las fuentes VB6.

        Dim texto = File.ReadAllText(reg, Windows1252())
        ' reg.cls tiene 19 módulos; SD0401 se quitó porque Coopepalmares no lo usa.
        Dim codigos = Regex.Matches(texto, "Modulos\.Codigo\s*=\s*""(SD\w+)""").Cast(Of Match)().
            Select(Function(m) m.Groups(1).Value).Where(Function(c) c <> "SD0401").ToArray()
        Assert.Equal(codigos, _lic.Modulos.Select(Function(m) m.Codigo).ToArray())
    End Sub

    <Fact>
    Public Sub La_clave_de_Init_de_cada_modulo_coincide_con_su_Clase_cls()
        If Not Directory.Exists(Fuentes) Then Return

        Dim revisados = 0
        For Each modulo In _lic.Modulos
            Dim carpeta = Directory.GetDirectories(Fuentes, modulo.Codigo & "*").FirstOrDefault()
            Dim clase = If(carpeta Is Nothing, Nothing, Path.Combine(carpeta, "Clase.cls"))
            If clase Is Nothing OrElse Not File.Exists(clase) Then Continue For

            Dim m = Regex.Match(File.ReadAllText(clase, Windows1252()), "If\s+pClave\s*=\s*""([0-9a-f]{32})""", RegexOptions.IgnoreCase)
            If Not m.Success Then Continue For

            Assert.Equal(m.Groups(1).Value, modulo.ClaveInit)
            revisados += 1
        Next
        Assert.True(revisados >= 10, $"Solo se pudieron revisar {revisados} módulos.")
    End Sub

    Private Shared Function Windows1252() As Encoding
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        Return Encoding.GetEncoding(1252)
    End Function

End Class
