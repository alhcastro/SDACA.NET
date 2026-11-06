Imports CoopeNet.Core.Modelo

Namespace Licencias

    ''' <summary>
    ''' Licencia de Coopepalmares, cédula jurídica 3-004-045003, cliente 7.
    ''' Copia fiel de <c>Fuentes\registro\07 - Coopepalmares\reg.cls</c> (sddat007.dll, 2009.257.841).
    ''' </summary>
    ''' <remarks>
    ''' reg.cls incluye SD0401 (GCP, Control de Producción), pero Coopepalmares no lo usa
    ''' (confirmado el 2026-11-03), así que no está en esta licencia. Quedan 18 módulos.
    ''' </remarks>
    Public NotInheritable Class LicenciaCoopepalmares
        Implements ILicencia

        Public Const CedulaJuridica As String = "3-004-045003"

        Public ReadOnly Property Registro As Modelo.Registro Implements ILicencia.Registro

        Public ReadOnly Property Modulos As IReadOnlyList(Of ModuloLicenciado) Implements ILicencia.Modulos

        Public Sub New()
            Registro = New Modelo.Registro With {
                .Cedula = CedulaJuridica,
                .Nombre = "Cooperativa de Caficultores y Servicios Múltiples de Palmares R.L.",
                .NombreCorto = "COOPEPALMARES R.L.",
                .Tipo = "1",
                .FechaCompra = New Date(2003, 5, 1),
                .FechaVencimiento = New Date(1900, 1, 1),
                .NumeroCliente = 7,
                .ModulosCount = 18,
                .Asociados = True,
                .UnaEmpresa = True
            }

            Modulos = {
                New ModuloLicenciado("SD0001", "GAT", "Gestion de Asistencia Técnica"),
                New ModuloLicenciado("SD0101", "GAC", "Gestión de Asociados y Clientes"),
                New ModuloLicenciado("SD0201", "GPC", "Gestión de Café"),
                New ModuloLicenciado("SD0301", "GVE", "Gestión de Ventas y Exportaciones"),
                New ModuloLicenciado("SD0502", "POS", "Facturación e Inventarios"),
                New ModuloLicenciado("SD0601", "CXC", "Cuentas por Cobrar"),
                New ModuloLicenciado("SD0701", "CXP", "Cuentas por Pagar"),
                New ModuloLicenciado("SD0801", "CAI", "Cuentas de Ahoro y Certificados a Plazo"),
                New ModuloLicenciado("SD0901", "GPP", "Gestión de Prestamos Personales"),
                New ModuloLicenciado("SD1001", "CAF", "Control de Activos Fijos"),
                New ModuloLicenciado("SD1101", "CEV", "Control de Equipos y Vehiculos"),
                New ModuloLicenciado("SD1201", "SIP", "Sistema Integrado de Presupuesto"),
                New ModuloLicenciado("SD1301", "SGC", "Sistema de Gestion Contable"),
                New ModuloLicenciado("SD1401", "CCB", "Control de Cuentas Bancarias"),
                New ModuloLicenciado("SD1501", "ECC", "Emisión y Control de Cheques"),
                New ModuloLicenciado("SD1601", "ARH", "Administración de Recursos Humanos"),
                New ModuloLicenciado("SD1701", "GCG", "Gestión de Consultas Gerenciales"),
                New ModuloLicenciado("SD1801", "GSE", "Gestión de Servicios Especiales")
            }
        End Sub

    End Class

End Namespace
