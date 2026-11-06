Imports System.Runtime.CompilerServices
Imports CoopeNet.Core.Modelo
Imports CoopeNet.Core.Seguridad

Namespace Licencias

    ''' <summary>Un módulo habilitado en la licencia (SDModulos de reg.cls).</summary>
    Public NotInheritable Class ModuloLicenciado

        Public Sub New(codigo As String, siglas As String, detalle As String)
            Me.Codigo = codigo
            Me.Siglas = siglas
            Me.Detalle = detalle
        End Sub

        ''' <summary>Código y ProgID base del módulo, ej. "SD0601" → "SD0601.Clase".</summary>
        Public ReadOnly Property Codigo As String
        Public ReadOnly Property Siglas As String
        Public ReadOnly Property Detalle As String

        ''' <summary>Clave que el menú pasa a <c>Init()</c>: MD5 del código en minúsculas.</summary>
        Public ReadOnly Property ClaveInit As String
            Get
                Return Md5Vb6.Calcular(Codigo)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{Codigo} {Siglas} - {Detalle}"
        End Function

    End Class

    ''' <summary>Licencia de uso de un cliente. Reemplaza a las DLL sddatNNN.dll.</summary>
    Public Interface ILicencia
        ReadOnly Property Registro As Registro
        ReadOnly Property Modulos As IReadOnlyList(Of ModuloLicenciado)
    End Interface

    Public Module LicenciaExtensiones

        <Extension>
        Public Function TieneModulo(licencia As ILicencia, codigo As String) As Boolean
            Return licencia.Modulos.Any(Function(m) String.Equals(m.Codigo, codigo, StringComparison.OrdinalIgnoreCase))
        End Function

        ''' <summary>FechaVencimiento = 1900-01-01 significa licencia perpetua.</summary>
        <Extension>
        Public Function EstaVigente(licencia As ILicencia, hoy As Date) As Boolean
            Dim vence = licencia.Registro.FechaVencimiento
            Return vence.Year <= 1900 OrElse hoy.Date <= vence.Date
        End Function

    End Module

End Namespace
