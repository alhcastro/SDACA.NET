Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports CoopeNet.Actualizacion

''' <summary>
''' Publica versiones de CoopeNet en el almacén de actualizaciones (catálogo coopenet_actualizaciones) y las
''' administra. La clave privada que firma cada versión se guarda cifrada con DPAPI en el perfil de Windows de
''' quien publica; sin ella no se puede publicar, y el lanzador rechaza lo que no esté firmado con ella.
''' </summary>
Public Module Programa

    Private ReadOnly RutaClave As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoopeNet", "publicacion.clave")
    Private ReadOnly Entropia As Byte() = Encoding.UTF8.GetBytes("CoopeNet/publicacion")

    Private Const Ayuda As String =
"Uso: Publicar <comando> [opciones]

  claves                         Crea la clave de firma si falta y muestra la pública.
  claves --exportar <archivo>    Copia la clave privada, sin cifrar, para respaldarla.
  claves --importar <archivo>    Toma una clave privada respaldada (otro equipo de publicación).
  esquema                        Crea el catálogo coopenet_actualizaciones si falta.
  publicar <carpeta> <versión>   Sube la carpeta publicada. Por omisión queda en el canal piloto.
           [--canal piloto|general] [--notas ""texto""]
  liberar <versión>              Pasa la versión al canal general: la reciben todos los equipos.
  retirar <versión>              La desactiva: los equipos vuelven a la anterior.
  reactivar <versión>            La vuelve a activar.
  listar                         Las versiones publicadas.
  equipos                        Qué versión tiene cada equipo.

  --servidor <ip>  --puerto <n>  Por omisión 190.113.126.199:3312."

    Public Function Main(args As String()) As Integer
        Console.OutputEncoding = Encoding.UTF8
        Dim lista = args.ToList()
        Dim servidor = Opcion(lista, "--servidor", ConfiguracionEquipo.ServidorPredeterminado)
        Dim puerto = UInteger.Parse(Opcion(lista, "--puerto", ConfiguracionEquipo.PuertoPredeterminado.ToString()))
        If lista.Count = 0 Then
            Console.WriteLine(Ayuda)
            Return 1
        End If
        Try
            Dim almacen As New AlmacenVersiones(servidor, puerto, esperaSegundos:=20UI)
            Dim ct = Threading.CancellationToken.None
            Select Case lista(0).ToLowerInvariant()
                Case "claves" : Return Claves(lista)
                Case "esquema"
                    almacen.CrearEsquemaAsync(ct).GetAwaiter().GetResult()
                    Console.WriteLine($"Catálogo {AlmacenVersiones.Catalogo} listo en {servidor}:{puerto}.")
                    Return 0
                Case "publicar"
                    If lista.Count < 3 Then Throw New ArgumentException("Falta la carpeta o la versión.")
                    Dim canal = Opcion(lista, "--canal", Canales.Piloto)
                    Dim notas = Opcion(lista, "--notas", "")
                    Return PublicarAsync(almacen, lista(1), lista(2), canal, notas).GetAwaiter().GetResult()
                Case "liberar" : Return Cambiar(almacen, lista, Canales.General, Nothing, "liberada para todos los equipos")
                Case "retirar" : Return Cambiar(almacen, lista, Nothing, False, "retirada: los equipos vuelven a la anterior")
                Case "reactivar" : Return Cambiar(almacen, lista, Nothing, True, "activa otra vez")
                Case "listar" : Return Listar(almacen)
                Case "equipos" : Return Equipos(almacen)
                Case Else
                    Console.WriteLine(Ayuda)
                    Return 1
            End Select
        Catch ex As Exception
            Console.Error.WriteLine($"Error: {ex.Message}")
            Return 2
        End Try
    End Function

    Private Function Opcion(lista As List(Of String), nombre As String, predeterminado As String) As String
        Dim i = lista.FindIndex(Function(a) String.Equals(a, nombre, StringComparison.OrdinalIgnoreCase))
        If i < 0 Then Return predeterminado
        If i = lista.Count - 1 Then Throw New ArgumentException($"Falta el valor de {nombre}.")
        Dim valor = lista(i + 1)
        lista.RemoveRange(i, 2)
        Return valor
    End Function

    ' ---- Claves ----

    Private Function Claves(lista As List(Of String)) As Integer
        Dim exportar = If(lista.Count >= 3 AndAlso lista(1) = "--exportar", lista(2), Nothing)
        Dim importar = If(lista.Count >= 3 AndAlso lista(1) = "--importar", lista(2), Nothing)
        If importar IsNot Nothing Then
            Dim pem = File.ReadAllText(importar)
            Firma.PublicaDe(pem) ' valida que sea una clave
            GuardarClave(pem)
            Console.WriteLine($"Clave importada en {RutaClave}.")
        ElseIf Not File.Exists(RutaClave) Then
            GuardarClave(Firma.CrearClaves().Privada)
            Console.WriteLine($"Clave nueva en {RutaClave}. Respáldela con: Publicar claves --exportar <archivo>")
        End If
        Dim privada = LeerClave()
        If exportar IsNot Nothing Then
            File.WriteAllText(exportar, privada)
            Console.WriteLine($"Clave privada copiada sin cifrar en {exportar}. Guárdela fuera de este equipo y bórrela de aquí.")
        End If
        Dim publica = Firma.PublicaDe(privada)
        Console.WriteLine()
        Console.WriteLine(publica)
        Console.WriteLine(If(Normalizar(publica) = Normalizar(ClavePublicaPublicacion.Pem),
                             "Coincide con la del lanzador.",
                             "NO coincide con ClavePublicaPublicacion.Pem: póngala ahí y arme un instalador nuevo."))
        Return 0
    End Function

    Private Sub GuardarClave(pem As String)
        Directory.CreateDirectory(Path.GetDirectoryName(RutaClave))
        Dim cifrado = ProtectedData.Protect(Encoding.UTF8.GetBytes(pem), Entropia, DataProtectionScope.CurrentUser)
        File.WriteAllBytes(RutaClave, cifrado)
    End Sub

    Private Function LeerClave() As String
        If Not File.Exists(RutaClave) Then Throw New FileNotFoundException("No hay clave de firma en este equipo. Corra: Publicar claves")
        Return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(RutaClave), Entropia, DataProtectionScope.CurrentUser))
    End Function

    Private Function Normalizar(pem As String) As String
        Return pem.Replace(vbCr, "").Trim()
    End Function

    ' ---- Publicar ----

    Private Async Function PublicarAsync(almacen As AlmacenVersiones, carpeta As String, numero As String, canal As String, notas As String) As Task(Of Integer)
        Canales.Interpretar(numero)
        If Not numero.All(Function(c) Char.IsAsciiDigit(c) OrElse c = "."c) Then Throw New ArgumentException("La versión lleva solo números y puntos, como 1.0.2.")
        Dim privada = LeerClave()
        If Normalizar(Firma.PublicaDe(privada)) <> Normalizar(ClavePublicaPublicacion.Pem) Then
            Throw New InvalidOperationException("La clave de este equipo no es la del lanzador: los equipos rechazarían la versión.")
        End If
        Dim ct = Threading.CancellationToken.None
        If (Await almacen.ObtenerAsync(numero, ct)) IsNot Nothing Then Throw New InvalidOperationException($"La versión {numero} ya está publicada. Suba <Version> en Directory.Build.props.")

        Dim m = Manifiesto.DeCarpeta(carpeta, numero)
        Dim total = m.Archivos.Sum(Function(a) a.Tamano)
        Console.WriteLine($"Versión {numero}: {m.Archivos.Count} archivos, {total / 1048576.0:N1} MB.")
        Dim nuevos = 0, subidos = 0L
        For Each a In m.Archivos
            Dim bytes = Await almacen.SubirArchivoAsync(Path.Combine(carpeta, a.Ruta), a.Hash, ct)
            If bytes > 0 Then
                nuevos += 1
                subidos += bytes
                Console.WriteLine($"  subido {a.Ruta} ({bytes / 1024.0:N0} KB comprimido)")
            End If
        Next
        Dim texto = m.ATexto()
        Await almacen.RegistrarVersionAsync(texto, Firma.Firmar(texto, privada), numero, canal, $"{Environment.UserName}@{Environment.MachineName}", notas, ct)
        Console.WriteLine($"Listo: {nuevos} archivos nuevos ({subidos / 1048576.0:N1} MB comprimidos); el resto ya estaba. Canal {Canales.Normalizar(canal)}.")
        If Canales.Normalizar(canal) = Canales.Piloto Then Console.WriteLine($"Cuando esté probada: Publicar liberar {numero}")
        Return 0
    End Function

    Private Function Cambiar(almacen As AlmacenVersiones, lista As List(Of String), canal As String, activa As Boolean?, mensaje As String) As Integer
        If lista.Count < 2 Then Throw New ArgumentException("Falta la versión.")
        If Not almacen.CambiarAsync(lista(1), canal, activa, Threading.CancellationToken.None).GetAwaiter().GetResult() Then
            Console.WriteLine($"No existe la versión {lista(1)}.")
            Return 1
        End If
        Console.WriteLine($"Versión {lista(1)} {mensaje}.")
        Return Listar(almacen)
    End Function

    Private Function Listar(almacen As AlmacenVersiones) As Integer
        Dim versiones = almacen.VersionesAsync(Threading.CancellationToken.None).GetAwaiter().GetResult()
        Dim general = Canales.Elegir(versiones, Canales.General)
        Dim piloto = Canales.Elegir(versiones, Canales.Piloto)
        Console.WriteLine($"{"Versión",-10} {"Canal",-8} {"Estado",-9} {"Publicada",-17} {"Por",-24} Notas")
        For Each v In versiones
            Dim marca = If(v Is general, " <- general", "") & If(v Is piloto, " <- piloto", "")
            Console.WriteLine($"{v.Version,-10} {v.Canal,-8} {If(v.Activa, "activa", "retirada"),-9} {v.Publicada:yyyy-MM-dd HH:mm} {v.Publicador,-24} {v.Notas}{marca}")
        Next
        If versiones.Count = 0 Then Console.WriteLine("(ninguna: los equipos usan la versión instalada)")
        Return 0
    End Function

    Private Function Equipos(almacen As AlmacenVersiones) As Integer
        Console.WriteLine($"{"Equipo",-20} {"Usuario",-16} {"Canal",-8} {"Versión",-10} {"Fecha",-16} Resultado")
        For Each e In almacen.EquiposAsync(Threading.CancellationToken.None).GetAwaiter().GetResult()
            Console.WriteLine($"{e(0),-20} {e(1),-16} {e(2),-8} {e(3),-10} {e(5),-16} {e(4)}")
        Next
        Return 0
    End Function

End Module
