Namespace Modelo

    ''' <summary>SDACA_Registro: datos de la licencia del cliente.</summary>
    Public NotInheritable Class Registro
        ''' <summary>Cédula con guiones, como en reg.cls: "3-004-045003".</summary>
        Public Property Cedula As String = ""
        Public Property Nombre As String = ""
        Public Property NombreCorto As String = ""
        ''' <summary>"0" persona física, "1" persona jurídica.</summary>
        Public Property Tipo As String = ""
        Public Property FechaCompra As Date
        ''' <summary>1900-01-01 significa sin vencimiento.</summary>
        Public Property FechaVencimiento As Date = New Date(1900, 1, 1)
        Public Property NumeroCliente As Long
        Public Property VersionRegistro As String = ""
        Public Property ModulosCount As Integer
        Public Property Asociados As Boolean
        Public Property UnaEmpresa As Boolean
        Public Property Telefono As String = ""
        Public Property Fax As String = ""
        Public Property Email As String = ""

        ''' <summary>Cédula sin guiones ("3004045003"), como se usa en factura electrónica y privilegios.</summary>
        Public ReadOnly Property CedulaSinGuiones As String
            Get
                Return Cedula.Replace("-", "")
            End Get
        End Property
    End Class

End Namespace
