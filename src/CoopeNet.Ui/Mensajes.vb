''' <summary>Avisos de ModPrin.bas que repiten los módulos.</summary>
Public NotInheritable Class Mensajes

    Private Sub New()
    End Sub

    Public Shared Sub NoConecta()
        MessageBox.Show("NO HAY CONEXION CON LA BASE DE DATOS, COMUNIQUESE CON EL ADMINISTRADOR", "FALLA EN CONEXION",
                        MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Public Shared Sub NoPrivilegio(nombreUsuario As String, titulo As String)
        MessageBox.Show($"El usuario {nombreUsuario} no tiene suficientes privilegios para accesar a esta opción.", titulo,
                        MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Public Shared Sub NoGuardo(descripcion As String, procedimiento As String, titulo As String)
        Dim mensaje = "TRANSACCION NO EJECUTADA" & vbCrLf & "EL SISTEMA A DETECTADO LA SIGUIENTE EXCEPCION:"
        If descripcion.Length > 0 Then mensaje &= vbCrLf & "DESCRIPCION: " & descripcion
        If procedimiento.Length > 0 Then mensaje &= vbCrLf & "PROCEDIMIENTO: " & procedimiento
        MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

End Class
