Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una moneda del POS (postipomoneda).</summary>
    Public NotInheritable Class TipoMonedaPos
        ''' <summary>0 mientras no se ha guardado.</summary>
        Public Property Tipo As Integer
        Public Property Descripcion As String = ""
        Public Overrides Function ToString() As String
            Return $"{Tipo} - {Descripcion}"
        End Function
    End Class

    ''' <summary>
    ''' Tipos de moneda y tipo de cambio del POS. Migración de SD0502\frmtipomonedas.frm y frmtipocambio.frm. Son
    ''' tablas propias del POS (postipomoneda y postipocambio), distintas de gatmonedas y gattipocambio de SD0001.
    ''' </summary>
    ''' <remarks>
    ''' postipomoneda no tiene local ni se replica. postipocambio tampoco tiene local; se replica a los locales de
    ''' gatreplicadef. Su llave es solo la fecha, así que un día guarda un único cambio aunque haya varias monedas:
    ''' con dos monedas, la última que se guarda reemplaza a la otra. Así está en la base y en VB6.
    ''' </remarks>
    Public NotInheritable Class MonedasPosRepositorio

        Public Const LargoDescripcion As Integer = 24
        Public Const ReplicaCambio As String = "postipocambio"

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>SDTDescri_LostFocus la pasa a mayúsculas; además se recorta al largo de definic.</summary>
        Public Shared Function Normalizar(descripcion As String) As String
            Dim d = If(descripcion, "").Trim().ToUpper(CultureInfo.CurrentCulture)
            Return If(d.Length > LargoDescripcion, d.Substring(0, LargoDescripcion), d)
        End Function

        ' ---- frmtipomonedas ----

        Private Const Columnas As String = "SELECT tipo AS Tipo, definic AS Descripcion FROM postipomoneda"

        Public Async Function MonedasAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of TipoMonedaPos))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of TipoMonedaPos)(New CommandDefinition(Columnas & " ORDER BY tipo", cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        Public Async Function CargarAsync(tipo As Integer, Optional ct As CancellationToken = Nothing) As Task(Of TipoMonedaPos)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of TipoMonedaPos)(New CommandDefinition(Columnas & " WHERE tipo = @tipo", New With {tipo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>cmdAnterior y cmdProximo: la vecina, o Nothing en el extremo.</summary>
        Public Async Function VecinaAsync(tipo As Integer, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of TipoMonedaPos)
            Dim sql = Columnas & If(siguiente, " WHERE tipo > @tipo ORDER BY tipo LIMIT 1", " WHERE tipo < @tipo ORDER BY tipo DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of TipoMonedaPos)(New CommandDefinition(sql, New With {tipo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar de la barra. Columnas: Tipo y Definición.</summary>
        Public Async Function BuscarAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition("SELECT tipo AS Tipo, definic AS `Definición` FROM postipomoneda ORDER BY tipo", cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>SD_Guardar: una nueva toma el siguiente tipo. Devuelve el tipo con que quedó.</summary>
        Public Async Function GuardarAsync(m As TipoMonedaPos, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim tipo = m.Tipo
                If tipo = 0 Then
                    tipo = Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition("SELECT IFNULL(MAX(tipo) + 1, 1) FROM postipomoneda", cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Await cn.ExecuteAsync(New CommandDefinition("REPLACE INTO postipomoneda (tipo, definic, fechacreacion) VALUES (@tipo, @definic, NULL)",
                                                            New With {tipo, .definic = Normalizar(m.Descripcion)}, cancellationToken:=ct)).ConfigureAwait(False)
                Return tipo
            End Using
        End Function

        ''' <summary>Borrar de la barra. Como en VB6, los tipos de cambio de esa moneda quedan.</summary>
        Public Async Function BorrarAsync(tipo As Integer, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM postipomoneda WHERE tipo = @tipo", New With {tipo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ' ---- frmtipocambio ----

        ''' <summary>El cambio de la moneda en ese día, o 0 si no hay.</summary>
        Public Async Function CambioAsync(fecha As Date, tipo As Integer, Optional ct As CancellationToken = Nothing) As Task(Of Double)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Double)(New CommandDefinition(
                    "SELECT monto FROM postipocambio WHERE fecha = @fecha AND tipomoneda = @tipo", New With {.fecha = fecha.Date, tipo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>El REPLACE de cmdAceptar, con el monto en formato invariante para la réplica.</summary>
        Friend Shared Function SentenciaCambio(fecha As Date, monto As Double, tipo As Integer, uid As Long) As String
            Return "REPLACE INTO postipocambio VALUES ('" & fecha.Date.ToString("yyyy-MM-dd HH:mm:ss", Invariante) & "'," &
                   monto.ToString("R", Invariante) & "," & tipo.ToString(Invariante) & "," & uid.ToString(Invariante) & ",NULL)"
        End Function

        ''' <summary>cmdAceptar: graba el cambio del día y lo encola para los demás locales.</summary>
        Public Async Function GuardarCambioAsync(fecha As Date, monto As Double, tipo As Integer, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim sql = SentenciaCambio(fecha, monto, tipo, uid)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, ReplicaCambio, sql, local, ct).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' sdtCambio acepta números con punto o coma decimal. Nothing si el texto no es un monto válido.
        ''' </summary>
        Public Shared Function LeerMonto(texto As String) As Double?
            Dim t = If(texto, "").Trim().Replace(","c, "."c)
            Dim valor As Double
            If t.Length = 0 OrElse Not Double.TryParse(t, NumberStyles.AllowDecimalPoint, Invariante, valor) Then Return Nothing
            Return valor
        End Function

    End Class

End Namespace
