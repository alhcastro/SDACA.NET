''' <summary>Lectura de las Collection de VB6 que vienen dentro de los UDT (Privilegios, Extra).</summary>
Public NotInheritable Class ColeccionVb

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Igual que SD_LoadFromCollection: el valor de la llave como texto, o "" si no existe.
    ''' </summary>
    Public Shared Function Leer(coleccion As VBA.Collection, llave As String) As String
        If coleccion Is Nothing Then Return ""
        Try
            Dim indice As Object = llave
            Return If(Convert.ToString(coleccion.Item(indice)), "")
        Catch
            Return ""
        End Try
    End Function

End Class
