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
    ''' Lee y escribe el sdcnnNNN.ini que usa hoy el menú VB6, para que las dos versiones compartan
    ''' los mismos perfiles. Escribe en Windows-1252 y deja intactas las llaves que no toca.
    ''' </summary>
    ''' <remarks>
    ''' La sección [GENERAL] no se lee como perfil a propósito: su clave CNN guarda una contraseña en
    ''' texto plano. Las contraseñas nunca se escriben en este archivo.
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

        ''' <summary>Nombres de todas las secciones del archivo, incluidas [GENERAL] y [USUARIOS].</summary>
        Public Shared Function LeerSecciones(ruta As String) As IReadOnlyList(Of String)
            If Not File.Exists(ruta) Then Return Array.Empty(Of String)()
            Return Secciones(File.ReadAllText(ruta, CodificacionWindows1252()))
        End Function

        Public Shared Function Secciones(texto As String) As IReadOnlyList(Of String)
            Return PartirLineas(texto).Select(AddressOf NombreSeccion).Where(Function(n) n IsNot Nothing).ToList()
        End Function

        ''' <summary>
        ''' Guarda un perfil igual que <c>SD_Perfil_Guardar_Valores</c> de Module01.bas: solo
        ''' catálogo, puerto y servidor. Las demás llaves del perfil (USUARIO, USER::) se conservan.
        ''' La contraseña no se guarda, tampoco la guardaba el original.
        ''' </summary>
        Public Shared Sub GuardarPerfil(ruta As String, nombre As String, servidor As String, puerto As UInteger, catalogo As String)
            Dim texto = LeerTexto(ruta)
            texto = ConValor(texto, nombre, "CATALOGO", catalogo)
            texto = ConValor(texto, nombre, "PUERTO", puerto.ToString())
            texto = ConValor(texto, nombre, "SERVIDOR", servidor)
            EscribirTexto(ruta, texto)
        End Sub

        ''' <summary>
        ''' Quita la sección del perfil, como MnuEli_Click de FrmConfigurarServidor. Si era la
        ''' predeterminada, pasa a serlo el primer perfil que quede (o ninguno).
        ''' </summary>
        Public Shared Sub EliminarPerfil(ruta As String, nombre As String)
            Dim texto = SinSeccion(LeerTexto(ruta), nombre)
            If String.Equals(ValorDe(texto, "GENERAL", "DEFAULT"), nombre, StringComparison.OrdinalIgnoreCase) Then
                Dim primero = Parsear(texto).FirstOrDefault(Function(p) p.Servidor.Length > 0)
                texto = ConValor(texto, "GENERAL", "DEFAULT", If(primero?.Nombre, ""))
            End If
            EscribirTexto(ruta, texto)
        End Sub

        Public Shared Sub FijarValor(ruta As String, seccion As String, llave As String, valor As String)
            EscribirTexto(ruta, ConValor(LeerTexto(ruta), seccion, llave, valor))
        End Sub

        ''' <summary>
        ''' Explica por qué un nombre no sirve para un perfil nuevo, o devuelve "" si sirve. Un
        ''' corchete rompería la sección, y GENERAL, USUARIOS y (Predeterminado) tienen otro uso.
        ''' </summary>
        Public Shared Function ValidarNombre(nombre As String, secciones As IEnumerable(Of String)) As String
            nombre = If(nombre, "").Trim()
            If nombre.Length = 0 Then Return "Indique un nombre para la conexión."
            If nombre.IndexOfAny({"["c, "]"c, "="c, ";"c}) >= 0 Then Return "El nombre no puede llevar corchetes, signo igual ni punto y coma."
            If {"GENERAL", "USUARIOS", "(PREDETERMINADO)"}.Contains(nombre.ToUpperInvariant()) Then Return $"""{nombre}"" es un nombre reservado."
            If secciones.Contains(nombre, StringComparer.OrdinalIgnoreCase) Then Return $"Ya existe una conexión llamada ""{nombre}""."
            Return ""
        End Function

        ''' <summary>
        ''' Cambia o agrega una llave sin tocar el resto del archivo, como WritePrivateProfileString:
        ''' conserva el nombre original de la llave y agrega al final de la sección (o crea la sección
        ''' al final del archivo).
        ''' </summary>
        Public Shared Function ConValor(texto As String, seccion As String, llave As String, valor As String) As String
            Dim lineas = PartirLineas(texto)
            Dim inicio = lineas.FindIndex(Function(l) Coincide(NombreSeccion(l), seccion))
            If inicio < 0 Then
                lineas.Add($"[{seccion}]")
                lineas.Add($"{llave}={valor}")
                Return Unir(lineas)
            End If
            Dim fin = FinDeSeccion(lineas, inicio)
            Dim ultima = inicio
            For i = inicio + 1 To fin - 1
                Dim linea = lineas(i).Trim()
                If linea.Length = 0 Then Continue For
                ultima = i
                Dim igual = linea.IndexOf("="c)
                If igual > 0 AndAlso Coincide(linea.Substring(0, igual).Trim(), llave) Then
                    lineas(i) = $"{linea.Substring(0, igual).Trim()}={valor}"
                    Return Unir(lineas)
                End If
            Next
            lineas.Insert(ultima + 1, $"{llave}={valor}")
            Return Unir(lineas)
        End Function

        Public Shared Function SinSeccion(texto As String, seccion As String) As String
            Dim lineas = PartirLineas(texto)
            Dim inicio = lineas.FindIndex(Function(l) Coincide(NombreSeccion(l), seccion))
            If inicio < 0 Then Return Unir(lineas)
            lineas.RemoveRange(inicio, FinDeSeccion(lineas, inicio) - inicio)
            Return Unir(lineas)
        End Function

        Private Shared Function ValorDe(texto As String, seccion As String, llave As String) As String
            Dim dentro = False
            For Each crudo In PartirLineas(texto)
                Dim nombre = NombreSeccion(crudo)
                If nombre IsNot Nothing Then
                    dentro = Coincide(nombre, seccion)
                    Continue For
                End If
                Dim linea = crudo.Trim()
                Dim igual = linea.IndexOf("="c)
                If dentro AndAlso igual > 0 AndAlso Coincide(linea.Substring(0, igual).Trim(), llave) Then Return linea.Substring(igual + 1).Trim()
            Next
            Return ""
        End Function

        Private Shared Function NombreSeccion(linea As String) As String
            Dim t = linea.Trim()
            If t.Length < 2 OrElse Not t.StartsWith("[") OrElse Not t.EndsWith("]") Then Return Nothing
            Return t.Substring(1, t.Length - 2).Trim()
        End Function

        Private Shared Function FinDeSeccion(lineas As List(Of String), inicio As Integer) As Integer
            Dim fin = lineas.FindIndex(inicio + 1, Function(l) NombreSeccion(l) IsNot Nothing)
            Return If(fin < 0, lineas.Count, fin)
        End Function

        Private Shared Function Coincide(a As String, b As String) As Boolean
            Return a IsNot Nothing AndAlso String.Equals(a, b.Trim(), StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function PartirLineas(texto As String) As List(Of String)
            Dim lista = If(texto, "").Split({vbCrLf, vbLf}, StringSplitOptions.None).ToList()
            If lista.Count > 0 AndAlso lista(lista.Count - 1).Length = 0 Then lista.RemoveAt(lista.Count - 1)
            Return lista
        End Function

        Private Shared Function Unir(lineas As List(Of String)) As String
            Return If(lineas.Count = 0, "", String.Join(vbCrLf, lineas) & vbCrLf)
        End Function

        Private Shared Function LeerTexto(ruta As String) As String
            Return If(File.Exists(ruta), File.ReadAllText(ruta, CodificacionWindows1252()), "")
        End Function

        Private Shared Sub EscribirTexto(ruta As String, texto As String)
            File.WriteAllText(ruta, texto, CodificacionWindows1252())
        End Sub

        Friend Shared Function CodificacionWindows1252() As Encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
            Return Encoding.GetEncoding(1252)
        End Function

    End Class

End Namespace
