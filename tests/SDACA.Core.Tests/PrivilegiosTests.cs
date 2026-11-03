using System;
using System.Collections.Generic;
using SDACA.Core.Seguridad;
using Xunit;

namespace SDACA.Core.Tests
{
    public class PrivilegiosTests
    {
        private static readonly ISet<string> Usuario = new HashSet<string>(new[] { "6000", "6003", "13000" }, StringComparer.OrdinalIgnoreCase);

        [Theory]
        [InlineData("6000", true)]
        [InlineData("6001", false)]
        [InlineData("6001;6003", true)]   // OR: basta uno
        [InlineData("6001;6002", false)]
        [InlineData("6003;", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Validar_con_OR_como_SD_ValidarPriv(string? requeridos, bool esperado)
        {
            Assert.Equal(esperado, Privilegios.Validar(Usuario, requeridos));
        }

        [Theory]
        [InlineData("6000;6003", true)]   // AND: hacen falta todos
        [InlineData("6000;6001", false)]
        [InlineData("6001;6000", false)]
        public void Validar_con_AND(string requeridos, bool esperado)
        {
            Assert.Equal(esperado, Privilegios.Validar(Usuario, requeridos, ";", cualquiera: false));
        }

        [Fact]
        public void Validar_con_separador_distinto()
        {
            Assert.True(Privilegios.Validar(Usuario, "1|13000", "|"));
        }

        [Theory]
        [InlineData("Administrador", true)]
        [InlineData("ROOT", true)]
        [InlineData("mrodriguez", false)]
        public void Administrador_y_root_reciben_todos(string login, bool esperado)
        {
            Assert.Equal(esperado, Privilegios.EsAdministrador(login));
        }
    }
}
