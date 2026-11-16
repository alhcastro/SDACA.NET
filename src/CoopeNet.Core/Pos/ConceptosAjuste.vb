Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>cmbMovimi de frmMantAjustes: el movimiento del ajuste. El valor es posajustesmensajes.Accion.</summary>
    Public Enum MovimientoAjuste
        Salida = 0
        Entrada = 1
    End Enum

    ''' <summary>Un concepto de ajuste de inventario (posajustesmensajes).</summary>
    Public NotInheritable Class ConceptoAjuste
        Public Property Accion As MovimientoAjuste
        ''' <summary>0 mientras no se ha guardado.</summary>
        Public Property Codigo As Integer
        Public Property Descripcion As String = ""
    End Class

    ''' <summary>
    ''' Conceptos de los ajustes de inventario. Migración de SD0502\frmMantAjustes.frm (SD_Guardar, SD_Cargar, Borrar
    ''' y Buscar). La tabla no tiene local ni se replica.
    ''' </summary>
    ''' <remarks>
    ''' VB6 solo muestra los códigos de 200 en adelante (desde 2013; los anteriores quedan ocultos) y un nuevo toma el
    ''' máximo + 1 del movimiento. Solo lo abren los usuarios 0 y 14.
    ''' </remarks>
    Public NotInheritable Class ConceptosAjusteRepositorio

        Public Const LargoDescripcion As Integer = 128
        Public Const CodigoMinimo As Integer = 200

        ''' <summary>Los usuarios que abren la opción en VB6 (Clase.cls, caso 070).</summary>
        Public Shared Function PuedeAbrir(uid As Long) As Boolean
            Return uid = 0 OrElse uid = 14
        End Function

        Public Shared ReadOnly Property Movimientos As IReadOnlyList(Of String) = {"SALIDA", "ENTRADA"}

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>SDTDescri_LostFocus la pasa a mayúsculas; además se recorta al largo de la columna.</summary>
        Public Shared Function Normalizar(descripcion As String) As String
            Dim d = If(descripcion, "").Trim().ToUpper(CultureInfo.CurrentCulture)
            Return If(d.Length > LargoDescripcion, d.Substring(0, LargoDescripcion), d)
        End Function

        Private Const Columnas As String = "SELECT Accion, Codigo, Descri AS Descripcion FROM posajustesmensajes WHERE Accion = @accion AND Codigo >= 200"

        Public Async Function CargarAsync(accion As MovimientoAjuste, codigo As Integer, Optional ct As CancellationToken = Nothing) As Task(Of ConceptoAjuste)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of ConceptoAjuste)(New CommandDefinition(
                    Columnas & " AND Codigo = @codigo LIMIT 1", New With {.accion = CInt(accion), codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>cmdAnterior y cmdProximo: el vecino dentro del movimiento, o Nothing en el extremo.</summary>
        Public Async Function VecinoAsync(accion As MovimientoAjuste, codigo As Integer, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of ConceptoAjuste)
            Dim sql = Columnas & If(siguiente, " AND Codigo > @codigo ORDER BY Codigo LIMIT 1", " AND Codigo < @codigo ORDER BY Codigo DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of ConceptoAjuste)(New CommandDefinition(sql, New With {.accion = CInt(accion), codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar de la barra. Columnas: Código y Descripción.</summary>
        Public Async Function BuscarAsync(accion As MovimientoAjuste, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT Codigo AS `Código`, Descri AS `Descripción` FROM posajustesmensajes WHERE Accion = @accion AND Codigo >= 200 ORDER BY Codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {.accion = CInt(accion)}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>SD_Guardar: uno nuevo toma el siguiente código del movimiento. Devuelve el código con que quedó.</summary>
        Public Async Function GuardarAsync(c As ConceptoAjuste, uid As Long, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim codigo = c.Codigo
                If codigo = 0 Then
                    codigo = Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition(
                        "SELECT IFNULL(MAX(Codigo) + 1, 1) FROM posajustesmensajes WHERE Accion = @accion", New With {.accion = CInt(c.Accion)}, cancellationToken:=ct)).ConfigureAwait(False)
                    If codigo < CodigoMinimo Then codigo = CodigoMinimo
                End If
                Await cn.ExecuteAsync(New CommandDefinition(
                    "REPLACE INTO posajustesmensajes (Accion, Codigo, Descri, Usuari) VALUES (@accion, @codigo, @descri, @usuari)",
                    New With {.accion = CInt(c.Accion), codigo, .descri = Normalizar(c.Descripcion), .usuari = uid.ToString(CultureInfo.InvariantCulture)},
                    cancellationToken:=ct)).ConfigureAwait(False)
                Return codigo
            End Using
        End Function

        Public Async Function BorrarAsync(accion As MovimientoAjuste, codigo As Integer, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posajustesmensajes WHERE Codigo = @codigo AND Accion = @accion",
                                                            New With {.accion = CInt(accion), codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
