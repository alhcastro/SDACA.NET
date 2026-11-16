Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Etiquetas para los precios de venta. Migración de SD0502\FrmEtiquetasPrecios.frm (Mantenimiento, opción 052,
    ''' privilegio 5083).
    ''' </summary>
    Public Class FrmEtiquetasPrecio
        Inherits Form

        Private ReadOnly _repo As EtiquetasPrecioRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _textos As New List(Of TextBox)

        Private ReadOnly gbPrecios As New GroupBox With {.Name = "gbPrecios", .Location = New Point(12, 8), .Size = New Size(340, 232)}
        Private ReadOnly btnAceptar As New Button With {.Name = "btnAceptar", .Text = "&Aceptar", .Location = New Point(132, 250), .Size = New Size(108, 30), .Enabled = False}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .Location = New Point(244, 250), .Size = New Size(108, 30)}

        Public Sub New(repo As EtiquetasPrecioRepositorio, local As Long, uid As Long)
            _repo = repo
            _local = local
            _uid = uid
            Text = "Etiquetas para los precios de venta"
            ClientSize = New Size(364, 292)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            AcceptButton = btnAceptar
            CancelButton = btnCancelar

            For i = 0 To EtiquetasPrecioRepositorio.Cantidad - 1
                Dim y = 28 + i * 33
                Dim txt As New TextBox With {
                    .Name = $"txtPrecio{i}", .AccessibleName = $"Precio {i + 1}", .MaxLength = EtiquetasPrecioRepositorio.LargoValor,
                    .Location = New Point(100, y - 3), .Width = 220}
                _textos.Add(txt)
                gbPrecios.Controls.Add(New Label With {.Text = $"Precio {i + 1}.", .AutoSize = True, .Location = New Point(16, y)})
                gbPrecios.Controls.Add(txt)
            Next
            Controls.AddRange(New Control() {gbPrecios, btnAceptar, btnCancelar})
            AddHandler btnAceptar.Click, Async Sub() Await GuardarAsync()
            AddHandler btnCancelar.Click, Sub() Close()
        End Sub

        ''' <summary>Form_Load.</summary>
        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                Dim etiquetas = Await _repo.LeerAsync(_local)
                For i = 0 To _textos.Count - 1
                    _textos(i).Text = etiquetas(i)
                Next
                btnAceptar.Enabled = True
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>cmdAceptar: guarda las seis y cierra.</summary>
        Private Async Function GuardarAsync() As Task
            Try
                btnAceptar.Enabled = False
                Await _repo.GuardarAsync(_local, _textos.Select(Function(t) t.Text).ToList(), _uid)
                MessageBox.Show(Me, "Etiquetas guardadas.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Close()
            Catch ex As MySqlException
                btnAceptar.Enabled = True
                Mensajes.NoGuardo(ex.Message, "Etiquetas de precios", Modulo.TituloModulo)
            End Try
        End Function

    End Class

End Namespace
