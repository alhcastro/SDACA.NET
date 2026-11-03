using MySqlConnector;
using SDACA.Core.Datos;
using SDACA.Core.Seguridad;
using Xunit;

namespace SDACA.Core.Tests
{
    public class CadenaConexionTests
    {
        [Theory]
        [InlineData("Administrador")]
        [InlineData("ADMINISTRADOR")]
        [InlineData("administrador")]
        public void Administrador_entra_como_root_con_la_contrasena_tal_cual(string usuario)
        {
            var c = CadenaConexion.ResolverCredenciales(usuario, "clave");
            Assert.Equal("root", c.UsuarioMySql);
            Assert.Equal("clave", c.Contrasena);
        }

        [Fact]
        public void Sin_usuario_usa_sdaca_con_md5_de_sdaca()
        {
            var c = CadenaConexion.ResolverCredenciales("", "");
            Assert.Equal("sdaca", c.UsuarioMySql);
            Assert.Equal("879ad25da507312992a3f41baaa498a0", c.Contrasena);
        }

        [Fact]
        public void Usuario_normal_recibe_md5_de_su_contrasena()
        {
            var c = CadenaConexion.ResolverCredenciales("mrodriguez", "Café2024");
            Assert.Equal("mrodriguez", c.UsuarioMySql);
            Assert.Equal(Md5Vb6.Calcular("Café2024"), c.Contrasena);
        }

        [Fact]
        public void Contrasena_de_32_o_mas_caracteres_se_toma_como_hash_ya_calculado()
        {
            var hash = Md5Vb6.Calcular("x");
            Assert.Equal(hash, CadenaConexion.ResolverCredenciales("nuria", hash).Contrasena);
        }

        [Fact]
        public void Usuario_normal_sin_contrasena_recibe_md5_vacio_como_en_vb6()
        {
            Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", CadenaConexion.ResolverCredenciales("nuria", "").Contrasena);
        }

        [Fact]
        public void Cadena_mysqlconnector_lleva_servidor_puerto_y_catalogo()
        {
            var b = new MySqlConnectionStringBuilder(CadenaConexion.Crear("130.0.0.2", "sdprincipal", 3306, "Administrador", "x"));
            Assert.Equal("130.0.0.2", b.Server);
            Assert.Equal(3306u, b.Port);
            Assert.Equal("sdprincipal", b.Database);
            Assert.Equal("root", b.UserID);
            Assert.True(b.ConvertZeroDateTime);
            Assert.True(b.AllowUserVariables);
        }

        [Fact]
        public void Sin_catalogo_usa_mysql()
        {
            var b = new MySqlConnectionStringBuilder(CadenaConexion.Crear("localhost", "", 0, "root", "x"));
            Assert.Equal("mysql", b.Database);
            Assert.Equal(3306u, b.Port);
        }

        [Fact]
        public void Cadena_odbc_legacy_es_igual_a_la_de_vb6()
        {
            var md5 = Md5Vb6.Calcular("clave");
            Assert.Equal(
                "Provider=MSDASQL.1;Password=" + md5 + ";Extended Properties=\"DATABASE=sdprincipal;DRIVER={MySQL ODBC 3.51 Driver};OPTION=18475;PWD=" + md5 + ";PORT=3306;SERVER=130.0.0.2;UID=nuria;\"",
                CadenaConexion.CrearOdbcLegacy("130.0.0.2", "sdprincipal", 3306, "nuria", "clave"));
        }
    }
}
