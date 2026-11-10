Imports CoopeNet.Core.Pos
Imports CoopeNet.Core.Seguridad
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' SD0502 - POS, Facturación e Inventarios de Coopepalmares. Migración de Fuentes\SD0502(Coopepalmares)\Clase.cls.
    ''' Por ahora el Catálogo de Artículos, Cajas y el Verificador de Presentación; las demás opciones del Menu_Order se agregan al migrarlas.
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

        ''' <summary>frmMantArticulos2: precio por unidad de medida y presentación del artículo. Entra con el 5057.</summary>
        Public Shared Function Presentacion(s As Sesion) As Form
            Return New FrmPresentacion(New PresentacionesRepositorio(s.Fabrica), New ArticulosRepositorio(s.Fabrica), s.Local)
        End Function

        ''' <summary>frmMantCajas: la caja de cada estación y sus usuarios. Entra con el 5018.</summary>
        Public Shared Function Cajas(s As Sesion) As Form
            Return New FrmCajas(New CajasRepositorio(s.Fabrica), s.Local, s.Uid, s.EstacionId, s.Genero)
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
                        OpcionMenu.Formulario("Verificador de Presentación", "5057", AddressOf Presentacion, IconoMenu.Consultar))
                }
            End Get
        End Property

    End Class

End Namespace
