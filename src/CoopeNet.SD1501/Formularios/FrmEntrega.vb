Imports MySqlConnector
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Ui

Namespace SD1501

    ''' <summary>
    ''' Opción 017 (Entregar Cheques), privilegio 15023. Migración de Fuentes\SD1501\FrmEntrega.frm.
    ''' Como en el original, "Remover" saca el cheque de la lista de entrega sin devolverlo a la de
    ''' arriba: reaparece al filtrar de nuevo.
    ''' </summary>
    Partial Public Class FrmEntrega
        Inherits Form

        Private ReadOnly _repositorio As ChequesRepositorio
        Private ReadOnly _uid As Long

        Public Sub New(repositorio As ChequesRepositorio, uid As Long)
            _repositorio = repositorio
            _uid = uid
            InitializeComponent()
            cmbFiltro.Items.AddRange(New Object() {FiltroEntrega.Nombre, FiltroEntrega.Cheque})
            cmbFiltro.SelectedIndex = 0
        End Sub

        Private Async Sub TxtFiltro_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtFiltro.KeyPress
            If e.KeyChar <> ChrW(Keys.Return) Then Return
            e.Handled = True
            txtFiltro.Enabled = False
            Try
                Dim cheques = Await _repositorio.ListarPorEntregarAsync(CType(cmbFiltro.SelectedItem, FiltroEntrega), txtFiltro.Text)
                rejillaPendientes.Rows.Clear()
                For Each c In cheques
                    Agregar(rejillaPendientes, c)
                Next
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                txtFiltro.Enabled = True
                txtFiltro.Focus()
            End Try
        End Sub

        Private Shared Sub Agregar(rejilla As DataGridView, c As ChequeEntrega)
            Dim fecha As Object = If(c.Fecha = Date.MinValue, Nothing, CType(c.Fecha, Object))
            Dim indice = rejilla.Rows.Add(False, c.Cheque, fecha, c.Id, c.Codigo, c.Nombre, c.Monto, c.Detalle)
            rejilla.Rows(indice).Tag = c
        End Sub

        Private Shared Function Marcadas(rejilla As DataGridView) As List(Of DataGridViewRow)
            rejilla.EndEdit()
            Return rejilla.Rows.Cast(Of DataGridViewRow)().Where(Function(r) CBool(r.Cells(0).Value)).ToList()
        End Function

        Private Sub BtnAgregar_Click(sender As Object, e As EventArgs) Handles btnAgregar.Click
            For Each fila In Marcadas(rejillaPendientes)
                Agregar(rejillaEntrega, CType(fila.Tag, ChequeEntrega))
                rejillaPendientes.Rows.Remove(fila)
            Next
        End Sub

        Private Sub BtnRemover_Click(sender As Object, e As EventArgs) Handles btnRemover.Click
            For Each fila In Marcadas(rejillaEntrega)
                rejillaEntrega.Rows.Remove(fila)
            Next
        End Sub

        Private Async Sub BtnEntregar_Click(sender As Object, e As EventArgs) Handles btnEntregar.Click
            Dim cheques = rejillaEntrega.Rows.Cast(Of DataGridViewRow)().Select(Function(r) CType(r.Tag, ChequeEntrega)).ToList()
            btnEntregar.Enabled = False
            Try
                Await _repositorio.EntregarAsync(cheques, txtRecibidoPor.Text, _uid)
                rejillaEntrega.Rows.Clear()
                txtRecibidoPor.Text = ""
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                btnEntregar.Enabled = True
            End Try
        End Sub

    End Class

End Namespace
