Imports System.Globalization
Imports System.IO

Namespace Configuracion

    ''' <summary>Último tamaño de una pantalla, medido a 96 DPI, y si quedó maximizada.</summary>
    Public Structure EstadoVentana
        Public Ancho As Integer
        Public Alto As Integer
        Public Maximizada As Boolean
    End Structure

    ''' <summary>
    ''' Lo que recuerdan las pantallas redimensionables: una línea CLAVE=ancho,alto,maximizada por pantalla, en la
    ''' carpeta del usuario de Windows, junto a preferencias.ini. Como en <see cref="Preferencias"/>, un archivo
    ''' que no se puede leer o escribir solo hace perder lo recordado.
    ''' </summary>
    Public NotInheritable Class EstadosVentana

        Private ReadOnly _ruta As String
        Private ReadOnly _estados As New Dictionary(Of String, EstadoVentana)(StringComparer.OrdinalIgnoreCase)
        Private Shared ReadOnly Candado As New Object()

        Private Sub New(ruta As String)
            _ruta = ruta
        End Sub

        Public Shared ReadOnly Property RutaPredeterminada As String
            Get
                Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SDACA", "ventanas.ini")
            End Get
        End Property

        Public Shared Function Cargar(ruta As String) As EstadosVentana
            Dim estados As New EstadosVentana(ruta)
            Try
                If File.Exists(ruta) Then
                    For Each linea In File.ReadAllLines(ruta)
                        estados.Interpretar(linea)
                    Next
                End If
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                Return New EstadosVentana(ruta)
            End Try
            Return estados
        End Function

        Private Sub Interpretar(linea As String)
            Dim igual = linea.LastIndexOf("="c)
            If igual <= 0 Then Return
            Dim partes = linea.Substring(igual + 1).Split(","c)
            Dim ancho, alto As Integer
            If partes.Length <> 3 OrElse Not Integer.TryParse(partes(0), NumberStyles.Integer, CultureInfo.InvariantCulture, ancho) OrElse
               Not Integer.TryParse(partes(1), NumberStyles.Integer, CultureInfo.InvariantCulture, alto) OrElse ancho <= 0 OrElse alto <= 0 Then Return
            _estados(linea.Substring(0, igual).Trim()) = New EstadoVentana With {.Ancho = ancho, .Alto = alto, .Maximizada = partes(2).Trim() = "1"}
        End Sub

        Public Function Obtener(clave As String) As EstadoVentana?
            Dim estado As EstadoVentana
            If _estados.TryGetValue(clave, estado) Then Return estado
            Return Nothing
        End Function

        Public Sub Recordar(clave As String, estado As EstadoVentana)
            If String.IsNullOrWhiteSpace(clave) OrElse clave.Contains("="c) OrElse estado.Ancho <= 0 OrElse estado.Alto <= 0 Then Return
            _estados(clave.Trim()) = estado
        End Sub

        Public Function Lineas() As IEnumerable(Of String)
            Return _estados.OrderBy(Function(e) e.Key, StringComparer.OrdinalIgnoreCase).
                Select(Function(e) String.Format(CultureInfo.InvariantCulture, "{0}={1},{2},{3}", e.Key, e.Value.Ancho, e.Value.Alto, If(e.Value.Maximizada, 1, 0)))
        End Function

        Public Sub Guardar()
            Try
                SyncLock Candado
                    Directory.CreateDirectory(Path.GetDirectoryName(_ruta))
                    File.WriteAllLines(_ruta, Lineas())
                End SyncLock
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                ' Solo se pierde lo recordado; ver la nota de la clase.
            End Try
        End Sub

    End Class

End Namespace
