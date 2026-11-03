using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SDACA.Core.Licencias;
using Xunit;

namespace SDACA.Core.Tests
{
    public class LicenciaCoopepalmaresTests
    {
        private const string Fuentes = @"C:\Sistemas\Fuentes";
        private readonly LicenciaCoopepalmares _lic = new LicenciaCoopepalmares();

        [Fact]
        public void Datos_del_registro_iguales_a_reg_cls()
        {
            Assert.Equal("3-004-045003", _lic.Registro.Cedula);
            Assert.Equal("3004045003", _lic.Registro.CedulaSinGuiones);
            Assert.Equal(7, _lic.Registro.NumeroCliente);
            Assert.Equal("COOPEPALMARES R.L.", _lic.Registro.NombreCorto);
            Assert.True(_lic.Registro.Asociados);
            Assert.True(_lic.Registro.UnaEmpresa);
        }

        [Fact]
        public void Tiene_18_modulos_sin_SD0401()
        {
            Assert.Equal(18, _lic.Modulos.Count);
            Assert.False(_lic.TieneModulo("SD0401"));
            Assert.Equal(_lic.Registro.ModulosCount, _lic.Modulos.Count);
            Assert.Equal(_lic.Modulos.Count, _lic.Modulos.Select(m => m.Codigo).Distinct().Count());
            Assert.True(_lic.TieneModulo("sd0502"));
            Assert.False(_lic.TieneModulo("SD1901"));
        }

        [Fact]
        public void Es_perpetua()
        {
            Assert.True(_lic.EstaVigente(new DateTime(2030, 1, 1)));
        }

        [Fact]
        public void Coincide_con_reg_cls_de_las_fuentes()
        {
            var reg = Path.Combine(Fuentes, "registro", "07 - Coopepalmares", "reg.cls");
            if (!File.Exists(reg)) return; // Solo en la máquina que tiene las fuentes VB6.

            var texto = File.ReadAllText(reg, Windows1252());
            // reg.cls tiene 19 módulos; SD0401 se quitó porque Coopepalmares no lo usa.
            var codigos = Regex.Matches(texto, "Modulos\\.Codigo\\s*=\\s*\"(SD\\w+)\"").Cast<Match>().Select(m => m.Groups[1].Value)
                .Where(c => c != "SD0401").ToArray();
            Assert.Equal(codigos, _lic.Modulos.Select(m => m.Codigo).ToArray());
        }

        [Fact]
        public void La_clave_de_Init_de_cada_modulo_coincide_con_su_Clase_cls()
        {
            if (!Directory.Exists(Fuentes)) return;

            var revisados = 0;
            foreach (var modulo in _lic.Modulos)
            {
                var carpeta = Directory.GetDirectories(Fuentes, modulo.Codigo + "*").FirstOrDefault();
                var clase = carpeta == null ? null : Path.Combine(carpeta, "Clase.cls");
                if (clase == null || !File.Exists(clase)) continue;

                var m = Regex.Match(File.ReadAllText(clase, Windows1252()), "If\\s+pClave\\s*=\\s*\"([0-9a-f]{32})\"", RegexOptions.IgnoreCase);
                if (!m.Success) continue;

                Assert.True(LicenciaExtensiones.ClaveValida(modulo.Codigo, m.Groups[1].Value),
                    $"{modulo.Codigo}: Clase.cls espera {m.Groups[1].Value}, MD5 da {modulo.ClaveInit}");
                revisados++;
            }
            Assert.True(revisados >= 10, $"Solo se pudieron revisar {revisados} módulos.");
        }

        private static Encoding Windows1252()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }
    }
}
