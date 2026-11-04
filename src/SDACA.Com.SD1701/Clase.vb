Imports System.Runtime.InteropServices
Imports SDACA.Core.Seguridad

Namespace SD1701

    ''' <summary>
    ''' SD1701 - GCG Consultas Gerenciales. Migración de Fuentes\SD1701\Clase.cls.
    ''' </summary>
    ''' <remarks>
    ''' El menú no pide opciones a este módulo: para los códigos SD17 arma el árbol con
    ''' getMantCGerenciales (tabla gatcgeren), que llama a los otros módulos. Aquí solo
    ''' importan SDClase (para el nodo principal y el privilegio 17000) e Init.
    ''' </remarks>
    <ComVisible(True)>
    <Guid("6F0C5B1E-2D7A-4E3B-9C41-5A8B1D7E1701")>
    <ProgId("SD1701.Clase")>
    <ClassInterface(ClassInterfaceType.None)>
    <ComDefaultInterface(GetType(IModuloSdaca))>
    Public NotInheritable Class Clase
        Inherits ModuloComBase

        Protected Overrides ReadOnly Property Codigo As String
            Get
                Return "SD1701"
            End Get
        End Property

        ' El original pone "ECC" (copiado de SD1501). Solo lo usa la copia interna de CONFIG,
        ' así que se corrige a las siglas reales del módulo.
        Protected Overrides ReadOnly Property Siglas As String
            Get
                Return "GCG"
            End Get
        End Property

        Public Overrides Function SDClase() As SD_Clase
            If Not Autorizado Then Return Nothing
            Return New SD_Clase With {
                .Clase = "SD1701",
                .Titulo = "GCG",
                .Descripcion = "Consultas Gerenciales",
                .Privilegio = "17000",
                .Icono = 101,
                .Picture = 0,
                .Reportes = "",
                .Color = &HD9E4EE,
                .Preferencias = ""
            }
        End Function

        Public Overrides Function Menu(Index As String) As SD_InfoMenu
            If Not Autorizado Then Return Nothing
            Dim m As New SD_InfoMenu With {.Key = Index, .Etiqueta = "", .Privilegio = ""}
            Select Case Index
                Case "001"
                    m.Etiqueta = "Mantenimiento"
                    m.Icono48 = 102
                    m.Icono16 = 103
                    m.Privilegio = "17000"
            End Select
            Return m
        End Function

        Public Overrides Sub Accion(pKey As String)
            If Not Autorizado Then Return
            If Not Privilegios.Validar(Function(p) ColeccionVb.Leer(Usuario.Privilegios, p) = "1", Menu(pKey).Privilegio) Then Return
            Select Case pKey
                Case "000" ' Mantenimientos: vacío en el original.
            End Select
        End Sub

        Public Overrides Function Menu_Order() As Object
            If Not Autorizado Then Return Nothing
            Return New Object() {"", Nothing}
        End Function

    End Class

End Namespace
