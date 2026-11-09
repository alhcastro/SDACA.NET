Imports System.IO

Namespace Configuracion

    ''' <summary>
    ''' La carpeta de los .ini de CoopeNet: las conexiones (sdcnnNNN.ini), sdconfig.ini, CONFIG.INI y temp. Es de
    ''' todos los usuarios del equipo (C:\ProgramData\CoopeNet) y el instalador le da permiso de modificar.
    ''' </summary>
    ''' <remarks>
    ''' CoopeNet no escribe en la carpeta de SDACA. La primera vez copia de ahí los .ini que le falten, para empezar
    ''' con los mismos perfiles y opciones; después cada sistema tiene los suyos.
    ''' </remarks>
    Public NotInheritable Class CarpetaDatos

        ''' <summary>Donde instala SDACA ERP sus .ini (Conexion.SDACADIR\Bin de VB6).</summary>
        Public Const BinSdaca As String = "C:\Program Files (x86)\SDACA\Bin"

        Private Sub New()
        End Sub

        Public Shared ReadOnly Property Predeterminada As String
            Get
                Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CoopeNet")
            End Get
        End Property

        ''' <summary>
        ''' Crea <paramref name="datos"/> y copia de SDACA los archivos que falten: sdcnn*.ini y sdconfig.ini de
        ''' <paramref name="binSdaca"/>, y CONFIG.INI de su carpeta padre. No reemplaza nada. Devuelve lo copiado; si
        ''' la carpeta no se puede crear, no copia nada y lo dirán los avisos de cada pantalla al guardar.
        ''' </summary>
        Public Shared Function Preparar(datos As String, binSdaca As String) As IReadOnlyList(Of String)
            Dim copiados As New List(Of String)
            Try
                Directory.CreateDirectory(datos)
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                Return copiados
            End Try
            If String.IsNullOrEmpty(binSdaca) OrElse Not Directory.Exists(binSdaca) Then Return copiados
            Dim origenes As New List(Of String)(Directory.GetFiles(binSdaca, "sdcnn*.ini"))
            origenes.Add(Path.Combine(binSdaca, "sdconfig.ini"))
            Dim sdaca = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(binSdaca))
            If Not String.IsNullOrEmpty(sdaca) Then origenes.Add(Path.Combine(sdaca, "CONFIG.INI"))
            For Each origen In origenes
                Dim destino = Path.Combine(datos, Path.GetFileName(origen))
                If Not File.Exists(origen) OrElse File.Exists(destino) Then Continue For
                Try
                    File.Copy(origen, destino, overwrite:=False)
                    copiados.Add(Path.GetFileName(origen))
                Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                    ' Otro usuario lo copió al mismo tiempo o no hay permiso: se sigue con los demás.
                End Try
            Next
            Return copiados
        End Function

    End Class

End Namespace
