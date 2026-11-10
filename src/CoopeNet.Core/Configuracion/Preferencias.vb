Imports System.IO

Namespace Configuracion

    ''' <summary>
    ''' Lo que el ERP recuerda entre sesiones: si el menú quedó maximizado, la última conexión y el
    ''' último local de cada usuario. Va en la carpeta del usuario de Windows porque los .ini de VB6
    ''' están en Program Files y no se modifican.
    ''' </summary>
    ''' <remarks>
    ''' Nada de esto debe impedir entrar o salir del sistema: si el archivo no se puede leer o
    ''' escribir, se sigue con los valores predeterminados.
    ''' </remarks>
    Public NotInheritable Class Preferencias

        Private Const LlaveMaximizado As String = "MENU.MAXIMIZADO"
        Private Const LlaveModulo As String = "MENU.MODULO"
        Private Const LlavePerfil As String = "CONEXION.PERFIL"
        Private Const LlaveUsuario As String = "CONEXION.USUARIO"
        Private Const PrefijoLocal As String = "LOCAL:"

        Private ReadOnly _ruta As String
        Private ReadOnly _locales As New Dictionary(Of String, Long)(StringComparer.OrdinalIgnoreCase)

        Public Property MenuMaximizado As Boolean

        ''' <summary>Código del módulo que estaba abierto en el menú al cerrar, ej. "SD0502"; "" si ninguno.</summary>
        Public Property MenuModulo As String = ""

        ''' <summary>Perfil de sdcnn007.ini con el que se entró la última vez; "" si no había perfil.</summary>
        Public Property UltimoPerfil As String = ""

        ''' <summary>Usuario de esa última entrada en este equipo; el USUARIO= del .ini es el del perfil.</summary>
        Public Property UltimoUsuario As String = ""

        Private Sub New(ruta As String)
            _ruta = ruta
        End Sub

        Public Shared ReadOnly Property RutaPredeterminada As String
            Get
                Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SDACA", "preferencias.ini")
            End Get
        End Property

        Public Shared Function Cargar(ruta As String) As Preferencias
            Dim preferencias As New Preferencias(ruta)
            Try
                If File.Exists(ruta) Then
                    For Each linea In File.ReadAllLines(ruta)
                        preferencias.Interpretar(linea)
                    Next
                End If
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                Return New Preferencias(ruta)
            End Try
            Return preferencias
        End Function

        Private Sub Interpretar(linea As String)
            Dim primera = linea.IndexOf("="c)
            If primera > 0 Then
                Dim texto = linea.Substring(primera + 1).Trim()
                Select Case linea.Substring(0, primera).ToUpperInvariant()
                    Case LlavePerfil
                        UltimoPerfil = texto
                        Return
                    Case LlaveUsuario
                        UltimoUsuario = texto
                        Return
                    Case LlaveModulo
                        MenuModulo = texto
                        Return
                End Select
            End If
            ' La última "=": la llave lleva datos de conexión y el valor siempre es un número.
            Dim igual = linea.LastIndexOf("="c)
            If igual <= 0 Then Return
            Dim llave = linea.Substring(0, igual)
            Dim valor = linea.Substring(igual + 1).Trim()
            Dim local As Long
            If String.Equals(llave, LlaveMaximizado, StringComparison.OrdinalIgnoreCase) Then
                MenuMaximizado = valor = "1"
            ElseIf llave.StartsWith(PrefijoLocal, StringComparison.OrdinalIgnoreCase) AndAlso Long.TryParse(valor, local) Then
                _locales(llave.Substring(PrefijoLocal.Length)) = local
            End If
        End Sub

        ''' <summary>Último local con el que entró esa conexión y usuario, si lo hay.</summary>
        Public Function LocalDe(conexion As String) As Long?
            Dim local As Long
            If _locales.TryGetValue(conexion, local) Then Return local
            Return Nothing
        End Function

        Public Sub RecordarLocal(conexion As String, local As Long)
            _locales(conexion) = local
        End Sub

        Public Sub Guardar()
            Dim lineas As New List(Of String) From {
                $"{LlaveMaximizado}={If(MenuMaximizado, 1, 0)}",
                $"{LlaveModulo}={MenuModulo}",
                $"{LlavePerfil}={UltimoPerfil}",
                $"{LlaveUsuario}={UltimoUsuario}"
            }
            lineas.AddRange(_locales.Select(Function(l) $"{PrefijoLocal}{l.Key}={l.Value}"))
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta))
                File.WriteAllLines(_ruta, lineas)
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                ' Solo se pierde la preferencia; ver la nota de la clase.
            End Try
        End Sub

    End Class

End Namespace
