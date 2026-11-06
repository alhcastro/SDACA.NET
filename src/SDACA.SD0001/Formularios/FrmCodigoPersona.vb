Imports MySqlConnector
Imports SDACA.Core.Catalogos
Imports SDACA.Ui

Namespace SD0001

    ''' <summary>
    ''' Agregar un código a una persona. Migración de Fuentes\SD0001\FrmAgregarTiposCodigos.frm. El
    ''' botón Tipos reemplaza a la opción "AGREGAR TIPOS DE CODIGOS" del combo, con el mismo privilegio.
    ''' </summary>
    Public Class FrmCodigoPersona
        Inherits Form

        Private Const LargoMinimo As Integer = 4
        Private Const LargoMaximo As Integer = 13
        Private Const AyudaCodigo As String = "El código debe tener al menos 4 caracteres y un máximo de 13."

        Private ReadOnly _repo As DatosPersonalesRepositorio
        Private ReadOnly _codigoInicial As String
        Private ReadOnly _abrirTipos As Func(Of Form)

        Private ReadOnly cmbTipo As New ComboBox With {.Name = "cmbTipo", .AccessibleName = "Tipo de código", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(80, 14), .Width = 320}
        Private ReadOnly btnTipos As New Button With {.Name = "btnTipos", .Text = "Tipos", .Location = New Point(338, 12), .Size = New Size(62, 28)}
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código", .MaxLength = LargoMaximo, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(80, 46), .Width = 220}
        Private ReadOnly btnCalcular As New Button With {.Name = "btnCalcular", .Text = "Siguiente", .Location = New Point(308, 44), .Size = New Size(92, 28)}
        Private ReadOnly lblInfo As New Label With {.Name = "lblInfo", .Text = AyudaCodigo, .AutoSize = False, .Location = New Point(12, 80), .Size = New Size(388, 40)}
        Private ReadOnly btnAceptar As New Button With {.Name = "btnAceptar", .Text = "Aceptar", .Location = New Point(216, 126), .Size = New Size(90, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "Cancelar", .DialogResult = DialogResult.Cancel, .Location = New Point(310, 126), .Size = New Size(90, 30)}

        ''' <param name="puedeCalcular">Privilegio 12: calcular el siguiente código y mantener los tipos.</param>
        ''' <param name="abrirTipos">Crea la pantalla de tipos de códigos.</param>
        Public Sub New(repositorio As DatosPersonalesRepositorio, codigoInicial As String, puedeCalcular As Boolean, abrirTipos As Func(Of Form))
            _repo = repositorio
            _codigoInicial = codigoInicial
            _abrirTipos = abrirTipos
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
            btnTipos.Visible = puedeCalcular
            If puedeCalcular Then cmbTipo.Width = 250
            Controls.AddRange(New Control() {
                New Label With {.Text = "Tipo", .AutoSize = True, .Location = New Point(12, 17)}, cmbTipo, btnTipos,
                New Label With {.Text = "Código", .AutoSize = True, .Location = New Point(12, 49)}, txtCodigo, btnCalcular, lblInfo, btnAceptar, btnCancelar
            })
            AddHandler Load, AddressOf Cargar
            AddHandler txtCodigo.KeyPress, Sub(s, e) e.Handled = e.KeyChar = "."c
            AddHandler txtCodigo.TextChanged, AddressOf Validar
            AddHandler cmbTipo.SelectedIndexChanged, AddressOf Validar
            AddHandler btnCalcular.Click, AddressOf Calcular_Clic
            AddHandler btnTipos.Click, AddressOf Tipos_Clic
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

        ''' <summary>Con un código de partida propone su tipo; si no, el interno, que no lleva prefijo.</summary>
        Private Async Function LlenarTiposAsync(codigo As String) As Task
            Dim tipos = Await _repo.TiposCodigoAsync()
            If tipos.Count = 0 Then tipos.Add(New ItemCatalogo With {.Descripcion = "CODIGO INTERNO"})
            cmbTipo.Items.Clear()
            cmbTipo.Items.AddRange(tipos.ToArray())
            Dim inicial = tipos.FindIndex(Function(t) t.Codigo.Trim().Length > 0 AndAlso codigo.StartsWith(t.Codigo.Trim(), StringComparison.Ordinal))
            If inicial < 0 Then inicial = Math.Max(tipos.FindIndex(Function(t) t.Codigo.Trim().Length = 0), 0)
            cmbTipo.SelectedIndex = inicial
        End Function

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                Await LlenarTiposAsync(_codigoInicial)
                txtCodigo.Text = _codigoInicial.Substring(Prefijo.Length)
                Validar(Nothing, EventArgs.Empty)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Tipos_Clic(sender As Object, e As EventArgs)
            Dim seleccionado = Prefijo
            Using ventana = _abrirTipos()
                Tema.Aplicar(ventana, Modulo.TituloModulo)
                ventana.ShowDialog(Me)
            End Using
            Try
                Await LlenarTiposAsync(seleccionado)
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
