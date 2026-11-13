Imports CoopeNet.Core.Pos
Imports CoopeNet.Core.Seguridad
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' SD0502 - POS, Facturación e Inventarios de Coopepalmares. Migración de Fuentes\SD0502(Coopepalmares)\Clase.cls.
    ''' Por ahora el Catálogo de Artículos, Cajas, Datos personales, Clientes, Código de asociados, Proveedores, Mensajes
    ''' para clientes, Justificaciones, Tipos de moneda, Tipo de cambio y el Verificador de Presentación; las demás opciones del Menu_Order se agregan al migrarlas.
    ''' </summary>
    Public NotInheritable Class Modulo
        Implements IModulo

        Friend Const TituloModulo As String = "Facturación e Inventarios"

        Public ReadOnly Property Codigo As String Implements IModulo.Codigo
            Get
                Return "SD0502"
            End Get
        End Property

        Public ReadOnly Property Descripcion As String Implements IModulo.Descripcion
            Get
                Return TituloModulo
            End Get
        End Property

        Public ReadOnly Property Privilegio As String Implements IModulo.Privilegio
            Get
                Return "5000"
            End Get
        End Property

        ''' <summary>
        ''' Entran con el 5009. Nuevo, Editar y Borrar piden el 5037; utilidades, precios y descuentos, el 5056; las
        ''' existencias por local, el 5008.
        ''' </summary>
        Public Shared Function Articulos(s As Sesion) As Form
            Dim f = New FrmArticulos(New ArticulosRepositorio(s.Fabrica), New ImagenesArticuloRepositorio(s.Fabrica), New ProveedoresArticuloRepositorio(s.Fabrica), New CodigosBarrasRepositorio(s.Fabrica), New AlternosArticuloRepositorio(s.Fabrica), New CabysRepositorio(s.Fabrica), s.DirectorioDatos, s.Local, s.Uid, s.Login, s.Estacion, s.LocalDescripcion, s.Puede("5037"), s.Puede("5056"))
            f.AbrirExistencias = Function(codigo) Existencias(s, codigo)
            f.PuedeExistencias = s.Puede("5008")
            f.NombreUsuario = s.Nombre
            f.AbrirMovimientos = Function(codigo) Movimientos(s, codigo)
            f.AbrirUtilidades = Function(codigo) Consultas(s, 1, codigo)
            Return f
        End Function

        ''' <summary>SD0502REP.ConsultasMod (frmConsultas): las consultas de posconsultas, con la pedida ya ejecutada.</summary>
        Public Shared Function Consultas(s As Sesion, consulta As Long, filtro As String) As Form
            Return New FrmConsultas(New Core.Datos.ConsultasGuardadasRepositorio(s.Fabrica), s.Local, s.DirectorioDatos, consulta, filtro)
        End Function

        ''' <summary>SD0502REP.ConsultaMovimientos (frmVentDetall): movimientos y gráfico del artículo.</summary>
        Public Shared Function Movimientos(s As Sesion, codigo As String) As Form
            Return New FrmMovimientos(New MovimientosArticuloRepositorio(s.Fabrica), New ArticulosRepositorio(s.Fabrica), codigo, s.Local, s.LocalDescripcion, s.Empresa, s.DirectorioDatos)
        End Function

        ''' <summary>
        ''' Datos personales (opción 055, privilegio 5085): VB6 abre la de SD0001 con clsDP.ShowDP, sin cédula ni
        ''' acción de módulo, así que es la misma pantalla. Editar e imágenes piden los privilegios de SD0001.
        ''' </summary>
        Public Shared Function DatosPersonales(s As Sesion) As Form
            Return SD0001.Modulo.DatosPersonales(s)
        End Function

        ''' <summary>frmMantArticulos2: precio por unidad de medida y presentación del artículo. Entra con el 5057.</summary>
        Public Shared Function Presentacion(s As Sesion) As Form
            Return New FrmPresentacion(New PresentacionesRepositorio(s.Fabrica), New ArticulosRepositorio(s.Fabrica), s.Local)
        End Function

        ''' <summary>frmMantCajas: la caja de cada estación y sus usuarios. Entra con el 5018.</summary>
        Public Shared Function Cajas(s As Sesion) As Form
            Return New FrmCajas(New CajasRepositorio(s.Fabrica), s.Local, s.Uid, s.EstacionId, s.Genero)
        End Function

        ''' <summary>frmClientes: las condiciones de venta de cada cliente en el local. Entra con el 5038.</summary>
        Public Shared Function Clientes(s As Sesion) As Form
            Return New FrmClientes(New ClientesRepositorio(s.Fabrica), s.Local)
        End Function

        ''' <summary>
        ''' Código de asociados y clientes (opción 031, privilegio 5038): VB6 abre el administrador de códigos de
        ''' SD0001 con clsDP.ShowCodigos, sin cédula, solo con otro título.
        ''' </summary>
        Public Shared Function CodigosClientes(s As Sesion) As Form
            Return SD0001.Modulo.AdministrarCodigos(s, "Administración de códigos de cliente")
        End Function

        ''' <summary>
        ''' FrmMantProveedores (opción 016, privilegio 5022): proveedores del local y sus cuentas de pago. La cuenta
        ''' contable es obligatoria porque la licencia trae contabilidad (SD1301). Una cédula que no está en Datos
        ''' personales se registra abriendo esa pantalla.
        ''' </summary>
        Public Shared Function Proveedores(s As Sesion) As Form
            Dim cheques As New Core.Cheques.ChequesRepositorio(s.Fabrica)
            Return New FrmProveedores(New ProveedoresRepositorio(s.Fabrica), Function() cheques.ObtenerFormatoCuentaAsync(s.Cedula), s.Local, s.Uid, s.Cedula, exigeContable:=True) With {
                .AbrirDatosPersonales = Function() SD0001.Modulo.DatosPersonales(s)
            }
        End Function

        ''' <summary>
        ''' frmMantMensajes (opción 019, privilegio 5024): los mensajes de la pantalla del cliente y su velocidad.
        ''' La imagen de cada mensaje se importa y se borra con el 5000, como en FrmImgsMens.
        ''' </summary>
        Public Shared Function MensajesClientes(s As Sesion) As Form
            Return New FrmMensajesClientes(New MensajesClientesRepositorio(s.Fabrica), New Core.Datos.Parametros(s.Fabrica), s.Local, s.Uid, s.Genero, s.Puede("5000"))
        End Function

        ''' <summary>frmMantJustif (opción 010, privilegio 5006): los motivos de anulaciones y devoluciones.</summary>
        Public Shared Function Justificaciones(s As Sesion) As Form
            Return New FrmJustificaciones(New JustificacionesRepositorio(s.Fabrica), s.Uid, s.Genero)
        End Function

        ''' <summary>frmtipomonedas (opción 025, privilegio 5035): las monedas del POS, con su botón de tipo de cambio.</summary>
        Public Shared Function TiposMoneda(s As Sesion) As Form
            Return New FrmTiposMoneda(New MonedasPosRepositorio(s.Fabrica), s.Uid, s.Local, s.Genero)
        End Function

        ''' <summary>frmtipocambio (opción 029, privilegio 5046): el tipo de cambio del día en postipocambio.</summary>
        Public Shared Function TipoCambio(s As Sesion) As Form
            Return New FrmTipoCambioPos(New MonedasPosRepositorio(s.Fabrica), s.Uid, s.Local)
        End Function

        ''' <summary>FrmMantLocales: existencias del artículo por local.</summary>
        Public Shared Function Existencias(s As Sesion, codigo As String) As Form
            Return New FrmExistencias(New ExistenciasLocalRepositorio(s.Fabrica), New ArticulosRepositorio(s.Fabrica), codigo, s.Local, s.Uid, s.Estacion, s.DirectorioDatos)
        End Function

        Public ReadOnly Property Opciones As IReadOnlyList(Of OpcionMenu) Implements IModulo.Opciones
            Get
                Return {
                    OpcionMenu.Grupo("Mantenimiento", "5059",
                        OpcionMenu.Formulario("Catálogo de Artículos", "5009", AddressOf Articulos, IconoMenu.Lista),
                        OpcionMenu.Formulario("Cajas", "5018", AddressOf Cajas, IconoMenu.Caja),
                        OpcionMenu.Formulario("Datos personales", "5085", AddressOf DatosPersonales, IconoMenu.Editar),
                        OpcionMenu.Formulario("Clientes", "5038", AddressOf Clientes, IconoMenu.Personas),
                        OpcionMenu.Formulario("Código de asociados y clientes", "5038", AddressOf CodigosClientes, IconoMenu.Lista),
                        OpcionMenu.Formulario("Proveedores", "5022", AddressOf Proveedores, IconoMenu.Entregar),
                        OpcionMenu.Formulario("Mensajes para clientes", "5024", AddressOf MensajesClientes, IconoMenu.Mensaje),
                        OpcionMenu.Formulario("Justificaciones", "5006", AddressOf Justificaciones, IconoMenu.Lista),
                        OpcionMenu.Formulario("Tipos de moneda", "5035", AddressOf TiposMoneda, IconoMenu.Banco),
                        OpcionMenu.Formulario("Tipo de cambio", "5046", AddressOf TipoCambio, IconoMenu.Calendario),
                        OpcionMenu.Formulario("Verificador de Presentación", "5057", AddressOf Presentacion, IconoMenu.Consultar))
                }
            End Get
        End Property

    End Class

End Namespace
