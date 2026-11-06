''' <summary>Avisos de ModPrin.bas que repiten los módulos.</summary>
Public NotInheritable Class Mensajes

    Private Sub New()
    End Sub

    Public Shared Sub NoConecta()
        MessageBox.Show("NO HAY CONEXION CON LA BASE DE DATOS, COMUNIQUESE CON EL ADMINISTRADOR", "FALLA EN CONEXION",
                        MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Public Shared Sub NoGuardo(descripcion As String, procedimiento As String, titulo As String)
        Dim mensaje = "TRANSACCION NO EJECUTADA" & vbNewLine & "EL SISTEMA A DETECTADO LA SIGUIENTE EXCEPCION:"
        If descripcion.Length > 0 Then mensaje &= vbNewLine & "DESCRIPCION: " & descripcion
        If procedimiento.Length > 0 Then mensaje &= vbNewLine & "PROCEDIMIENTO: " & procedimiento
        MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

End Class
