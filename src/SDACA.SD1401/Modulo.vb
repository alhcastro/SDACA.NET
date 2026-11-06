Imports SDACA.Core.Bancos
Imports SDACA.Core.Cheques
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
                        OpcionMenu.Pendiente("Bancos", "14001"),
                        OpcionMenu.Pendiente("Tipos de Monedas", "14008"),
                        OpcionMenu.Formulario("Tipos de Movimiento", "14015",
                            Function(s) New FrmTiposMovimiento(New TiposMovimientoRepositorio(s.Fabrica), s.Local), IconoMenu.Lista),
                        OpcionMenu.Pendiente("Transferencias", "14014"),
                        OpcionMenu.Formulario("Cerrar Mes", "14016",
                            Function(s) New FrmCierreMes(New CierreMes(s.Fabrica), s.Uid, s.Puede("14007")), IconoMenu.Calendario)),
                    OpcionMenu.Grupo("Procesos", "14013",
                        OpcionMenu.Pendiente("Generar archivo de transferencia bancaria", "14012")),
                    OpcionMenu.Formulario("Cuentas Bancarias", "14002",
                        Function(s) New FrmCuentasBancarias(New CuentasBancariasRepositorio(s.Fabrica), New ChequesRepositorio(s.Fabrica), s.Uid, s.Local, s.Cedula), IconoMenu.Banco),
                    OpcionMenu.Formulario("Movimientos Bancarios", "14003",
                        Function(s) New FrmLibro(New MovimientosLibro(s.Fabrica), New SaldosCuenta(s.Fabrica), New CierreMes(s.Fabrica), New TiposMovimientoRepositorio(s.Fabrica),
                                                 s.Uid, s.Local, s.Puede("14004"), s.Puede("14007"), s.Cedula, s.Empresa), IconoMenu.Lista),
                    OpcionMenu.Grupo("Conciliación Bancaria", "14004",
                        OpcionMenu.Pendiente("Paso #1.a - Conciliación Bancaria CSV", "14004"),
                        OpcionMenu.Pendiente("Paso #1.b - Conciliación Bancaria TEC", "14004"),
                        OpcionMenu.Pendiente("Paso #2 - Importar Movimientos Bancarios", "14004"),
                        OpcionMenu.Pendiente("Paso #3 - Errores de Conciliación", "14004"),
                        OpcionMenu.Pendiente("Paso #4 - Conciliación Manual", "14004"),
                        OpcionMenu.Pendiente("Paso #5 - Informe de Conciliación", "14004")),
                    OpcionMenu.Formulario("Calcular Cierres", "14010",
                        Function(s) New FrmCalcularCierres(New CierreMes(s.Fabrica), s.Uid, s.Local, s.Puede("14007")), IconoMenu.Calendario),
                    OpcionMenu.Formulario("Saldos por Cuenta", "14017",
                        Function(s) New FrmSaldosCuenta(New CierreMes(s.Fabrica), New SaldosCuenta(s.Fabrica), s.Uid, s.Local, s.Cedula, s.Puede("14007")), IconoMenu.Lista)
                }
            End Get
        End Property

    End Class

End Namespace
