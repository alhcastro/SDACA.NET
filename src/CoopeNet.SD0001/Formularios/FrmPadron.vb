Imports MySqlConnector
Imports CoopeNet.Core.Catalogos
Imports CoopeNet.Ui

Namespace SD0001

    ''' <summary>
    ''' Padrón Nacional y Padrón Jurídico. Migración de Fuentes\SD0001\FrmBuscaPadron.frm en el modo del menú,
    ''' que solo consulta: sin el botón Aceptar. El modo de eventos lo usa Datos Personales (LblPadron), que
    ''' todavía no está migrado.
    ''' </summary>
    Public Class FrmPadron
        Inherits Form

        Private ReadOnly _repo As PadronRepositorio
        Private ReadOnly _juridico As Boolean

        Private ReadOnly txtCedula As New TextBox With {.Name = "txtCedula", .AccessibleName = "Identificación", .Location = New Point(12, 30), .Width = 130}
        Private ReadOnly txtApellido1 As New TextBox With {.Name = "txtApellido1", .AccessibleName = "1er apellido", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(150, 30), .Width = 160}
        Private ReadOnly txtApellido2 As New TextBox With {.Name = "txtApellido2", .AccessibleName = "2do apellido", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(318, 30), .Width = 160}
        Private ReadOnly txtNombre As New TextBox With {.Name = "txtNombre", .AccessibleName = "Nombre", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(486, 30), .Width = 180}
        Private ReadOnly btnBuscar As New Button With {.Name = "btnBuscar", .Text = "&Buscar", .Location = New Point(674, 28), .Size = New Size(90, 28)}
        Private ReadOnly grid As New DataGridView With {.Name = "gridPadron", .AccessibleName = "Resultados", .Location = New Point(12, 66), .Size = New Size(752, 360),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right,
            .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .ReadOnly = True, .MultiSelect = False,
            .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
        Private ReadOnly lblRegistros As New Label With {.Name = "lblRegistros", .AutoSize = True, .Location = New Point(12, 442), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .DialogResult = DialogResult.Cancel, .Location = New Point(674, 436), .Size = New Size(90, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}

        Public Sub New(repositorio As PadronRepositorio, juridico As Boolean)
            _repo = repositorio
            _juridico = juridico
            Text = If(juridico, "Padrón Jurídico", "Padrón Nacional")
            ClientSize = New Size(776, 478)
            MinimumSize = New Size(640, 360)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            AcceptButton = btnBuscar
            CancelButton = btnSalir
            ' La máscara de VB6 era 1-4-4 en el nacional y 1-3-6 en el jurídico, sin guiones en el valor.
            txtCedula.MaxLength = If(juridico, 12, 11)
            Controls.AddRange(New Control() {
                New Label With {.Text = "Identificación", .AutoSize = True, .Location = New Point(12, 10)}, txtCedula,
                btnBuscar, grid, lblRegistros, btnSalir
            })
            If juridico Then
                txtNombre.Location = txtApellido1.Location
                txtNombre.Width = 516
                Controls.AddRange(New Control() {New Label With {.Text = "Nombre", .AutoSize = True, .Location = New Point(150, 10)}, txtNombre})
            Else
                Controls.AddRange(New Control() {
                    New Label With {.Text = "1er apellido", .AutoSize = True, .Location = New Point(150, 10)}, txtApellido1,
                    New Label With {.Text = "2do apellido", .AutoSize = True, .Location = New Point(318, 10)}, txtApellido2,
                    New Label With {.Text = "Nombre", .AutoSize = True, .Location = New Point(486, 10)}, txtNombre
                })
            End If
            AddHandler btnBuscar.Click, AddressOf Buscar_Clic
            AddHandler grid.CellFormatting, AddressOf Fecha_Formato
        End Sub

        ''' <summary>1,414 personas tienen Nacimiento 0000-00-00, que llega como 01/01/0001: se deja en blanco.</summary>
        Private Sub Fecha_Formato(sender As Object, e As DataGridViewCellFormattingEventArgs)
            If Not TypeOf e.Value Is Date Then Return
            Dim fecha = CDate(e.Value)
            e.Value = If(fecha.Year < 1800, "", fecha.ToString("dd/MM/yyyy"))
            e.FormattingApplied = True
        End Sub

        Private Async Sub Buscar_Clic(sender As Object, e As EventArgs)
            Dim criterio As New CriterioPadron With {.Juridico = _juridico, .Cedula = txtCedula.Text, .Apellido1 = txtApellido1.Text, .Apellido2 = txtApellido2.Text, .Nombre = txtNombre.Text}
            btnBuscar.Enabled = False
            UseWaitCursor = True
            lblRegistros.Text = ""
            Try
                Dim resultado = Await _repo.BuscarAsync(criterio)
                If resultado.Tabla Is Nothing Then
                    MessageBox.Show(Me, If(_juridico, "Digite la identificación o el nombre.", "Digite la identificación, un apellido o el nombre."), Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    txtCedula.Focus()
                    Return
                End If
                Dim anterior = TryCast(grid.DataSource, IDisposable)
                grid.DataSource = resultado.Tabla
                anterior?.Dispose()
                lblRegistros.Text = $"Registros encontrados: {resultado.Tabla.Rows.Count}"
                If resultado.Excedido Then
                    MessageBox.Show(Me, $"Existen más de {PadronRepositorio.Maximo} resultados para la búsqueda.{Environment.NewLine}Por favor sea más específico.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
                If resultado.Tabla.Rows.Count > 0 Then grid.Focus() Else txtCedula.Focus()
            Catch ex As MySqlException When ex.ErrorCode = MySqlErrorCode.UnknownDatabase OrElse ex.ErrorCode = MySqlErrorCode.NoSuchTable
                MessageBox.Show(Me, "El padrón no está instalado en este servidor.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                UseWaitCursor = False
                btnBuscar.Enabled = True
            End Try
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then TryCast(grid.DataSource, IDisposable)?.Dispose()
            MyBase.Dispose(disposing)
        End Sub

    End Class

End Namespace
