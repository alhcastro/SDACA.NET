Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>
    ''' Un billete o moneda (posmonedas): su valor, su nombre y el tipo de moneda de gatmonedas al que pertenece.
    ''' frmMonedas, la pantalla táctil de cobro, muestra los de colones (tipo 0).
    ''' </summary>
    Public NotInheritable Class Denominacion
        Public Property Moneda As Double
        Public Property Descripcion As String = ""
        Public Property Tipo As Integer

        ''' <summary>El valor como texto, como lo mostraba sdtCodigo: con punto y sin ceros de más.</summary>
        Public ReadOnly Property Codigo As String
            Get
                Return DenominacionesRepositorio.Texto(Moneda)
            End Get
        End Property
    End Class

    ''' <summary>Un tipo de moneda de gatmonedas, para cmbTipo.</summary>
    Public NotInheritable Class TipoDenominacion
        Public Property Tipo As Integer
        Public Property Descripcion As String = ""

        Public Overrides Function ToString() As String
            Return $"{Tipo} - {If(Descripcion = "", "(no está en Monedas de SD0001)", Descripcion)}"
        End Function
    End Class

    ''' <summary>
    ''' Catálogo de monedas de SD0502. Migración de SD0502\frmMantMonedas.frm (SDCargando, SD_ComboTipos, la barra y
    ''' la imagen de cmdBuscar). posmonedas no se replica.
    ''' </summary>
    ''' <remarks>
    ''' La imagen va en posimagartic con id = el valor como texto y Consec 1, igual que en VB6, para que la lea quien
    ''' la leía. Esa tabla es la de imágenes de artículos: VB6 borraba con <c>id = valor</c> numérico, que también
    ''' alcanza a "05" o "0005", y al guardar podía pisar la imagen de un artículo con ese código. Aquí se borra solo
    ''' el id exacto y se rechaza el valor que es código de un artículo. VB6 además navegaba sin filtrar el local.
    ''' </remarks>
    Public NotInheritable Class DenominacionesRepositorio

        Public Const LargoDescripcion As Integer = 64
        Public Const TamanoMaximoImagen As Integer = 262144

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Shared Function Texto(moneda As Double) As String
            Return moneda.ToString("0.##########", Invariante)
        End Function

        ''' <summary>sdtDescricion_LostFocus la pasa a mayúsculas; además se recorta al largo de la columna.</summary>
        Public Shared Function Normalizar(descripcion As String) As String
            Dim d = If(descripcion, "").Trim().ToUpper(CultureInfo.CurrentCulture)
            Return If(d.Length > LargoDescripcion, d.Substring(0, LargoDescripcion), d)
        End Function

        ''' <summary>El valor que se escribe en sdtCodigo: positivo, con punto o coma decimal. Nothing si no sirve.</summary>
        Public Shared Function Leer(texto As String) As Double?
            Dim t = If(texto, "").Trim().Replace(","c, "."c)
            Dim valor As Double
            If Not Double.TryParse(t, NumberStyles.AllowDecimalPoint, Invariante, valor) OrElse valor <= 0 Then Return Nothing
            Return valor
        End Function

        ''' <summary>
        ''' SD_ComboTipos, más los tipos que usa posmonedas y ya no están en gatmonedas (en VB6 esas monedas no se podían
        ''' elegir), con la descripción vacía.
        ''' </summary>
        Public Async Function TiposAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of TipoDenominacion))
            Const sql As String = "SELECT moneda AS Tipo, descripcion AS Descripcion FROM gatmonedas " &
                "UNION SELECT DISTINCT TipMoneda, '' FROM posmonedas WHERE TipMoneda NOT IN (SELECT moneda FROM gatmonedas) ORDER BY Tipo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of TipoDenominacion)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        Private Const Columnas As String = "SELECT Moneda, Descrip AS Descripcion, TipMoneda AS Tipo FROM posmonedas WHERE local = @local AND TipMoneda = @tipo"

        ''' <summary>SDCargando 0.</summary>
        Public Async Function CargarAsync(tipo As Integer, moneda As Double, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Denominacion)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of Denominacion)(New CommandDefinition(
                    Columnas & " AND Moneda = @moneda LIMIT 1", New With {tipo, moneda, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>SDCargando 1 y 2: la vecina dentro del tipo, o Nothing en el extremo.</summary>
        Public Async Function VecinaAsync(tipo As Integer, moneda As Double, local As Long, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of Denominacion)
            Dim sql = Columnas & If(siguiente, " AND Moneda > @moneda ORDER BY Moneda LIMIT 1", " AND Moneda < @moneda ORDER BY Moneda DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of Denominacion)(New CommandDefinition(sql, New With {tipo, moneda, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar de la barra: las del local y el tipo. Columnas: Moneda y Descripción.</summary>
        Public Async Function BuscarAsync(tipo As Integer, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT Moneda, Descrip AS `Descripción` FROM posmonedas WHERE local = @local AND TipMoneda = @tipo ORDER BY Moneda"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {tipo, local}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>¿Ya hay una con ese valor en el local, de cualquier tipo? La llave es Moneda y Local.</summary>
        Public Async Function ExisteAsync(moneda As Double, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Denominacion)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of Denominacion)(New CommandDefinition(
                    "SELECT Moneda, Descrip AS Descripcion, TipMoneda AS Tipo FROM posmonedas WHERE local = @local AND Moneda = @moneda LIMIT 1",
                    New With {moneda, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Guardar de la barra: el REPLACE de VB6.</summary>
        Public Async Function GuardarAsync(d As Denominacion, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(
                    "REPLACE INTO posmonedas (Moneda, Descrip, TipMoneda, FM, Usuario, Local) VALUES (@moneda, @descripcion, @tipo, NOW(), @uid, @local)",
                    New With {.moneda = d.Moneda, .descripcion = Normalizar(d.Descripcion), .tipo = d.Tipo, uid, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Borrar de la barra: la del local y su imagen, si el valor no es código de un artículo.</summary>
        Public Async Function BorrarAsync(moneda As Double, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posmonedas WHERE Moneda = @moneda AND Local = @local", New With {moneda, local}, cancellationToken:=ct)).ConfigureAwait(False)
                Dim id = Texto(moneda)
                If Not Await EsArticuloAsync(cn, id, ct).ConfigureAwait(False) Then
                    Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posimagartic WHERE id = @id", New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
                End If
            End Using
        End Function

        Private Shared Async Function EsArticuloAsync(cn As MySqlConnector.MySqlConnection, id As String, ct As CancellationToken) As Task(Of Boolean)
            Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(
                "SELECT COUNT(*) FROM posarticulos WHERE CodigoArt = @id", New With {id}, cancellationToken:=ct)).ConfigureAwait(False) > 0
        End Function

        ''' <summary>¿El valor como texto es el código de un artículo? Entonces su imagen es la del artículo.</summary>
        Public Async Function EsArticuloAsync(moneda As Double, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await EsArticuloAsync(cn, Texto(moneda), ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>POSLoadPictArticF: la imagen, o Nothing. Si el valor es código de un artículo no se muestra la de él.</summary>
        Public Async Function ImagenAsync(moneda As Double, Optional ct As CancellationToken = Nothing) As Task(Of Byte())
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim id = Texto(moneda)
                If Await EsArticuloAsync(cn, id, ct).ConfigureAwait(False) Then Return Nothing
                Return Await cn.QueryFirstOrDefaultAsync(Of Byte())(New CommandDefinition(
                    "SELECT imag FROM posimagartic WHERE id = @id ORDER BY Consec LIMIT 1", New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' POSSavePictArtic de frmMantMonedas: reemplaza todas las imágenes del id por esta, con Consec 1 y la descripción.
        ''' Devuelve False sin tocar nada si el valor es código de un artículo.
        ''' </summary>
        Public Async Function GuardarImagenAsync(moneda As Double, descripcion As String, imagen As Byte(), uid As Long, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim id = Texto(moneda)
                If Await EsArticuloAsync(cn, id, ct).ConfigureAwait(False) Then Return False
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posimagartic WHERE id = @id", New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(
                    "INSERT INTO posimagartic (id, Consec, Descr, imag, fact, uid) VALUES (@id, 1, @descr, @imagen, NOW(), @uid)",
                    New With {id, .descr = Normalizar(descripcion), imagen, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Return True
            End Using
        End Function

    End Class

End Namespace
