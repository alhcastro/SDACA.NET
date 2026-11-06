Imports System.IO
Imports System.Text

Namespace Configuracion

    ''' <summary>Un perfil de conexiones.ini, por ejemplo [CP.LOCAL].</summary>
    Public NotInheritable Class PerfilConexion
        Public Property Nombre As String = ""
        Public Property Servidor As String = ""
        Public Property Puerto As UInteger = 3306
        Public Property Catalogo As String = ""
        ''' <summary>Último usuario que entró con este perfil (USUARIO=).</summary>
        Public Property UltimoUsuario As String = ""
        ''' <summary>Historial de usuarios (USUARIO0=, USUARIO1=, ...).</summary>
        Public ReadOnly Property Usuarios As IList(Of String) = New List(Of String)()
    End Class

    ''' <summary>
    ''' Lee el conexiones.ini que usa hoy el menú VB6, para que la versión .NET ofrezca los
    ''' mismos perfiles sin configurar nada aparte.
    ''' </summary>
    ''' <remarks>
    ''' La sección [GENERAL] no se lee a propósito: su clave CNN guarda una contraseña en texto
    ''' plano. Las contraseñas en .NET se guardan con <see cref="Secretos"/>.
    ''' </remarks>
    Public NotInheritable Class ConexionesIni

        Private Sub New()
        End Sub

        Public Shared Function Leer(ruta As String) As IReadOnlyList(Of PerfilConexion)
            ' El archivo lo escribe VB6 con la página de códigos de Windows (1252).
            Return Parsear(File.ReadAllText(ruta, CodificacionWindows1252()))
        End Function

        Public Shared Function Parsear(texto As String) As IReadOnlyList(Of PerfilConexion)
            Dim perfiles As New List(Of PerfilConexion)()
            Dim actual As PerfilConexion = Nothing
            For Each crudo In texto.Split({vbCrLf, vbLf}, StringSplitOptions.None)
                Dim linea = crudo.Trim()
                If linea.Length = 0 OrElse linea.StartsWith(";") Then Continue For
                If linea.StartsWith("[") AndAlso linea.EndsWith("]") Then
                    Dim nombre = linea.Substring(1, linea.Length - 2).Trim()
                    actual = If(String.Equals(nombre, "GENERAL", StringComparison.OrdinalIgnoreCase), Nothing, New PerfilConexion With {.Nombre = nombre})
                    If actual IsNot Nothing Then perfiles.Add(actual)
                    Continue For
                End If
                If actual Is Nothing Then Continue For
                Dim igual = linea.IndexOf("="c)
                If igual <= 0 Then Continue For
                Dim clave = linea.Substring(0, igual).Trim().ToUpperInvariant()
                Dim valor = linea.Substring(igual + 1).Trim()
                Select Case clave
                    Case "SERVIDOR" : actual.Servidor = valor
                    Case "CATALOGO" : actual.Catalogo = valor
                    Case "PUERTO"
                        Dim p As UInteger
                        actual.Puerto = If(UInteger.TryParse(valor, p) AndAlso p > 0UI, p, 3306UI)
                    Case "USUARIO" : actual.UltimoUsuario = valor
                    Case Else
                        If clave.StartsWith("USUARIO") AndAlso clave.Substring(7).All(AddressOf Char.IsDigit) AndAlso
                           Not actual.Usuarios.Contains(valor, StringComparer.OrdinalIgnoreCase) Then
                            actual.Usuarios.Add(valor)
                        End If
                End Select
            Next
            Return perfiles
        End Function

        ''' <summary>
        ''' Lee una sola llave de un .ini, por ejemplo [GENERAL] DEFAULT. Devuelve "" si no existe
        ''' el archivo, la sección o la llave.
        ''' </summary>
        Public Shared Function LeerValor(ruta As String, seccion As String, llave As String) As String
            If Not File.Exists(ruta) Then Return ""
            Dim dentro = False
            For Each crudo In File.ReadAllLines(ruta, CodificacionWindows1252())
                Dim linea = crudo.Trim()
                If linea.StartsWith("[") AndAlso linea.EndsWith("]") Then
                    dentro = String.Equals(linea.Substring(1, linea.Length - 2).Trim(), seccion, StringComparison.OrdinalIgnoreCase)
                    Continue For
                End If
                If Not dentro Then Continue For
                Dim igual = linea.IndexOf("="c)
                If igual > 0 AndAlso String.Equals(linea.Substring(0, igual).Trim(), llave, StringComparison.OrdinalIgnoreCase) Then
                    Return linea.Substring(igual + 1).Trim()
                End If
            Next
            Return ""
        End Function

        Friend Shared Function CodificacionWindows1252() As Encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
            Return Encoding.GetEncoding(1252)
        End Function

    End Class

End Namespace
