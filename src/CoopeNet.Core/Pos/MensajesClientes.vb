Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Un mensaje de posmensajes: lo que la pantalla del cliente (frmscreen) muestra por turnos.</summary>
    Public NotInheritable Class MensajeCliente
        ''' <summary>0 mientras no se ha guardado.</summary>
        Public Property Codigo As Long
        Public Property Titulo1 As String = ""
        Public Property Titulo2 As String = ""
        Public Property Titulo3 As String = ""
        ''' <summary>NLocal: el nombre del local que se muestra con el mensaje (texto libre).</summary>
        Public Property NombreLocal As String = ""
    End Class

    ''' <summary>
    ''' Mensajes para clientes de SD0502. Migración de SD0502\frmMantMensajes.frm (SD_Guardar, SD_Cargar, Borrar,
    ''' Buscar, cmdAnterior, cmdSiguiente y la velocidad) y de FrmImgsMens.frm (la imagen de cada mensaje).
    ''' </summary>
    ''' <remarks>
    ''' Un mensaje nuevo se graba con Codigo 0 para que el AUTO_INCREMENT le dé número, y se replica esa misma
    ''' sentencia, así que cada local destino le asigna el suyo, igual que en VB6. La imagen vive en posimagenes
    ''' con el código del mensaje como id; al guardarla no se replica (POSSavePicture2DB).
    ''' </remarks>
    Public NotInheritable Class MensajesClientesRepositorio

        Public Const Replica As String = "posmensajes"
        Public Const ReplicaImagenes As String = "posimagenes"
        Public Const ParametroVelocidad As String = "PosVelocMensaj"
        Public Const LargoTexto As Integer = 255
        ''' <summary>FrmImgsMens.cmdAbrir: VB6 rechaza archivos de 256 KB o más.</summary>
        Public Const TamanoMaximoImagen As Integer = 262144
        ''' <summary>Slider1 de frmMantMensajes: de 1 a 10.</summary>
        Public Const VelocidadMinima As Integer = 1
        Public Const VelocidadMaxima As Integer = 10

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        Public Shared Function Recortar(texto As String) As String
            Dim t = If(texto, "").Trim()
            Return If(t.Length > LargoTexto, t.Substring(0, LargoTexto), t)
        End Function

        ''' <summary>El REPLACE de SD_Guardar, con $Local para el replicador. Un mensaje nuevo va con Codigo 0.</summary>
        Friend Shared Function SentenciaGuardar(m As MensajeCliente) As String
            Return "REPLACE INTO posmensajes (Codigo, titulo1, titulo2, titulo3, Nlocal, local) VALUES (" &
                   String.Join(", ", {Q(m.Codigo.ToString(CultureInfo.InvariantCulture)), Q(Recortar(m.Titulo1)), Q(Recortar(m.Titulo2)),
                                      Q(Recortar(m.Titulo3)), Q(Recortar(m.NombreLocal)), "$Local"}) & ")"
        End Function

        Friend Shared Function SentenciaBorrar(codigo As Long) As String
            Return "DELETE FROM posmensajes WHERE codigo = " & codigo.ToString(CultureInfo.InvariantCulture)
        End Function

        Friend Shared Function SentenciaBorrarImagen(codigo As Long) As String
            Return $"DELETE FROM posimagenes WHERE id = '{codigo.ToString(CultureInfo.InvariantCulture)}' AND LOCAL = $Local"
        End Function

        ''' <summary>SD_Cargar: VB6 busca por código sin mirar el local. Nothing si no existe.</summary>
        Public Async Function CargarAsync(codigo As Long, Optional ct As CancellationToken = Nothing) As Task(Of MensajeCliente)
            Const sql As String = "SELECT Codigo, Titulo1, Titulo2, Titulo3, NLocal AS NombreLocal FROM posmensajes WHERE Codigo = @codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of MensajeCliente)(New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' cmdAnterior y cmdSiguiente: el código vecino dentro del local, o 0 si no hay. Desde un mensaje nuevo
        ''' (código 0) Siguiente da el primero.
        ''' </summary>
        Public Async Function VecinoAsync(codigo As Long, siguiente As Boolean, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Dim sql = If(siguiente,
                "SELECT Codigo FROM posmensajes WHERE Codigo > @codigo AND Local = @local ORDER BY Codigo LIMIT 1",
                "SELECT Codigo FROM posmensajes WHERE Codigo < @codigo AND Local = @local ORDER BY Codigo DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(sql, New With {codigo, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar de la barra. Columnas: Código, Título 1 y Título 2.</summary>
        Public Async Function BuscarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT Codigo AS `Código`, Titulo1 AS `Título 1`, Titulo2 AS `Título 2` FROM posmensajes WHERE Local = @local ORDER BY Codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>SD_Guardar: graba, encola la réplica y devuelve el código con que quedó.</summary>
        Public Async Function GuardarAsync(m As MensajeCliente, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Dim sql = SentenciaGuardar(m)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql.Replace("$Local", local.ToString(CultureInfo.InvariantCulture)), cancellationToken:=ct)).ConfigureAwait(False)
                Dim codigo = m.Codigo
                If codigo = 0 Then
                    codigo = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition("SELECT LAST_INSERT_ID()", cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Await Replicador.ReplicarAsync(cn, Replica, sql, local, ct).ConfigureAwait(False)
                Return codigo
            End Using
        End Function

        ''' <summary>
        ''' Borrar de la barra: el mensaje (sin mirar el local, como VB6, y replicado) y su imagen en el local.
        ''' VB6 borraba la imagen con id código&amp;local, que nunca coincide con el id con que la guarda; aquí se
        ''' borra la del mensaje. Esa parte no se replica: la réplica de VB6 tampoco borraba nada.
        ''' </summary>
        Public Async Function BorrarAsync(codigo As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim sql = SentenciaBorrar(codigo)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, Replica, sql, local, ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(SentenciaBorrarImagen(codigo).Replace("$Local", local.ToString(CultureInfo.InvariantCulture)), cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ' ---- FrmImgsMens ----

        ''' <summary>POSLoadPictureFromDB: el contenido de la imagen del mensaje en el local, o Nothing.</summary>
        Public Async Function ImagenAsync(codigo As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Byte())
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Byte())(New CommandDefinition(
                    "SELECT imag FROM posimagenes WHERE local = @local AND id = @id", New With {local, .id = codigo.ToString(CultureInfo.InvariantCulture)}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>POSSavePicture2DB: reemplaza la imagen del mensaje en el local. No se replica.</summary>
        Public Async Function GuardarImagenAsync(codigo As Long, local As Long, contenido As Byte(), Optional ct As CancellationToken = Nothing) As Task
            Dim p = New With {local, .id = codigo.ToString(CultureInfo.InvariantCulture), contenido}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posimagenes WHERE local = @local AND id = @id", p, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("INSERT INTO posimagenes (local, id, imag, fing) VALUES (@local, @id, @contenido, NOW())", p, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>FrmImgsMens.cmdBorrar: borra la imagen en el local y, como VB6, encola el borrado en los demás.</summary>
        Public Async Function BorrarImagenAsync(codigo As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim sql = SentenciaBorrarImagen(codigo)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql.Replace("$Local", local.ToString(CultureInfo.InvariantCulture)), cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, ReplicaImagenes, sql, local, ct).ConfigureAwait(False)
            End Using
        End Function

        ' ---- Velocidad ----

        ''' <summary>Form_Load: PosVelocMensaj del local (o el global), dentro del rango del control.</summary>
        Public Shared Function AjustarVelocidad(valor As Long) As Integer
            Return CInt(Math.Max(VelocidadMinima, Math.Min(VelocidadMaxima, valor)))
        End Function

    End Class

End Namespace
