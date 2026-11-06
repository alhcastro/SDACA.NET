Imports SDACA.Core.Catalogos
Imports SDACA.Core.Seguridad
Imports SDACA.Ui

Namespace SD0001

    ''' <summary>
    ''' SD0001 - GAT Gestión de Asistencia Técnica. Migración de Fuentes\SD0001\Clase.cls: las opciones que
    ''' atiende su Accion. Las demás del Menu_Order (usuarios, departamentos, respaldos) las atiende el menú
    ''' de VB6, no este módulo.
    ''' </summary>
    Public NotInheritable Class Modulo
        Implements IModulo

        Friend Const TituloModulo As String = "Gestión de Asistencia Técnica"

        Public ReadOnly Property Codigo As String Implements IModulo.Codigo
            Get
                Return "SD0001"
            End Get
        End Property

        Public ReadOnly Property Descripcion As String Implements IModulo.Descripcion
            Get
                Return TituloModulo
            End Get
        End Property

        Public ReadOnly Property Privilegio As String Implements IModulo.Privilegio
            Get
                Return "0"
            End Get
        End Property

        ''' <summary>También los abren SD1401 y SD1501, con su propio privilegio.</summary>
        Public Shared Function Bancos(s As Sesion) As Form
            Return New FrmBancos(New CatalogosMonetarios(s.Fabrica), s.Uid, s.Local)
        End Function

        Public Shared Function Monedas(s As Sesion) As Form
            Return New FrmMonedas(New CatalogosMonetarios(s.Fabrica), s.Uid, s.Local)
        End Function

        ''' <summary>Todos entran a consultar. Editar pide el 10 o el 19015, e imágenes el 11 o el 19016.</summary>
        Public Shared Function DatosPersonales(s As Sesion) As Form
            Return New FrmDatosPersonales(New DatosPersonalesRepositorio(s.Fabrica), s.Uid, s.Local, s.Puede("10;19015"), Privilegios.EsAdministrador(s.Login), s.Puede("11;19016"), s.Puede("12"), Function() TiposCodigo(s))
        End Function

        ''' <summary>Sin opción en el menú: la abre Agregar código con el privilegio 12.</summary>
        Private Shared Function TiposCodigo(s As Sesion) As Form
            Return New FrmTiposCodigo(New CodigosRepositorio(s.Fabrica), s.Uid, s.Local)
        End Function

        Private Shared Function AdminCodigos(s As Sesion) As Form
            Return New FrmAdminCodigos(New DatosPersonalesRepositorio(s.Fabrica), New CodigosRepositorio(s.Fabrica), s.Uid, s.Local, s.Puede("12"), Function() TiposCodigo(s))
        End Function

        Public ReadOnly Property Opciones As IReadOnlyList(Of OpcionMenu) Implements IModulo.Opciones
            Get
                Return {
                    OpcionMenu.Grupo("Mantenimiento", "8",
                        OpcionMenu.Formulario("Datos Personales", "0", AddressOf DatosPersonales, IconoMenu.Editar),
                        OpcionMenu.Formulario("Administración de Códigos", "1", AddressOf AdminCodigos, IconoMenu.Lista),
                        OpcionMenu.Pendiente("Cambio de Identificación", "1"),
                        OpcionMenu.Pendiente("Cambio de Identificación Masiva", "999"),
                        OpcionMenu.Pendiente("Padrón Nacional", "1"),
                        OpcionMenu.Pendiente("Padrón Jurídico", "1"),
                        OpcionMenu.Formulario("Bancos", "1", AddressOf Bancos, IconoMenu.Banco),
                        OpcionMenu.Formulario("Monedas", "1", AddressOf Monedas, IconoMenu.Lista),
                        OpcionMenu.Formulario("Tipo de Cambio de Moneda", "1",
                            Function(s) New FrmTipoCambio(New CatalogosMonetarios(s.Fabrica), s.Uid, s.Local), IconoMenu.Calendario),
                        OpcionMenu.Pendiente("Fallecidos", "13"),
                        OpcionMenu.Pendiente("Configuración de Impresión y Consecutivos API", "1")),
                    OpcionMenu.Pendiente("Agenda", "0"),
                    OpcionMenu.Grupo("Sistema", "0",
                        OpcionMenu.Pendiente("Reparar BD", "1"),
                        OpcionMenu.Pendiente("Sincronizar", "14"))
                }
            End Get
        End Property

    End Class

End Namespace
