Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Tipo de cambio del POS (postipocambio). Migración de SD0502\frmtipocambio.frm: opción 029 de Mantenimiento
    ''' (privilegio 5046) y botón "Tipo de cambio" de Tipos de moneda.
    ''' </summary>
    ''' <remarks>
    ''' Se puede consultar cualquier día, pero solo se asigna el de hoy, como en VB6. El calendario de VB6 se cambia
    ''' por un selector de fecha, igual que en el tipo de cambio de SD0001. Al cambiar de moneda se muestra su cambio
    ''' (VB6 seguía mostrando el de la moneda anterior).
    ''' </remarks>
    Public Class FrmTipoCambioPos
        Inherits Form

        Private ReadOnly _repo As MonedasPosRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private _consulta As Integer

        Private ReadOnly cmbMoneda As New ComboBox With {.Name = "cmbMoneda", .AccessibleName = "Tipo de moneda", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 16), .Width = 220}
        Private ReadOnly dtpFecha As New DateTimePicker With {.Name = "dtpFecha", .AccessibleName = "Fecha", .Format = DateTimePickerFormat.Long, .Location = New Point(130, 52), .Width = 220}
        Private ReadOnly txtCambio As New TextBox With {
            .Name = "txtCambio", .AccessibleName = "Tipo de cambio", .TextAlign = HorizontalAlignment.Right, .Location = New Point(130, 88), .Width = 220,
            .Font = New Font(Tema.FuenteBase, 14.0F)}
        Private ReadOnly btnAceptar As New Button With {.Name = "btnGuardar", .Text = "&Aceptar", .Location = New Point(164, 140), .Size = New Size(90, 30)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(260, 140), .Size = New Size(90, 30)}

        Public Sub New(repo As MonedasPosRepositorio, uid As Long, local As Long)
            _repo = repo
            _uid = uid
            _local = local
            Text = "Tipo de cambio"
            ClientSize = New Size(366, 184)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            AcceptButton = btnAceptar
            CancelButton = btnSalir
            dtpFecha.Value = Date.Today
            dtpFecha.MaxDate = Date.Today
            Controls.AddRange(New Control() {
                Rotulo("Tipo de moneda", 19), cmbMoneda, Rotulo("Fecha", 55), dtpFecha, Rotulo("Tipo de cambio", 94), txtCambio, btnAceptar, btnSalir
            })
            AddHandler cmbMoneda.SelectionChangeCommitted, Async Sub() Await MostrarAsync()
            AddHandler dtpFecha.ValueChanged, Async Sub() Await MostrarAsync()
            AddHandler txtCambio.KeyPress, AddressOf SoloMonto
            AddHandler btnAceptar.Click, Async Sub() Await AceptarAsync()
            AddHandler btnSalir.Click, Sub() Close()
        End Sub

        Private Shared Function Rotulo(texto As String, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(12, y)}
        End Function

        Private Shared Sub SoloMonto(sender As Object, e As KeyPressEventArgs)
            If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> "."c AndAlso e.KeyChar <> ","c Then e.Handled = True
        End Sub

        Private ReadOnly Property Moneda As TipoMonedaPos
            Get
                Return TryCast(cmbMoneda.SelectedItem, TipoMonedaPos)
            End Get
        End Property

        ''' <summary>SD_Combos: con una sola moneda el combo queda fijo.</summary>
        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                cmbMoneda.Items.AddRange((Await _repo.MonedasAsync()).ToArray())
                If cmbMoneda.Items.Count > 0 Then cmbMoneda.SelectedIndex = 0
                cmbMoneda.Enabled = cmbMoneda.Items.Count > 1
                If cmbMoneda.Items.Count = 0 Then
                    btnAceptar.Enabled = False
                    MessageBox.Show(Me, "No hay tipos de moneda. Créelos en Tipos de moneda.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Await MostrarAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>MonthView1_SelChange: el cambio de ese día; Aceptar solo con la fecha de hoy.</summary>
        Private Async Function MostrarAsync() As Task
            btnAceptar.Enabled = dtpFecha.Value.Date = Date.Today AndAlso Moneda IsNot Nothing
            If Moneda Is Nothing Then Return
            _consulta += 1
            Dim consulta = _consulta
            Try
                Dim monto = Await _repo.CambioAsync(dtpFecha.Value, Moneda.Tipo)
                If consulta = _consulta Then txtCambio.Text = monto.ToString("0.####", CultureInfo.InvariantCulture)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>cmdAceptar: graba el cambio de hoy, lo replica y cierra.</summary>
        Private Async Function AceptarAsync() As Task
            If Moneda Is Nothing OrElse dtpFecha.Value.Date <> Date.Today Then Return
            Dim monto = MonedasPosRepositorio.LeerMonto(txtCambio.Text)
            If Not monto.HasValue Then
                MessageBox.Show(Me, "Escriba un tipo de cambio válido.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtCambio.Focus()
                Return
            End If
            Try
                btnAceptar.Enabled = False
                Await _repo.GuardarCambioAsync(Date.Today, monto.Value, Moneda.Tipo, _uid, _local)
                MessageBox.Show(Me, "Tipo de cambio asignado correctamente.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Close()
            Catch ex As MySqlException
                btnAceptar.Enabled = True
                Mensajes.NoGuardo(ex.Message, "TIPO DE CAMBIO", Modulo.TituloModulo)
            End Try
        End Function

    End Class

End Namespace
