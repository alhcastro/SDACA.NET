using System;
using System.Collections.Generic;
using SDACA.Core.Modelo;

namespace SDACA.Core.Licencias
{
    /// <summary>
    /// Licencia de Coopepalmares, cédula jurídica 3-004-045003, cliente 7.
    /// Copia fiel de <c>Fuentes\registro\07 - Coopepalmares\reg.cls</c> (sddat007.dll, 2009.257.841).
    /// </summary>
    /// <remarks>
    /// SD0401 (GCP) está en la licencia, pero no hay carpeta SD0401 en Fuentes. Se deja igual
    /// que en el original; el menú lo ignora si no puede crear el objeto.
    /// </remarks>
    public sealed class LicenciaCoopepalmares : ILicencia
    {
        public const string CedulaJuridica = "3-004-045003";

        public Registro Registro { get; } = new Registro
        {
            Cedula = CedulaJuridica,
            Nombre = "Cooperativa de Caficultores y Servicios Múltiples de Palmares R.L.",
            NombreCorto = "COOPEPALMARES R.L.",
            Tipo = "1",
            FechaCompra = new DateTime(2003, 5, 1),
            FechaVencimiento = new DateTime(1900, 1, 1),
            NumeroCliente = 7,
            ModulosCount = 19,
            Asociados = true,
            UnaEmpresa = true,
        };

        public IReadOnlyList<ModuloLicenciado> Modulos { get; } = new[]
        {
            new ModuloLicenciado("SD0001", "GAT", "Gestion de Asistencia Técnica"),
            new ModuloLicenciado("SD0101", "GAC", "Gestión de Asociados y Clientes"),
            new ModuloLicenciado("SD0201", "GPC", "Gestión de Café"),
            new ModuloLicenciado("SD0301", "GVE", "Gestión de Ventas y Exportaciones"),
            new ModuloLicenciado("SD0401", "GCP", "Gestión de Control de Producción"),
            new ModuloLicenciado("SD0502", "POS", "Facturación e Inventarios"),
            new ModuloLicenciado("SD0601", "CXC", "Cuentas por Cobrar"),
            new ModuloLicenciado("SD0701", "CXP", "Cuentas por Pagar"),
            new ModuloLicenciado("SD0801", "CAI", "Cuentas de Ahoro y Certificados a Plazo"),
            new ModuloLicenciado("SD0901", "GPP", "Gestión de Prestamos Personales"),
            new ModuloLicenciado("SD1001", "CAF", "Control de Activos Fijos"),
            new ModuloLicenciado("SD1101", "CEV", "Control de Equipos y Vehiculos"),
            new ModuloLicenciado("SD1201", "SIP", "Sistema Integrado de Presupuesto"),
            new ModuloLicenciado("SD1301", "SGC", "Sistema de Gestion Contable"),
            new ModuloLicenciado("SD1401", "CCB", "Control de Cuentas Bancarias"),
            new ModuloLicenciado("SD1501", "ECC", "Emisión y Control de Cheques"),
            new ModuloLicenciado("SD1601", "ARH", "Administración de Recursos Humanos"),
            new ModuloLicenciado("SD1701", "GCG", "Gestión de Consultas Gerenciales"),
            new ModuloLicenciado("SD1801", "GSE", "Gestión de Servicios Especiales"),
        };
    }
}
