Imports SDACA.Core.Bancos
Imports SDACA.Core.Cheques
Imports SDACA.Core.Datos
Imports SDACA.Core.Licencias
Imports SDACA.Ui

Namespace SD1501

    ''' <summary>SD1501 - ECC Emisión y Control de Cheques. Migración de Fuentes\SD1501\Clase.cls, en el orden de Menu_Order.</summary>
    Public NotInheritable Class Modulo
        Implements IModulo

        Friend Const TituloModulo As String = "Emisión y Control de Cheques"

        Public ReadOnly Property Codigo As String Implements IModulo.Codigo
            Get
                Return "SD1501"
            End Get
        End Property

        Public ReadOnly Property Descripcion As String Implements IModulo.Descripcion
            Get
                Return TituloModulo
            End Get
        End Property

        Public ReadOnly Property Privilegio As String Implements IModulo.Privilegio
            Get
                Return "15000"
            End Get
        End Property

        Public ReadOnly Property Opciones As IReadOnlyList(Of OpcionMenu) Implements IModulo.Opciones
            Get
                Return {
                    OpcionMenu.Grupo("Mantenimiento", "15018",
                        OpcionMenu.Pendiente("Bancos", "15002"),
                        OpcionMenu.Pendiente("Monedas", "15003"),
                        OpcionMenu.Formulario("Cuentas Bancarias", "15004",
                            Function(s) New SD1401.FrmCuentasBancarias(New CuentasBancariasRepositorio(s.Fabrica), New ChequesRepositorio(s.Fabrica), s.Uid, s.Local, s.Cedula), IconoMenu.Banco),
                        OpcionMenu.Formulario("Configuración Cheque", "15005",
                            Function(s) New FrmConfigCheque(New ChequesRepositorio(s.Fabrica), s.Local), IconoMenu.Configurar),
                        OpcionMenu.Formulario("Editar Asiento del Cheque", "15008",
                            Function(s) New FrmEditarCheque(New EmisionCheques(s.Fabrica), New ChequesRepositorio(s.Fabrica), s.Local, s.Uid, s.Puede("14007"), s.Cedula, LicenciaCoopepalmares.CedulaJuridica), IconoMenu.Editar)),
                    OpcionMenu.Pendiente("Pago a Proveedores", "15011"),
                    OpcionMenu.Pendiente("Pago Recibos de Café", "15012"),
                    OpcionMenu.Pendiente("Retiros a la Vista", "15020"),
                    OpcionMenu.Formulario("Impresión de Cheques", "15006",
                        Function(s) New FrmPrintCheque(New EmisionCheques(s.Fabrica), New ImpresionCheques(s.Fabrica), New ChequesRepositorio(s.Fabrica), s.Local, s.Uid, s.Puede("14007"), s.Cedula, LicenciaCoopepalmares.CedulaJuridica, s.EstacionId), IconoMenu.Imprimir),
                    OpcionMenu.Formulario("Consultar Cheque", "15013",
                        Function(s) New FrmVerCheque(New ChequesRepositorio(s.Fabrica), s.Local, s.Uid, s.Puede("14007"), s.Cedula), IconoMenu.Consultar),
                    OpcionMenu.Formulario("Anular Cheques", "15007",
                        Function(s) New FrmAnularCheque(New ChequesRepositorio(s.Fabrica), New AnulacionCheques(s.Fabrica), New Parametros(s.Fabrica), s.Local, s.Uid, s.Puede("14007"), s.Cedula), IconoMenu.Anular),
                    OpcionMenu.Formulario("Impresión Masiva de Cheques", "15009",
                        Function(s) New FrmPrintChequeM(New ChequesRepositorio(s.Fabrica), New ImpresionCheques(s.Fabrica), s.Local, s.Uid, s.Puede("14007"), s.Cedula, s.EstacionId), IconoMenu.Lote),
                    OpcionMenu.Formulario("Mover Cheque", "15010",
                        Function(s) New FrmMoveCheque(New ChequesRepositorio(s.Fabrica), New AnulacionCheques(s.Fabrica), New Parametros(s.Fabrica), s.Local, s.Uid, s.Puede("14007"), s.Cedula, s.EstacionId), IconoMenu.Mover),
                    OpcionMenu.Pendiente("Tramitar Pagos", "15022"),
                    OpcionMenu.Formulario("Entregar Cheques", "15023",
                        Function(s) New FrmEntrega(New ChequesRepositorio(s.Fabrica), s.Uid), IconoMenu.Entregar)
                }
            End Get
        End Property

    End Class

End Namespace
