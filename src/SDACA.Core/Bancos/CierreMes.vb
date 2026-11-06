Imports System.Threading
Imports Dapper
Imports SDACA.Core.Datos

Namespace Bancos

    Public NotInheritable Class BancoCierre
        Public Property Codigo As Integer
        Public Property Descripcion As String = ""

        Public Overrides Function ToString() As String
            Return Descripcion
        End Function
    End Class

    Public NotInheritable Class CuentaCierre
        Public Property Cuenta As String = ""
        Public Property Descripcion As String = ""
        Public Property Moneda As String = ""

        Public Overrides Function ToString() As String
            Return $"{Cuenta} ({Descripcion})"
        End Function
    End Class

    ''' <summary>
    ''' Cerrar y abrir meses por cuenta. Migración de SD1401\FrmCierre.frm. Sin el privilegio 14007
    ''' solo se ven las cuentas de ccbusuarioscuentas. Como en VB6, no se replica ni se filtra por local.
    ''' </summary>
    Public NotInheritable Class CierreMes

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Async Function ListarBancosAsync(uid As Long, todasLasCuentas As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of List(Of BancoCierre))
            Dim permiso = If(todasLasCuentas, "", " INNER JOIN ccbusuarioscuentas uc ON (uc.banco = b.banco AND uc.usuario = @uid) ")
            Dim sql = "SELECT DISTINCT b.banco AS Codigo, b.descripcion AS Descripcion FROM gatbancos b " &
                      "INNER JOIN ccbcuentas ca ON (ca.banco = b.banco) " & permiso & "ORDER BY Codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of BancoCierre)(New CommandDefinition(sql, New With {uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        Public Async Function ListarCuentasAsync(banco As Integer, uid As Long, todasLasCuentas As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of List(Of CuentaCierre))
            Dim permiso = If(todasLasCuentas, "", " INNER JOIN ccbusuarioscuentas uc ON (uc.banco = a.banco AND uc.cuenta = a.cuenta AND uc.usuario = @uid) ")
            Dim sql = "SELECT a.cuenta AS Cuenta, a.descripcion AS Descripcion, IFNULL(m.descripcion,'') AS Moneda FROM ccbcuentas a " &
                      "LEFT JOIN gatmonedas m ON (m.moneda = a.moneda) " & permiso & "WHERE a.banco = @banco"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of CuentaCierre)(New CommandDefinition(sql, New With {banco, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        Public Async Function EstaCerradoAsync(banco As Integer, cuenta As String, fecha As Date, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await LibroBancos.MesCerradoAsync(cn, banco, cuenta, fecha, ct).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function CerrarAsync(banco As Integer, cuenta As String, fecha As Date, Optional ct As CancellationToken = Nothing) As Task
            Const sql As String = "INSERT INTO ccbcierre (banco, cuenta, mes) VALUES (@banco, @cuenta, @mes)"
            Dim mes = LibroBancos.MesDeCierre(fecha)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {banco, cuenta, mes}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function AbrirAsync(banco As Integer, cuenta As String, fecha As Date, Optional ct As CancellationToken = Nothing) As Task
            Const sql As String = "DELETE FROM ccbcierre WHERE banco = @banco AND cuenta = @cuenta AND mes = @mes"
            Dim mes = LibroBancos.MesDeCierre(fecha)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {banco, cuenta, mes}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
