Imports System.Data
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>
    ''' cmbMovim de frmMantJustif: el movimiento del POS que pide la justificación. El valor es posvitacmensajes.Accion.
    ''' </summary>
    Public Enum MovimientoJustificado
        ModificarCantidad = 0
        EliminarLinea = 1
        CerrarFacturaSinGuardar = 2
        RetornarArticulos = 3
        AnularFactura = 4
        ModificarPrecios = 5
    End Enum

    ''' <summary>Una justificación (posvitacmensajes): el motivo que el cajero elige al hacer ese movimiento.</summary>
    Public NotInheritable Class Justificacion
        Public Property Accion As MovimientoJustificado
        ''' <summary>0 mientras no se ha guardado.</summary>
        Public Property Codigo As Integer
        Public Property Descripcion As String = ""
    End Class

    ''' <summary>
    ''' Justificaciones de anulaciones y devoluciones de SD0502. Migración de SD0502\frmMantJustif.frm (SD_Guardar,
    ''' SD_Cargar, Borrar y Buscar). La tabla no tiene local ni se replica: cada base tiene su lista.
    ''' </summary>
    Public NotInheritable Class JustificacionesRepositorio

        Public Const LargoDescripcion As Integer = 128

        ''' <summary>Los textos de cmbMovim, en el orden de su valor.</summary>
        Public Shared ReadOnly Property Movimientos As IReadOnlyList(Of String) = {
            "0 - Modificar cantidad", "1 - Eliminar línea", "2 - Cerrar factura sin guardar",
            "3 - Retornar artículos", "4 - Anular factura", "5 - Modificar precios"}

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>SDTDescri_LostFocus la pasa a mayúsculas; además se recorta al largo de la columna.</summary>
        Public Shared Function Normalizar(descripcion As String) As String
            Dim d = If(descripcion, "").Trim().ToUpper(Globalization.CultureInfo.CurrentCulture)
            Return If(d.Length > LargoDescripcion, d.Substring(0, LargoDescripcion), d)
        End Function

        Private Const Columnas As String = "SELECT Accion, Codigo, Descri AS Descripcion FROM posvitacmensajes WHERE Accion = @accion"

        ''' <summary>SD_Cargar con Consulta. Nothing si no existe.</summary>
        Public Async Function CargarAsync(accion As MovimientoJustificado, codigo As Integer, Optional ct As CancellationToken = Nothing) As Task(Of Justificacion)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of Justificacion)(New CommandDefinition(
                    Columnas & " AND Codigo = @codigo LIMIT 1", New With {.accion = CInt(accion), codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>SD_Cargar con Siguient o Anterior: la vecina dentro del movimiento, o Nothing en el extremo.</summary>
        Public Async Function VecinaAsync(accion As MovimientoJustificado, codigo As Integer, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of Justificacion)
            Dim sql = Columnas & If(siguiente, " AND Codigo > @codigo ORDER BY Codigo LIMIT 1", " AND Codigo < @codigo ORDER BY Codigo DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of Justificacion)(New CommandDefinition(sql, New With {.accion = CInt(accion), codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar de la barra: las del movimiento. Columnas: Código y Descripción.</summary>
        Public Async Function BuscarAsync(accion As MovimientoJustificado, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT Codigo AS `Código`, Descri AS `Descripción` FROM posvitacmensajes WHERE Accion = @accion ORDER BY Codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {.accion = CInt(accion)}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>
        ''' SD_Guardar: una nueva toma el siguiente código del movimiento. Devuelve el código con que quedó.
        ''' </summary>
        Public Async Function GuardarAsync(j As Justificacion, uid As Long, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim codigo = j.Codigo
                If codigo = 0 Then
                    codigo = Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition(
                        "SELECT IFNULL(MAX(Codigo) + 1, 1) FROM posvitacmensajes WHERE Accion = @accion", New With {.accion = CInt(j.Accion)}, cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Await cn.ExecuteAsync(New CommandDefinition(
                    "REPLACE INTO posvitacmensajes (Accion, Codigo, Descri, Usuari) VALUES (@accion, @codigo, @descri, @usuari)",
                    New With {.accion = CInt(j.Accion), codigo, .descri = Normalizar(j.Descripcion), .usuari = uid.ToString(Globalization.CultureInfo.InvariantCulture)},
                    cancellationToken:=ct)).ConfigureAwait(False)
                Return codigo
            End Using
        End Function

        Public Async Function BorrarAsync(accion As MovimientoJustificado, codigo As Integer, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posvitacmensajes WHERE Codigo = @codigo AND Accion = @accion",
                                                            New With {.accion = CInt(accion), codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
