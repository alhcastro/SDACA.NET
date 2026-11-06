Imports System.Security.Cryptography
Imports CoopeNet.Core.Seguridad

Public Class Md5Vb6Tests

    <Theory>
    <InlineData("SD0601", "132a3832f7f5deaed9a9b3b6e6230470")> ' Clave de Init() en Fuentes\SD0601\Clase.cls
    <InlineData("sdaca", "879ad25da507312992a3f41baaa498a0")>  ' Contraseña MySQL predeterminada del usuario "sdaca"
    <InlineData("", "d41d8cd98f00b204e9800998ecf8427e")>
    Public Sub Coincide_con_valores_del_sistema(texto As String, esperado As String)
        Assert.Equal(esperado, Md5Vb6.Calcular(texto))
    End Sub

    <Fact>
    Public Sub Devuelve_minusculas()
        Dim h = Md5Vb6.Calcular("Contraseña")
        Assert.Equal(h.ToLowerInvariant(), h)
        Assert.Equal(32, h.Length)
    End Sub

    <Fact>
    Public Sub Usa_el_byte_bajo_de_cada_caracter_como_AscB()
        ' "ñ" es U+00F1 → byte F1 (igual a Latin-1).
        ' "€" es U+20AC → VB6 toma &HAC, no &H80 (Windows-1252) ni E2 82 AC (UTF-8).
        Assert.Equal(Esperado(&HF1), Md5Vb6.Calcular("ñ"))
        Assert.Equal(Esperado(&HAC), Md5Vb6.Calcular("€"))
    End Sub

    <Fact>
    Public Sub Hash_de_privilegios_usa_local_coma_uid()
        Assert.Equal(Md5Vb6.Calcular("1,25"), Privilegios.HashUsuario(1, 25))
        Assert.Equal(Md5Vb6.Calcular("1,25,300"), Privilegios.HashPrivilegio(1, 25, 300))
    End Sub

    Private Shared Function Esperado(ParamArray bytes As Byte()) As String
        Using algoritmo = MD5.Create()
            Return String.Concat(algoritmo.ComputeHash(bytes).Select(Function(b) b.ToString("x2")))
        End Using
    End Function

End Class
