using System.Linq;
using SDACA.Core.Configuracion;
using Xunit;

namespace SDACA.Core.Tests
{
    public class ConfiguracionTests
    {
        private const string Ini =
            "[CP.LOCAL]\r\nCATALOGO=sdprincipal\r\nPUERTO= 3306\r\nSERVIDOR=130.0.0.2\r\nUSUARIO=Administrador\r\nUSUARIO0=root\r\nUSUARIO1=mrodriguez\r\nUSUARIO2=Administrador\r\nUSER::ROOT= 0\r\n" +
            "[GENERAL]\r\nDEFAULT=CP DB SERVER PRUEBAS\r\nCNN=Provider=MSDASQL.1;Password=secreto\r\n" +
            "[CP DB SERVER PRUEBAS]\r\nCATALOGO=pruebas\r\nPUERTO=\r\nSERVIDOR=130.0.0.2\r\n";

        [Fact]
        public void Lee_perfiles_y_omite_GENERAL()
        {
            var p = ConexionesIni.Parsear(Ini);
            Assert.Equal(new[] { "CP.LOCAL", "CP DB SERVER PRUEBAS" }, p.Select(x => x.Nombre).ToArray());
            Assert.Equal("sdprincipal", p[0].Catalogo);
            Assert.Equal(3306u, p[0].Puerto);
            Assert.Equal("Administrador", p[0].UltimoUsuario);
            Assert.Equal(new[] { "root", "mrodriguez", "Administrador" }, p[0].Usuarios.ToArray());
            Assert.Equal(3306u, p[1].Puerto);
        }

        [Fact]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void Secretos_ida_y_vuelta()
        {
            var cifrado = Secretos.Proteger("Café-2024");
            Assert.StartsWith("dpapi:", cifrado);
            Assert.DoesNotContain("Café", cifrado);
            Assert.Equal("Café-2024", Secretos.Desproteger(cifrado));
        }

        [Theory]
        [InlineData("12abc", 12)]
        [InlineData("abc", 0)]
        [InlineData(" 3 4", 34)]
        [InlineData("-7", -7)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        public void Val_como_vb6(string? texto, long esperado)
        {
            Assert.Equal(esperado, Vb6.Val(texto));
        }
    }
}
