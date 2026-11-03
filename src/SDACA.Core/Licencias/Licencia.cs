using System;
using System.Collections.Generic;
using System.Linq;
using SDACA.Core.Modelo;
using SDACA.Core.Seguridad;

namespace SDACA.Core.Licencias
{
    /// <summary>Un módulo habilitado en la licencia (SDModulos de reg.cls).</summary>
    public sealed class ModuloLicenciado
    {
        public ModuloLicenciado(string codigo, string siglas, string detalle)
        {
            Codigo = codigo;
            Siglas = siglas;
            Detalle = detalle;
        }

        /// <summary>Código y ProgID base del módulo, ej. "SD0601" → "SD0601.Clase".</summary>
        public string Codigo { get; }
        public string Siglas { get; }
        public string Detalle { get; }

        /// <summary>Clave que el menú pasa a <c>Init()</c>: MD5 del código en minúsculas.</summary>
        public string ClaveInit => Md5Vb6.Calcular(Codigo);

        public override string ToString() => $"{Codigo} {Siglas} - {Detalle}";
    }

    /// <summary>Licencia de uso de un cliente. Reemplaza a las DLL sddatNNN.dll.</summary>
    public interface ILicencia
    {
        Registro Registro { get; }
        IReadOnlyList<ModuloLicenciado> Modulos { get; }
    }

    public static class LicenciaExtensiones
    {
        public static bool TieneModulo(this ILicencia licencia, string codigo) =>
            licencia.Modulos.Any(m => string.Equals(m.Codigo, codigo, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Valida la clave que recibe un módulo en <c>Init()</c>, igual que
        /// <c>If pClave = "132a3832..."</c> en cada Clase.cls.
        /// </summary>
        public static bool ClaveValida(string codigoModulo, string? clave) =>
            string.Equals(Md5Vb6.Calcular(codigoModulo), clave, StringComparison.Ordinal);

        /// <summary>FechaVencimiento = 1900-01-01 significa licencia perpetua.</summary>
        public static bool EstaVigente(this ILicencia licencia, DateTime hoy)
        {
            var vence = licencia.Registro.FechaVencimiento;
            return vence.Year <= 1900 || hoy.Date <= vence.Date;
        }
    }
}
