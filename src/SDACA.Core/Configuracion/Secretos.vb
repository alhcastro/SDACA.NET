Imports System.Runtime.Versioning
Imports System.Security.Cryptography
Imports System.Text

Namespace Configuracion

    ''' <summary>
    ''' Cifra contraseñas con DPAPI de Windows, ligadas al usuario de Windows o al equipo.
    ''' Reemplaza la contraseña en texto plano de conexiones.ini.
    ''' </summary>
    <SupportedOSPlatform("windows")>
    Public NotInheritable Class Secretos

        Private Const Prefijo As String = "dpapi:"
        Private Shared ReadOnly Entropia As Byte() = Encoding.UTF8.GetBytes("SDACA.ERP/conexiones")

        Private Sub New()
        End Sub

        Public Shared Function Proteger(textoPlano As String, Optional porEquipo As Boolean = False) As String
            Dim datos = Encoding.UTF8.GetBytes(textoPlano)
            Dim cifrado = ProtectedData.Protect(datos, Entropia, Alcance(porEquipo))
            Return Prefijo & Convert.ToBase64String(cifrado)
        End Function

        Public Shared Function Desproteger(valor As String, Optional porEquipo As Boolean = False) As String
            If Not valor.StartsWith(Prefijo, StringComparison.Ordinal) Then
                Throw New FormatException("El valor no está cifrado con Secretos.Proteger. Vuelve a guardarlo cifrado.")
            End If
            Dim cifrado = Convert.FromBase64String(valor.Substring(Prefijo.Length))
            Dim datos = ProtectedData.Unprotect(cifrado, Entropia, Alcance(porEquipo))
            Return Encoding.UTF8.GetString(datos)
        End Function

        Private Shared Function Alcance(porEquipo As Boolean) As DataProtectionScope
            Return If(porEquipo, DataProtectionScope.LocalMachine, DataProtectionScope.CurrentUser)
        End Function

    End Class

End Namespace

#If Not NET5_0_OR_GREATER Then
Namespace Global.System.Runtime.Versioning
    ''' <summary>
    ''' En .NET Framework 4.8 no existe este atributo. Se declara aquí para poder marcar las clases
    ''' que solo funcionan en Windows con el mismo código en net48 y net8.0.
    ''' </summary>
    <AttributeUsage(AttributeTargets.All, AllowMultiple:=True, Inherited:=False)>
    Friend NotInheritable Class SupportedOSPlatformAttribute
        Inherits Attribute
        Public Sub New(platformName As String)
            Me.PlatformName = platformName
        End Sub
        Public ReadOnly Property PlatformName As String
    End Class
End Namespace
#End If
