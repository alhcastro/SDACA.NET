Imports MySqlConnector
Imports SDACA.Core.Catalogos
Imports SDACA.Ui

Namespace SD0001

    ''' <summary>
    ''' Agregar un código a una persona. Migración de Fuentes\SD0001\FrmAgregarTiposCodigos.frm. La
    ''' opción de agregar tipos de códigos es de Administración de Códigos, que aún no está migrada.
    ''' </summary>
    Public Class FrmCodigoPersona
        Inherits Form

        Private Const LargoMinimo As Integer = 4
        Private Const LargoMaximo As Integer = 13
        Private Const AyudaCodigo As String = "El código debe tener al menos 4 caracteres y un máximo de 13."

        Private ReadOnly _repo As DatosPersonalesRepositorio
        Private ReadOnly _codigoInicial As String

        Private ReadOnly cmbTipo As New ComboBox With {.Name = "cmbTipo", .AccessibleName = "Tipo de código", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(80, 14), .Width = 320}
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código", .MaxLength = LargoMaximo, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(80, 46), .Width = 220}
        Private ReadOnly btnCalcular As New Button With {.Name = "btnCalcular", .Text = "Siguiente", .Location = New Point(308, 44), .Size = New Size(92, 28)}
        Private ReadOnly lblInfo As New Label With {.Name = "lblInfo", .Text = AyudaCodigo, .AutoSize = False, .Location = New Point(12, 80), .Size = New Size(388, 40)}
        Private ReadOnly btnAceptar As New Button With {.Name = "btnAceptar", .Text = "Aceptar", .Location = New Point(216, 126), .Size = New Size(90, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "Cancelar", .DialogResult = DialogResult.Cancel, .Location = New Point(310, 126), .Size = New Size(90, 30)}

        Public Sub New(repositorio As DatosPersonalesRepositorio, codigoInicial As String, puedeCalcular As Boolean)
            _repo = repositorio
            _codigoInicial = codigoInicial
            Text = "Agregar código"
            ClientSize = New Size(412, 168)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnCancelar
            AcceptButton = btnAceptar
            btnCalcular.Visible = puedeCalcular
            Controls.AddRange(New Control() {
                New Label With {.Text = "Tipo", .AutoSize = True, .Location = New Point(12, 17)}, cmbTipo,
                New Label With {.Text = "Código", .AutoSize = True, .Location = New Point(12, 49)}, txtCodigo, btnCalcular, lblInfo, btnAceptar, btnCancelar
            })
            AddHandler Load, AddressOf Cargar
            AddHandler txtCodigo.KeyPress, Sub(s, e) e.Handled = e.KeyChar = "."c
            AddHandler txtCodigo.TextChanged, AddressOf Validar
            AddHandler cmbTipo.SelectedIndexChanged, AddressOf Validar
            AddHandler btnCalcular.Click, AddressOf Calcular_Clic
            AddHandler btnAceptar.Click, AddressOf Aceptar_Clic
        End Sub

        ''' <summary>Tipo y código ya unidos, como se graban en gaccodclientes.</summary>
        Public ReadOnly Property Codigo As String
            Get
                Return Prefijo & txtCodigo.Text.Trim()
            End Get
        End Property

        Private ReadOnly Property Prefijo As String
            Get
                Dim tipo = TryCast(cmbTipo.SelectedItem, ItemCatalogo)
                Return If(tipo Is Nothing, "", tipo.Codigo.Trim())
            End Get
        End Property

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                Dim tipos = Await _repo.TiposCodigoAsync()
                If tipos.Count = 0 Then tipos.Add(New ItemCatalogo With {.Descripcion = "CODIGO INTERNO"})
                cmbTipo.Items.AddRange(tipos.ToArray())

                ' Con un código de partida se propone su tipo; si no, el interno, que no lleva prefijo.
                Dim inicial = tipos.FindIndex(Function(t) t.Codigo.Trim().Length > 0 AndAlso _codigoInicial.StartsWith(t.Codigo.Trim(), StringComparison.Ordinal))
                If inicial < 0 Then inicial = Math.Max(tipos.FindIndex(Function(t) t.Codigo.Trim().Length = 0), 0)
                cmbTipo.SelectedIndex = inicial
                txtCodigo.Text = _codigoInicial.Substring(Prefijo.Length)
                Validar(Nothing, EventArgs.Empty)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Validar(sender As Object, e As EventArgs)
            lblInfo.ForeColor = Tema.TextoTenue
            lblInfo.Text = AyudaCodigo
            btnAceptar.Enabled = txtCodigo.Text.Trim().Length >= LargoMinimo AndAlso cmbTipo.SelectedIndex >= 0
        End Sub

        Private Async Sub Calcular_Clic(sender As Object, e As EventArgs)
            Try
                txtCodigo.Text = Await _repo.SiguienteCodigoAsync(Prefijo)
            Catch ex As MySqlException
                MessageBox.Show(Me, "Error al intentar obtener el nuevo código.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End Try
        End Sub

        Private Async Sub Aceptar_Clic(sender As Object, e As EventArgs)
            Try
                Dim dueno = Await _repo.DuenoCodigoAsync(Codigo)
                If dueno.Length = 0 Then
                    DialogResult = DialogResult.OK
                    Return
                End If
                lblInfo.ForeColor = Tema.Peligro
                lblInfo.Text = $"Código ya asignado a: {dueno} - {Await _repo.NombreAsync(dueno)}"
                btnAceptar.Enabled = False
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

    End Class

End Namespace
