Imports CoopeNet.Core.Pos
Imports CoopeNet.Core.Seguridad
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' SD0502 - POS, Facturación e Inventarios de Coopepalmares. Migración de Fuentes\SD0502(Coopepalmares)\Clase.cls.
    ''' Por ahora solo el Catálogo de Artículos; las demás opciones del Menu_Order se agregan al migrarlas.
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
        ''' Entran con el 5009. Nuevo, Editar y Borrar piden el 5037; utilidades, precios y descuentos, el 5056.
        ''' </summary>
        Public Shared Function Articulos(s As Sesion) As Form
            Return New FrmArticulos(New ArticulosRepositorio(s.Fabrica), New ImagenesArticuloRepositorio(s.Fabrica), New ProveedoresArticuloRepositorio(s.Fabrica), New CodigosBarrasRepositorio(s.Fabrica), s.Local, s.Uid, s.Login, s.Estacion, s.LocalDescripcion, s.Puede("5037"), s.Puede("5056"))
        End Function

        Public ReadOnly Property Opciones As IReadOnlyList(Of OpcionMenu) Implements IModulo.Opciones
            Get
                Return {
                    OpcionMenu.Grupo("Mantenimiento", "5059",
                        OpcionMenu.Formulario("Catálogo de Artículos", "5009", AddressOf Articulos, IconoMenu.Lista))
                }
            End Get
        End Property

    End Class

End Namespace
