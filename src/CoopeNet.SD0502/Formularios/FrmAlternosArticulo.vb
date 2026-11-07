Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Artículos alternos. Migración de SD0502\frmArtAlternos.frm. VB6 mostraba un alterno a la vez con anterior y
    ''' siguiente, que comparaban el código como número y ordenaban por el artículo; aquí se ven todos en una lista.
    ''' Seleccionar uno permite cambiar su detalle o eliminarlo; Nuevo pide el código (Enter vacío abre el buscador).
    ''' No migra el lector serial (posscanestac).
    ''' </summary>
    Public Class FrmAlternosArticulo
        Inherits Form

        Private Const SinArticulo As String = "<< Nuevo >>"

        Private ReadOnly _articulos As ArticulosRepositorio
        Private ReadOnly _repo As AlternosArticuloRepositorio
        Private ReadOnly _articulo As String
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        ''' <summary>El código cuya descripción se validó; vacío si el código escrito no es un artículo válido.</summary>
        Private _validado As String = ""
        Private _fc As Date = Date.Now
        Private _cargando As Boolean

        Private ReadOnly lblArticulo As New Label With {.Name = "lblArticulo", .AutoSize = False, .Location = New Point(12, 12), .Size = New Size(616, 20)}
        Private ReadOnly grid As New DataGridView With {.Name = "grid", .AccessibleName = "Artículos alternos", .Location = New Point(12, 38), .Size = New Size(616, 220),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .MultiSelect = False, .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
        Private ReadOnly colCodigo As New DataGridViewTextBoxColumn With {.Name = "colCodigo", .HeaderText = "Cód. alterno", .FillWeight = 70}
        Private ReadOnly colDescripcion As New DataGridViewTextBoxColumn With {.Name = "colDescripcion", .HeaderText = "Descripción", .FillWeight = 150}
        Private ReadOnly colDetalle As New DataGridViewTextBoxColumn With {.Name = "colDetalle", .HeaderText = "Detalle", .FillWeight = 150}
        Private ReadOnly colCreado As New DataGridViewTextBoxColumn With {.Name = "colCreado", .HeaderText = "Creado", .FillWeight = 70}

        Private ReadOnly gbAlterno As New GroupBox With {.Name = "gbAlterno", .Text = "Alterno", .Location = New Point(12, 266), .Size = New Size(616, 120)}
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código del artículo alterno", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(100, 24), .Width = 130, .MaxLength = AlternosArticuloRepositorio.LargoCodigo}
        Private ReadOnly lblDescripcion As New Label With {.Name = "lblDescripcion", .AutoSize = False, .Text = SinArticulo, .Location = New Point(240, 27), .Size = New Size(360, 20)}
        Private ReadOnly txtDetalle As New TextBox With {.Name = "txtDetalle", .AccessibleName = "Detalle", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(100, 56), .Width = 500, .MaxLength = AlternosArticuloRepositorio.LargoDetalle}
        Private ReadOnly lblFecha As New Label With {.Name = "lblFecha", .AutoSize = True, .Location = New Point(100, 88)}
        Private ReadOnly btnNuevo As New Button With {.Name = "btnNuevo", .Text = "&Nuevo", .Location = New Point(12, 398), .Size = New Size(100, 30)}
        Private ReadOnly btnGuardar As New Button With {.Name = "btnGuardar", .Text = "&Guardar", .Location = New Point(118, 398), .Size = New Size(100, 30)}
        Private ReadOnly btnEliminar As New Button With {.Name = "btnEliminar", .Text = "&Eliminar", .Location = New Point(224, 398), .Size = New Size(100, 30)}
        Private ReadOnly btnCerrar As New Button With {.Name = "btnCerrar", .Text = "&Cerrar", .DialogResult = DialogResult.Cancel, .Location = New Point(528, 398), .Size = New Size(100, 30)}

        Public Sub New(articulos As ArticulosRepositorio, repositorio As AlternosArticuloRepositorio, articulo As String, descripcion As String, local As Long, uid As Long)
            _articulos = articulos
            _repo = repositorio
            _articulo = articulo
            _local = local
            _uid = uid
            Text = "Artículos alternos"
            ClientSize = New Size(640, 440)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnCerrar
            lblArticulo.Text = $"{articulo}  -  {descripcion}"
            grid.Columns.AddRange(colCodigo, colDescripcion, colDetalle, colCreado)
            gbAlterno.Controls.AddRange(New Control() {
                New Label With {.Text = "Cód. alterno", .AutoSize = True, .Location = New Point(12, 27)}, txtCodigo, lblDescripcion,
                New Label With {.Text = "Detalle", .AutoSize = True, .Location = New Point(12, 59)}, txtDetalle, lblFecha})
            Controls.AddRange(New Control() {lblArticulo, grid, gbAlterno, btnNuevo, btnGuardar, btnEliminar, btnCerrar})
            lblArticulo.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            grid.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            gbAlterno.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            txtDetalle.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            lblDescripcion.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            For Each b In {btnNuevo, btnGuardar, btnEliminar}
                b.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
            Next
            btnCerrar.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            Ventanas.Recordar(Me)

            AddHandler Load, Async Sub() Await CargarAsync("")
            AddHandler grid.SelectionChanged, Sub() Mostrar()
            AddHandler txtCodigo.KeyDown, AddressOf Codigo_Tecla
            AddHandler txtCodigo.Leave, Async Sub() Await ValidarCodigoAsync()
            AddHandler txtCodigo.TextChanged, Sub() If Not _cargando Then lblDescripcion.Text = SinArticulo : _validado = ""
            AddHandler btnNuevo.Click, Sub() Nuevo()
            AddHandler btnGuardar.Click, Async Sub() Await GuardarAsync()
            AddHandler btnEliminar.Click, Async Sub() Await EliminarAsync()
        End Sub

        Private Function Seleccionado() As AlternoArticulo
            Return If(grid.CurrentRow Is Nothing, Nothing, CType(grid.CurrentRow.Tag, AlternoArticulo))
        End Function

        ''' <summary>Llena la lista y selecciona <paramref name="codigo"/>; sin alternos deja la ficha para uno nuevo.</summary>
        Private Async Function CargarAsync(codigo As String) As Task
            Try
                Dim lista = Await _repo.ListarAsync(_articulo, _local)
                _cargando = True
                grid.Rows.Clear()
                For Each a In lista
                    Dim i = grid.Rows.Add(a.CodAlterno, a.Descripcion, a.Detalle, a.FC.ToString("d", CultureInfo.CurrentCulture))
                    grid.Rows(i).Tag = a
                Next
                _cargando = False
                Dim fila = grid.Rows.Cast(Of DataGridViewRow)().FirstOrDefault(Function(r) CType(r.Tag, AlternoArticulo).CodAlterno = codigo)
                If fila IsNot Nothing Then
                    grid.CurrentCell = fila.Cells(0)
                    Mostrar()
                ElseIf grid.Rows.Count > 0 AndAlso codigo.Length = 0 Then
                    grid.CurrentCell = grid.Rows(0).Cells(0)
                    Mostrar()
                Else
                    Nuevo()
                End If
            Catch ex As MySqlException
                _cargando = False
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>SD_Cargar: el alterno seleccionado; su código no se cambia, solo el detalle.</summary>
        Private Sub Mostrar()
            If _cargando Then Return
            Dim a = Seleccionado()
            If a Is Nothing Then Return
            _cargando = True
            txtCodigo.Text = a.CodAlterno
            _cargando = False
            txtCodigo.ReadOnly = True
            lblDescripcion.Text = If(a.Descripcion.Length = 0, SinArticulo, a.Descripcion)
            _validado = a.CodAlterno
            txtDetalle.Text = a.Detalle
            _fc = a.FC
            lblFecha.Text = $"Creado el: {a.FC.ToString("D", CultureInfo.CurrentCulture)}"
            btnEliminar.Enabled = True
        End Sub

        ''' <summary>SDBoton_Nuevo y SD_Limpiar.</summary>
        Private Sub Nuevo()
            _cargando = True
            grid.ClearSelection()
            grid.CurrentCell = Nothing
            txtCodigo.Clear()
            _cargando = False
            txtCodigo.ReadOnly = False
            lblDescripcion.Text = SinArticulo
            _validado = ""
            txtDetalle.Clear()
            _fc = Date.Now
            lblFecha.Text = ""
            btnEliminar.Enabled = False
            txtCodigo.Focus()
        End Sub

        ''' <summary>sdtCodAlterno_KeyDown: Enter con el código vacío abre el buscador de artículos del local.</summary>
        Private Async Sub Codigo_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter OrElse txtCodigo.ReadOnly Then Return
            e.SuppressKeyPress = True
            If txtCodigo.Text.Trim().Length > 0 Then
                txtDetalle.Focus()
                Return
            End If
            Try
                Using datos = Await _articulos.BuscarAsync(_local, 0, 0, 0)
                    Using buscador As New FrmBuscador("Seleccione el artículo...", datos)
                        If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(1).Length = 0 Then Return
                        txtCodigo.Text = buscador.Resultado(1)
                    End Using
                End Using
                Await ValidarCodigoAsync()
                txtDetalle.Focus()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>SD_CargarArt: el código debe ser un artículo registrado en el local (o servicio, o sin precio).</summary>
        Private Async Function ValidarCodigoAsync() As Task(Of Boolean)
            Dim codigo = txtCodigo.Text.Trim()
            If txtCodigo.ReadOnly Then Return _validado.Length > 0
            If codigo.Length = 0 Then Return False
            If codigo = _validado Then Return True
            Try
                Dim a = Await _articulos.ObtenerAsync(codigo, _local)
                If a Is Nothing Then
                    MessageBox.Show(Me, "No se encontró ningún artículo con este código", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return False
                End If
                If Not ArticulosRepositorio.SePuedeMostrar(a) Then
                    MessageBox.Show(Me, $"El artículo [{a.Descripcion}] no se encuentra registrado en el local.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return False
                End If
                _cargando = True
                txtCodigo.Text = a.CodigoArt
                _cargando = False
                lblDescripcion.Text = a.Descripcion
                _validado = a.CodigoArt
                Return True
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return False
            End Try
        End Function

        Private Async Function GuardarAsync() As Task
            If Not Await ValidarCodigoAsync() Then
                txtCodigo.Focus()
                Return
            End If
            If String.Equals(_validado, _articulo, StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show(Me, "Un artículo no puede ser alterno de sí mismo.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim codigo = _validado
            Try
                Await _repo.GuardarAsync(_articulo, codigo, txtDetalle.Text, _local, _uid, _fc)
                Await CargarAsync(codigo)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "ALTERNOS", Modulo.TituloModulo)
            End Try
        End Function

        Private Async Function EliminarAsync() As Task
            Dim a = Seleccionado()
            If a Is Nothing OrElse Not txtCodigo.ReadOnly Then Return
            If MessageBox.Show(Me, "¿Está seguro de eliminar los datos actuales?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
            Try
                Await _repo.BorrarAsync(_articulo, a.CodAlterno, _local)
                Await CargarAsync("")
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "ALTERNOS", Modulo.TituloModulo)
            End Try
        End Function

    End Class

End Namespace
