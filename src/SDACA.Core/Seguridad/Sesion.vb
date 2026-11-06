Imports SDACA.Core.Datos

Namespace Seguridad

    ''' <summary>
    ''' Usuario que entró al sistema, con su local, estación y permisos. Reemplaza a las variables
    ''' globales Usuario y Conexion de VB6: cada formulario recibe lo que necesita de aquí.
    ''' </summary>
    Public NotInheritable Class Sesion
        Public Property Login As String = ""
        Public Property Uid As Long
        Public Property Nombre As String = ""
        Public Property Apellidos As String = ""
        ''' <summary>0 masculino, 1 femenino, 2 jurídica.</summary>
        Public Property Genero As String = ""
        Public Property Local As Long
        Public Property LocalDescripcion As String = ""
        Public Property EstacionId As String = ""
        Public Property Estacion As String = ""
        Public Property Catalogo As String = ""
        ''' <summary>Cédula de la licencia, sin guiones.</summary>
        Public Property Cedula As String = ""
        Public Property Permisos As ISet(Of String) = New HashSet(Of String)()
        Public Property Fabrica As IFabricaConexiones

        ''' <summary>Igual que <c>SD_ValidarPriv(Usuario.Privilegios, requeridos)</c>.</summary>
        Public Function Puede(requeridos As String) As Boolean
            Return Privilegios.Validar(Permisos, requeridos)
        End Function
    End Class

End Namespace
