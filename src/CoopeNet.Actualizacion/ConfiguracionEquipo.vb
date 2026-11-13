Imports System.IO

''' <summary>
''' De dónde se actualiza este equipo y en qué canal está. Se lee de <c>C:\ProgramData\CoopeNet\actualizacion.ini</c>;
''' sin el archivo o sin una llave, vale lo predeterminado: el servidor central y el canal general.
''' </summary>
''' <example>
''' [ACTUALIZACION]
''' SERVIDOR=190.113.126.199
''' PUERTO=3312
''' CANAL=piloto
''' ; ACTIVA=0 apaga las actualizaciones en este equipo: arranca la versión que tenga.
''' </example>
Public NotInheritable Class ConfiguracionEquipo

    Public Const ServidorPredeterminado As String = "190.113.126.199"
    Public Const PuertoPredeterminado As UInteger = 3312UI

    Public Property Servidor As String = ServidorPredeterminado
    Public Property Puerto As UInteger = PuertoPredeterminado
    Public Property Canal As String = Canales.General
    Public Property Activa As Boolean = True

    Public Shared ReadOnly Property RutaPredeterminada As String
        Get
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CoopeNet", "actualizacion.ini")
        End Get
    End Property

    Public Shared Function Cargar(ruta As String) As ConfiguracionEquipo
        Dim c As New ConfiguracionEquipo()
        If Not File.Exists(ruta) Then Return c
        Dim dentro = False
        For Each crudo In File.ReadAllLines(ruta)
            Dim linea = crudo.Trim()
            If linea.Length = 0 OrElse linea.StartsWith(";") Then Continue For
            If linea.StartsWith("[") Then
                dentro = String.Equals(linea, "[ACTUALIZACION]", StringComparison.OrdinalIgnoreCase)
                Continue For
            End If
            Dim igual = linea.IndexOf("="c)
            If Not dentro OrElse igual <= 0 Then Continue For
            Dim valor = linea.Substring(igual + 1).Trim()
            Select Case linea.Substring(0, igual).Trim().ToUpperInvariant()
                Case "SERVIDOR" : If valor.Length > 0 Then c.Servidor = valor
                Case "PUERTO"
                    Dim p As UInteger
                    If UInteger.TryParse(valor, p) AndAlso p > 0UI Then c.Puerto = p
                Case "CANAL" : c.Canal = Canales.Normalizar(valor)
                Case "ACTIVA" : c.Activa = valor <> "0"
            End Select
        Next
        Return c
    End Function

End Class
