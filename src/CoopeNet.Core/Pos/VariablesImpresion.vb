Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una variable de impresión (posvariables_imp): la clave que un formato usa y qué imprime.</summary>
    Public NotInheritable Class VariableImpresion
        ''' <summary>0 mientras no se ha guardado.</summary>
        Public Property Codigo As Integer
        Public Property Parametro As String = ""
        Public Property Descripcion As String = ""
        Public Property Valor As String = ""
        Public Property FC As Date?
    End Class

    ''' <summary>
    ''' Variables de impresión para facturas. Migración de SD0502\FACPOS\frmVariables_Imp.frm, el botón Variables de
    ''' Propiedades de facturación. Es una lista de referencia: la impresión no la lee. No se replica (VB6 llamaba al
    ''' replicador, pero posvariables_imp no tiene destinos en gatreplicadef).
    ''' </summary>
    Public NotInheritable Class VariablesImpresionRepositorio

        Public Const LargoParametro As Integer = 10
        Public Const LargoTexto As Integer = 64

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>Los LostFocus de VB6 pasan los tres textos a mayúsculas.</summary>
        Public Shared Function Normalizar(texto As String, largo As Integer) As String
            Dim t = If(texto, "").Trim().ToUpper(CultureInfo.CurrentCulture)
            Return If(t.Length > largo, t.Substring(0, largo), t)
        End Function

        Private Const Columnas As String = "SELECT Codigo, Parametro, Descripcion, Valor, FC FROM posvariables_imp"

        Public Async Function CargarAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task(Of VariableImpresion)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of VariableImpresion)(New CommandDefinition(Columnas & " WHERE Codigo = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function VecinaAsync(codigo As Integer, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of VariableImpresion)
            Dim sql = Columnas & If(siguiente, " WHERE Codigo > @codigo ORDER BY Codigo LIMIT 1", " WHERE Codigo < @codigo ORDER BY Codigo DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of VariableImpresion)(New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar: Código, Parámetro, Descripción y Valor.</summary>
        Public Async Function BuscarAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT Codigo AS `Código`, Parametro AS `Parámetro`, Descripcion AS `Descripción`, Valor FROM posvariables_imp ORDER BY Codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>SD_Imprimir: todas, ordenadas por descripción.</summary>
        Public Async Function ListarAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of VariableImpresion))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of VariableImpresion)(New CommandDefinition(Columnas & " ORDER BY Descripcion", cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>SD_Guardar: una nueva toma el siguiente código y se crea ahora. Devuelve el código.</summary>
        Public Async Function GuardarAsync(v As VariableImpresion, uid As Long, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim codigo = v.Codigo
                If codigo = 0 Then
                    codigo = Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition("SELECT IFNULL(MAX(Codigo) + 1, 1) FROM posvariables_imp", cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Await cn.ExecuteAsync(New CommandDefinition(
                    "REPLACE INTO posvariables_imp (Codigo, Parametro, Descripcion, Valor, Usuario, FC, FM) VALUES (@codigo, @parametro, @descripcion, @valor, @uid, @fc, NOW())",
                    New With {codigo, .parametro = Normalizar(v.Parametro, LargoParametro), .descripcion = Normalizar(v.Descripcion, LargoTexto),
                              .valor = Normalizar(v.Valor, LargoTexto), uid, .fc = If(v.FC, Date.Now)}, cancellationToken:=ct)).ConfigureAwait(False)
                Return codigo
            End Using
        End Function

        Public Async Function BorrarAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posvariables_imp WHERE Codigo = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
