using System.Linq;
using System.Security.Cryptography;
using SDACA.Core.Seguridad;
using Xunit;

namespace SDACA.Core.Tests
{
    public class Md5Vb6Tests
    {
        [Theory]
        // Clave de Init() en Fuentes\SD0601\Clase.cls
        [InlineData("SD0601", "132a3832f7f5deaed9a9b3b6e6230470")]
        // Contraseña MySQL predeterminada del usuario "sdaca"
        [InlineData("sdaca", "879ad25da507312992a3f41baaa498a0")]
        [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
        public void Coincide_con_valores_del_sistema(string texto, string esperado)
        {
            Assert.Equal(esperado, Md5Vb6.Calcular(texto));
        }

        [Fact]
        public void Devuelve_minusculas()
        {
            var h = Md5Vb6.Calcular("Contraseña");
            Assert.Equal(h.ToLowerInvariant(), h);
            Assert.Equal(32, h.Length);
        }

        [Fact]
        public void Usa_el_byte_bajo_de_cada_caracter_como_AscB()
        {
            // "ñ" es U+00F1 → byte F1 (igual a Latin-1).
            // "€" es U+20AC → VB6 toma 0xAC, no 0x80 (Windows-1252) ni E2 82 AC (UTF-8).
            Assert.Equal(Esperado(0xF1), Md5Vb6.Calcular("ñ"));
            Assert.Equal(Esperado(0xAC), Md5Vb6.Calcular("€"));
        }

        [Fact]
        public void Hash_de_privilegios_usa_local_coma_uid()
        {
            Assert.Equal(Md5Vb6.Calcular("1,25"), Privilegios.HashUsuario(1, 25));
            Assert.Equal(Md5Vb6.Calcular("1,25,300"), Privilegios.HashPrivilegio(1, 25, 300));
        }

        private static string Esperado(params byte[] bytes)
        {
            using var md5 = MD5.Create();
            return string.Concat(md5.ComputeHash(bytes).Select(b => b.ToString("x2")));
        }
    }
}
