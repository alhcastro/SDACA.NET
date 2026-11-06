Imports CoopeNet.Core.Cheques
Imports CoopeNet.Core.Datos

Namespace SD1501

    ''' <summary>
    ''' Opción 011 (Mover Cheque), privilegio 15010. Migración de Fuentes\SD1501\FrmMoveCheque.frm.
    ''' Comparte la pantalla de anulación. Igual que el original, no avisa si el cheque no existe o
    ''' está anulado, y no tiene la opción de borrar.
    ''' </summary>
    Public Class FrmMoveCheque
        Inherits FrmAnularCheque

        Private ReadOnly _anulacion As AnulacionCheques
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _estacion As String

        Public Sub New(repositorio As ChequesRepositorio, anulacion As AnulacionCheques, parametros As Parametros,
                       local As Long, uid As Long, todasLasCuentas As Boolean, cedula As String, estacion As String)
            MyBase.New(repositorio, anulacion, parametros, local, uid, todasLasCuentas, cedula)
            _anulacion = anulacion
            _local = local
            _uid = uid
            _estacion = estacion
            Rotular("Mover Cheques", "Mover Cheque #", "&Mover")
        End Sub

        Protected Overrides Sub ChequeNoDisponible(cheque As ChequeConsulta)
        End Sub

        Protected Overrides Function PermiteBorrar(cheque As ChequeConsulta, dias As Long) As Boolean
            Return False
        End Function

        Protected Overrides Async Function AccionPrincipalAsync() As Task
            Dim numero = ChequeCargado.Numero
            Dim resultado = Await _anulacion.MoverAsync(BancoCargado, CuentaCargada, ChequeCargado, Motivo, _local, _estacion, _uid)
            If resultado.Repetido Then MessageBox.Show(Me, "# Cheque Repetido.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            MessageBox.Show(Me, $"La información del cheque # {numero} se movio al cheque # {resultado.Cheque}", Modulo.TituloModulo)
        End Function

    End Class

End Namespace
