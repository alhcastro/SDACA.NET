Imports System.Runtime.InteropServices
Imports SDACA.Core.Datos
Imports SDACA.Core.Seguridad

Namespace SD1101

    ''' <summary>
    ''' SD1101 - SBH Sistema de Bodegas de Herramientas. Migración de Fuentes\SD1101\Clase.cls.
    ''' </summary>
    <ComVisible(True)>
    <Guid("6F0C5B1E-2D7A-4E3B-9C41-5A8B1D7E1101")>
    <ProgId("SD1101.Clase")>
    <ClassInterface(ClassInterfaceType.None)>
    <ComDefaultInterface(GetType(IModuloSdaca))>
    Public NotInheritable Class Clase
        Inherits ModuloComBase

        Friend Const TituloModulo As String = "Sistema de Bodegas de Herramientas"

        Protected Overrides ReadOnly Property Codigo As String
            Get
                Return "SD1101"
            End Get
        End Property

        Protected Overrides ReadOnly Property Siglas As String
            Get
                Return "SBH"
            End Get
        End Property

        Public Overrides Function SDClase() As SD_Clase
            If Not Autorizado Then Return Nothing
            Return New SD_Clase With {
                .Clase = "SD1101",
                .Titulo = "SBH",
                .Descripcion = TituloModulo,
                .Privilegio = "11000",
                .Icono = 101,
                .Picture = 0,
                .Reportes = "",
                .Color = &H80000013,
                .Preferencias = ""
            }
        End Function

        Public Overrides Function Menu(Index As String) As SD_InfoMenu
            If Not Autorizado Then Return Nothing
            Dim m As New SD_InfoMenu With {.Key = Index, .Etiqueta = "", .Privilegio = ""}
            Select Case Index
                Case "001" : Llenar(m, "Mantenimiento", 102, "11000")
                Case "002" : Llenar(m, "Tipos de Préstamos", 103, "11002")
                Case "003" : Llenar(m, "Tipos de Herramientas", 104, "11002")
                Case "004" : Llenar(m, "Unidades Funcionales", 105, "11002")
                Case "005" : Llenar(m, "Herramientas", 106, "11002")
                Case "006" : Llenar(m, "Inventario", 107, "11002")
                Case "007" : Llenar(m, "Procesos", 108, "11000")
                Case "008" : Llenar(m, "Movimientos de Inventario", 109, "11003")
                Case "009" : Llenar(m, "Solicitud de Préstamos", 110, "11004")
                Case "010" : Llenar(m, "Control de Herramientas", 111, "11005")
                Case "011" : Llenar(m, "Usuarios", 112, "11002")
                Case "012" : Llenar(m, "Datos Personales", 113, "11002")
                Case "013" : Llenar(m, "Informes", 114, "11007")
                Case "014" : Llenar(m, "Consulta de Préstamos", 115, "11007")
                Case "015" : Llenar(m, "Herramientas Prestadas", 115, "11007")
                ' 016 y 017 solo existen en el SD1101.dll instalado (2010.155.629): Fuentes\SD1101 es de 2009.
                Case "016" : Llenar(m, "Herramientas por Tipo", 115, "11007")
                Case "017" : Llenar(m, "Movimientos de Inventario", 115, "11007")
            End Select
            Return m
        End Function

        Private Shared Sub Llenar(ByRef m As SD_InfoMenu, etiqueta As String, icono As Integer, privilegio As String)
            m.Etiqueta = etiqueta
            m.Icono48 = icono
            m.Icono16 = icono
            m.Privilegio = privilegio
        End Sub

        Public Overrides Sub Accion(pKey As String)
            If Not Autorizado Then Return
            If Not Privilegios.Validar(Function(p) ColeccionVb.Leer(Usuario.Privilegios, p) = "1", Menu(pKey).Privilegio) Then Return
            Select Case pKey
                Case "001", "007", "013" ' Grupos del menú: no abren nada.
                Case "003"
                    Dim fabrica As New FabricaConexiones(CadenaConexion.DesdeOdbc(Conexion.ConnectionString))
                    MostrarModal(New FrmTiposHerramientas(New Core.Bodegas.TiposHerramientaRepositorio(fabrica), Usuario.UID, Usuario.Genero))
                Case "002", "004", "005", "006", "008", "009", "010", "011", "012", "014", "015", "016", "017"
                    MessageBox.Show("Esta opción todavía no está migrada a .NET.", TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Select
        End Sub

        Public Overrides Function Menu_Order() As Object
            If Not Autorizado Then Return Nothing
            ' Igual que el SD1101.dll instalado: 17 elementos, sin un Empty al final.
            Return New Object() {
                "001", "001.012", "001.011", "001.003", "001.005", "001.006", "001.002", "001.004",
                "007", "007.008", "007.009", "007.010",
                "013", "013.014", "013.015", "013.016", "013.017"
            }
        End Function

    End Class

End Namespace
