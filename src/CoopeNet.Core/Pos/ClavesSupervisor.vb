Imports System.Security.Cryptography
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>
    ''' Generador de claves de SD0502. Migración de SD0502\frmGenerador.frm (cmdGenerar y cmdGuardar): una clave
    ''' numérica temporal por supervisor y local en posclaves, que el POS pide para autorizar. Se guarda su MD5 y
    ''' vence a las 23:59:59 del último día.
    ''' </summary>
    ''' <remarks>
    ''' VB6 creaba la tabla al abrir (CREATE TABLE sin IF NOT EXISTS, que fallaba en silencio); aquí se da por hecha.
    ''' Los números se sacan con RandomNumberGenerator en lugar de Rnd, que con Randomize depende de la hora.
    ''' </remarks>
    Public NotInheritable Class ClavesSupervisorRepositorio

        Public Const DiasMaximo As Integer = 30

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>Option1: una clave de 3, 4 o 5 dígitos que no empieza con cero.</summary>
        Public Shared Function Generar(digitos As Integer) As String
            If digitos < 3 OrElse digitos > 5 Then Throw New ArgumentOutOfRangeException(NameOf(digitos))
            Dim minimo = CInt(Math.Pow(10, digitos - 1))
            Return RandomNumberGenerator.GetInt32(minimo, minimo * 10).ToString(Globalization.CultureInfo.InvariantCulture)
        End Function

        ''' <summary>cmdGenerar: ¿otro supervisor del local ya tiene esa clave?</summary>
        Public Async Function ExisteAsync(clave As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(
                    "SELECT COUNT(*) FROM posclaves WHERE Clave = MD5(@clave) AND Local = @local", New With {clave, local}, cancellationToken:=ct)).ConfigureAwait(False) > 0
            End Using
        End Function

        ''' <summary>Genera hasta dar con una que no use otro supervisor del local.</summary>
        Public Async Function GenerarLibreAsync(digitos As Integer, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            For intento = 1 To 50
                Dim clave = Generar(digitos)
                If Not Await ExisteAsync(clave, local, ct).ConfigureAwait(False) Then Return clave
            Next
            Throw New InvalidOperationException("No se encontró una clave libre; use más dígitos.")
        End Function

        ''' <summary>Cuándo vence la clave actual del supervisor en el local, si tiene.</summary>
        Public Async Function VenceAsync(uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Date?)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Date?)(New CommandDefinition(
                    "SELECT Vence FROM posclaves WHERE Local = @local AND Usuario = @uid", New With {uid, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>cmdGuardar: reemplaza la clave del supervisor en el local. Devuelve cuándo vence.</summary>
        Public Async Function GuardarAsync(local As Long, uid As Long, clave As String, dias As Integer, Optional ct As CancellationToken = Nothing) As Task(Of Date)
            If dias < 1 OrElse dias > DiasMaximo Then Throw New ArgumentOutOfRangeException(NameOf(dias))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(
                    "REPLACE INTO posclaves (Local, Usuario, Clave, Vence, FC) VALUES (@local, @uid, MD5(@clave), " &
                    "DATE_FORMAT(DATE_ADD(NOW(), INTERVAL @dias DAY), '%Y-%m-%d 23:59:59'), NOW())",
                    New With {local, uid, clave, dias}, cancellationToken:=ct)).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Date)(New CommandDefinition(
                    "SELECT Vence FROM posclaves WHERE Local = @local AND Usuario = @uid", New With {uid, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
