Imports CoopeNet.Core.Bodegas
Imports CoopeNet.Ui

Namespace SD1101

    ''' <summary>
    ''' SD1101 - SBH Sistema de Bodegas de Herramientas. Migración de Fuentes\SD1101\Clase.cls.
    ''' Las opciones 016 y 017 solo existen en el SD1101.dll instalado (2010.155.629).
    ''' </summary>
    Public NotInheritable Class Modulo
        Implements IModulo

        Friend Const TituloModulo As String = "Sistema de Bodegas de Herramientas"

        Public ReadOnly Property Codigo As String Implements IModulo.Codigo
            Get
                Return "SD1101"
            End Get
        End Property

        Public ReadOnly Property Descripcion As String Implements IModulo.Descripcion
            Get
                Return TituloModulo
            End Get
        End Property

        Public ReadOnly Property Privilegio As String Implements IModulo.Privilegio
            Get
                Return "11000"
            End Get
        End Property

        Public ReadOnly Property Opciones As IReadOnlyList(Of OpcionMenu) Implements IModulo.Opciones
            Get
                Return {
                    OpcionMenu.Grupo("Mantenimiento", "11000",
                        OpcionMenu.Pendiente("Datos Personales", "11002"),
                        OpcionMenu.Pendiente("Usuarios", "11002"),
                        OpcionMenu.Formulario("Tipos de Herramientas", "11002",
                            Function(s) New FrmTiposHerramientas(New TiposHerramientaRepositorio(s.Fabrica), CInt(s.Uid), s.Genero), IconoMenu.Lista),
                        OpcionMenu.Pendiente("Herramientas", "11002"),
                        OpcionMenu.Pendiente("Inventario", "11002"),
                        OpcionMenu.Formulario("Tipos de Préstamos", "11002",
                            Function(s) New FrmTiposPrestamos(New TiposPrestamoRepositorio(s.Fabrica), s.Genero), IconoMenu.Lista),
                        OpcionMenu.Pendiente("Unidades Funcionales", "11002")),
                    OpcionMenu.Grupo("Procesos", "11000",
                        OpcionMenu.Pendiente("Movimientos de Inventario", "11003"),
                        OpcionMenu.Pendiente("Solicitud de Préstamos", "11004"),
                        OpcionMenu.Pendiente("Control de Herramientas", "11005")),
                    OpcionMenu.Grupo("Informes", "11007",
                        OpcionMenu.Pendiente("Consulta de Préstamos", "11007"),
                        OpcionMenu.Pendiente("Herramientas Prestadas", "11007"),
                        OpcionMenu.Pendiente("Herramientas por Tipo", "11007"),
                        OpcionMenu.Pendiente("Movimientos de Inventario", "11007"))
                }
            End Get
        End Property

    End Class

End Namespace
