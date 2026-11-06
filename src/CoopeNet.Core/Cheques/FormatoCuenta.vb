Namespace Cheques

    ''' <summary>
    ''' Cómo se muestra una cuenta contable según la empresa. Igual que <c>SGCGetCodigo</c> de
    ''' SD1301\ModConta.bas: sin <c>showgands</c> se ocultan los dos primeros niveles de la codificación.
    ''' </summary>
    Public NotInheritable Class FormatoCuenta

        ''' <summary>Valores de SD_CargarEmpresa cuando la cédula no está en sgcempresas.</summary>
        Public Shared ReadOnly Property Predeterminado As New FormatoCuenta("113222", False)

        Private ReadOnly _mostrarGrupos As Boolean
        Private ReadOnly _oculto As Integer

        Public Sub New(codificacion As String, mostrarGrupos As Boolean)
            _mostrarGrupos = mostrarGrupos
            If codificacion.Length >= 2 Then _oculto = CInt(Vb6.Val(codificacion.Substring(0, 1)) + Vb6.Val(codificacion.Substring(1, 1)))
        End Sub

        ''' <summary>SGCGetCodigoSql: la misma regla como expresión de SQL.</summary>
        Public Function ExpresionSql(campo As String) As String
            If _mostrarGrupos Then Return campo
            Return $"MID({campo}, {_oculto + 1}, LENGTH({campo}) - {_oculto})"
        End Function

        Public Function Mostrar(cuenta As String) As String
            Dim texto = If(cuenta, "")
            If _mostrarGrupos Then Return texto
            Return Mid(texto, _oculto + 1)
        End Function

    End Class

End Namespace
