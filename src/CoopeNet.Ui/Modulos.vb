Imports CoopeNet.Core.Seguridad

''' <summary>Una opción del menú: un grupo, un formulario o una opción que falta migrar.</summary>
Public NotInheritable Class OpcionMenu
    Public Property Etiqueta As String = ""
    Public Property Privilegio As String = ""
    ''' <summary>Crea el formulario. Nothing en grupos y en opciones sin migrar.</summary>
    Public Property Crear As Func(Of Sesion, Form)
    Public Property Hijos As IReadOnlyList(Of OpcionMenu) = Array.Empty(Of OpcionMenu)()
    ''' <summary>Glifo de la tarjeta del menú. Ver <see cref="IconoMenu"/>.</summary>
    Public Property Icono As Char = IconoMenu.Documento

    Public ReadOnly Property EsGrupo As Boolean
        Get
            Return Hijos.Count > 0
        End Get
    End Property

    Public Shared Function Grupo(etiqueta As String, privilegio As String, ParamArray hijos As OpcionMenu()) As OpcionMenu
        Return New OpcionMenu With {.Etiqueta = etiqueta, .Privilegio = privilegio, .Hijos = hijos}
    End Function

    Public Shared Function Formulario(etiqueta As String, privilegio As String, crear As Func(Of Sesion, Form), Optional icono As Char = IconoMenu.Documento) As OpcionMenu
        Return New OpcionMenu With {.Etiqueta = etiqueta, .Privilegio = privilegio, .Crear = crear, .Icono = icono}
    End Function

    Public Shared Function Pendiente(etiqueta As String, privilegio As String) As OpcionMenu
        Return New OpcionMenu With {.Etiqueta = etiqueta, .Privilegio = privilegio}
    End Function
End Class

''' <summary>Glifos de Segoe MDL2 Assets, incluida en Windows 10 y posteriores.</summary>
Public NotInheritable Class IconoMenu
    Public Const Documento As Char = ChrW(&HE8A5)
    Public Const Pendiente As Char = ChrW(&HE823)
    Public Const Imprimir As Char = ChrW(&HE749)
    Public Const Lote As Char = ChrW(&HE8C8)
    Public Const Consultar As Char = ChrW(&HE721)
    Public Const Anular As Char = ChrW(&HE711)
    Public Const Mover As Char = ChrW(&HE8DE)
    Public Const Editar As Char = ChrW(&HE70F)
    Public Const Configurar As Char = ChrW(&HE713)
    Public Const Entregar As Char = ChrW(&HE724)
    Public Const Lista As Char = ChrW(&HE8FD)
    Public Const Calendario As Char = ChrW(&HE787)
    Public Const Banco As Char = ChrW(&HE825)
    Public Const Caja As Char = ChrW(&HE8EF)
    Public Const Personas As Char = ChrW(&HE716)
    Public Const Mensaje As Char = ChrW(&HE8BD)

    Private Sub New()
    End Sub
End Class

''' <summary>Lo que cada módulo declara para aparecer en el menú. Reemplaza a Clase.cls.</summary>
Public Interface IModulo
    ''' <summary>Código de la licencia, ej. "SD1101".</summary>
    ReadOnly Property Codigo As String
    ReadOnly Property Descripcion As String
    ''' <summary>Privilegio de acceso al módulo, ej. "11000".</summary>
    ReadOnly Property Privilegio As String
    ReadOnly Property Opciones As IReadOnlyList(Of OpcionMenu)
End Interface
