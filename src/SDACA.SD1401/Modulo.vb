Imports SDACA.Core.Bancos
Imports SDACA.Core.Cheques
Imports SDACA.Core.Datos
Imports SDACA.Ui

Namespace SD1401

    ''' <summary>
    ''' SD1401 - CCB Control de Cuentas Bancarias. Migración de Fuentes\SD1401\Clase.cls, en el orden de
    ''' Menu_Order. Bancos y Monedas son de SD0001 y solo se abren desde aquí. No se migra la opción 016
    ''' ("Pruebas no usar"). Las opciones 008 y 010 están comentadas en Menu_Order.
    ''' </summary>
    Public NotInheritable Class Modulo
        Implements IModulo

        Friend Const TituloModulo As String = "Control de Cuentas Bancarias"

        Public ReadOnly Property Codigo As String Implements IModulo.Codigo
            Get
                Return "SD1401"
            End Get
        End Property

        Public ReadOnly Property Descripcion As String Implements IModulo.Descripcion
            Get
                Return TituloModulo
            End Get
        End Property

        Public ReadOnly Property Privilegio As String Implements IModulo.Privilegio
            Get
                Return "14000"
            End Get
        End Property

        Public ReadOnly Property Opciones As IReadOnlyList(Of OpcionMenu) Implements IModulo.Opciones
            Get
                Return {
                    OpcionMenu.Grupo("Mantenimiento", "14007",
                        OpcionMenu.Formulario("Bancos", "14001", AddressOf SD0001.Modulo.Bancos, IconoMenu.Banco),
                        OpcionMenu.Formulario("Tipos de Monedas", "14008", AddressOf SD0001.Modulo.Monedas, IconoMenu.Lista),
                        OpcionMenu.Formulario("Tipos de Movimiento", "14015",
                            Function(s) New FrmTiposMovimiento(New TiposMovimientoRepositorio(s.Fabrica), s.Local), IconoMenu.Lista),
                        OpcionMenu.Formulario("Transferencias", "14014",
                            Function(s) New FrmTransferencias(New TransferenciasBancarias(s.Fabrica), s.Uid, s.Local), IconoMenu.Lista),
                        OpcionMenu.Formulario("Cerrar Mes", "14016",
                            Function(s) New FrmCierreMes(New CierreMes(s.Fabrica), s.Uid, s.Puede("14007")), IconoMenu.Calendario)),
                    OpcionMenu.Grupo("Procesos", "14013",
                        OpcionMenu.Formulario("Generar archivo de transferencia bancaria", "14012",
                            Function(s) New FrmGenerarTransferencia(New TransferenciasBancarias(s.Fabrica), New Parametros(s.Fabrica), s.Uid, s.Local, s.EstacionId), IconoMenu.Documento)),
                    OpcionMenu.Formulario("Cuentas Bancarias", "14002",
                        Function(s) New FrmCuentasBancarias(New CuentasBancariasRepositorio(s.Fabrica), New ChequesRepositorio(s.Fabrica), s.Uid, s.Local, s.Cedula), IconoMenu.Banco),
                    OpcionMenu.Formulario("Movimientos Bancarios", "14003",
                        Function(s) New FrmLibro(New MovimientosLibro(s.Fabrica), New SaldosCuenta(s.Fabrica), New CierreMes(s.Fabrica), New TiposMovimientoRepositorio(s.Fabrica),
                                                 s.Uid, s.Local, s.Puede("14004"), s.Puede("14007"), s.Cedula, s.Empresa), IconoMenu.Lista),
                    OpcionMenu.Grupo("Conciliación Bancaria", "14004",
                        OpcionMenu.Formulario("Paso #1.a - Conciliación Bancaria CSV", "14004",
                            Function(s) New FrmConciliarArchivo(New ConciliacionBancaria(s.Fabrica), New CierreMes(s.Fabrica), s.Uid, s.Local, s.Puede("14007"), s.Empresa, False), IconoMenu.Documento),
                        OpcionMenu.Formulario("Paso #1.b - Conciliación Bancaria TEC", "14004",
                            Function(s) New FrmConciliarArchivo(New ConciliacionBancaria(s.Fabrica), New CierreMes(s.Fabrica), s.Uid, s.Local, s.Puede("14007"), s.Empresa, True), IconoMenu.Documento),
                        OpcionMenu.Formulario("Paso #2 - Importar Movimientos Bancarios", "14004",
                            Function(s) Pendientes(s, FrmMovimientosPendientes.Paso.ImportarAlLibro), IconoMenu.Lista),
                        OpcionMenu.Formulario("Paso #3 - Errores de Conciliación", "14004",
                            Function(s) Pendientes(s, FrmMovimientosPendientes.Paso.ErroresDeConciliacion), IconoMenu.Lista),
                        OpcionMenu.Formulario("Paso #4 - Conciliación Manual", "14004",
                            Function(s) Pendientes(s, FrmMovimientosPendientes.Paso.ConciliacionManual), IconoMenu.Editar),
                        OpcionMenu.Formulario("Paso #5 - Informe de Conciliación", "14004",
                            Function(s) New FrmInformeConciliacion(New ConciliacionBancaria(s.Fabrica), New CierreMes(s.Fabrica), s.Uid, s.Puede("14007"), s.Empresa), IconoMenu.Imprimir)),
                    OpcionMenu.Formulario("Calcular Cierres", "14010",
                        Function(s) New FrmCalcularCierres(New CierreMes(s.Fabrica), s.Uid, s.Local, s.Puede("14007")), IconoMenu.Calendario),
                    OpcionMenu.Formulario("Saldos por Cuenta", "14017",
                        Function(s) New FrmSaldosCuenta(New CierreMes(s.Fabrica), New SaldosCuenta(s.Fabrica), s.Uid, s.Local, s.Cedula, s.Puede("14007")), IconoMenu.Lista)
                }
            End Get
        End Property

        Private Shared Function Pendientes(s As Core.Seguridad.Sesion, paso As FrmMovimientosPendientes.Paso) As Form
            Return New FrmMovimientosPendientes(New ConciliacionBancaria(s.Fabrica), New CierreMes(s.Fabrica), s.Uid, s.Local, s.Puede("14007"), s.Puede("14006"), s.Empresa, paso)
        End Function

    End Class

End Namespace
