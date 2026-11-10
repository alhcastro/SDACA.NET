Imports System.Drawing.Printing
Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Catalogos
Imports CoopeNet.Core.Impresion
Imports CoopeNet.Ui

Namespace SD0001

    ''' <summary>
    ''' Configuración de impresión y consecutivos. Migración de Comun\FrmManImp.frm (opción 048). Se elige un
    ''' documento y se editan sus filas de gatimpresion; Borrar marca la fila y Aplicar graba todo junto, igual
    ''' que en VB6. Pregunta antes de descartar cambios al cambiar de documento, refrescar o salir.
    ''' </summary>
    Public Class FrmConfiguracionImpresion
        Inherits Form

        ''' <summary>Fila marcada para borrar: el rojo suave del tema, que sigue el modo claro u oscuro.</summary>
        Private Shared ReadOnly Property ColorBorrar As Color
            Get
                Return Tema.PeligroSuave
            End Get
        End Property

        Private ReadOnly _repo As ConfiguracionImpresion
        Private _documento As String = ""
        Private _locales As New List(Of ItemCatalogo)
        Private _estaciones As New List(Of ItemCatalogo)
        Private ReadOnly _impresoras As New AutoCompleteStringCollection()
        Private _cambiando As Boolean

        Private ReadOnly cmbDocumento As New ComboBox With {.Name = "cmbDocumento", .AccessibleName = "Documento", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(100, 12), .Width = 360}
        Private ReadOnly grid As New DataGridView With {.Name = "gridImpresion", .AccessibleName = "Configuración", .Location = New Point(12, 48), .Size = New Size(936, 420),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right,
            .AllowUserToAddRows = True, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .MultiSelect = False,
            .RowHeadersWidth = 28, .SelectionMode = DataGridViewSelectionMode.CellSelect, .EditMode = DataGridViewEditMode.EditOnEnter}
        Private ReadOnly colLocal As New DataGridViewComboBoxColumn With {.Name = "colLocal", .HeaderText = "Departamento", .ValueMember = "Codigo", .DisplayMember = "Descripcion", .FlatStyle = FlatStyle.Flat, .FillWeight = 160, .MinimumWidth = 200}
        Private ReadOnly colEstacion As New DataGridViewComboBoxColumn With {.Name = "colEstacion", .HeaderText = "Estación de trabajo", .ValueMember = "Codigo", .DisplayMember = "Descripcion", .FlatStyle = FlatStyle.Flat, .FillWeight = 180}
        Private ReadOnly colImpresora As New DataGridViewTextBoxColumn With {.Name = "colImpresora", .HeaderText = "Impresora destino", .ToolTipText = "Vacío: la impresora predeterminada del equipo", .FillWeight = 200, .MaxInputLength = 128}
        Private ReadOnly colLleva As New DataGridViewCheckBoxColumn With {.Name = "colLleva", .HeaderText = "Consecutivo", .FillWeight = 60}
        Private ReadOnly colConsecutivo As New DataGridViewTextBoxColumn With {.Name = "colConsecutivo", .HeaderText = "Último Nº", .FillWeight = 60}
        Private ReadOnly colTipo As New DataGridViewComboBoxColumn With {.Name = "colTipo", .HeaderText = "Tipo", .ValueMember = "Codigo", .DisplayMember = "Descripcion", .FlatStyle = FlatStyle.Flat, .FillWeight = 80}
        Private ReadOnly btnBorrar As New Button With {.Name = "btnBorrar", .Text = "&Borrar", .Location = New Point(12, 478), .Size = New Size(96, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left}
        Private ReadOnly btnRefrescar As New Button With {.Name = "btnRefrescar", .Text = "&Refrescar", .Location = New Point(114, 478), .Size = New Size(96, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left}
        Private ReadOnly btnAplicar As New Button With {.Name = "btnAplicar", .Text = "&Aplicar", .Location = New Point(750, 478), .Size = New Size(96, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .DialogResult = DialogResult.Cancel, .Location = New Point(852, 478), .Size = New Size(96, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}

        Public Sub New(repositorio As ConfiguracionImpresion)
            _repo = repositorio
            Text = "Configuración de impresión y consecutivos"
            ClientSize = New Size(960, 520)
            MinimumSize = New Size(760, 400)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            colTipo.DataSource = New List(Of ItemCatalogo) From {
                New ItemCatalogo With {.Codigo = "0", .Descripcion = "PrintApi"},
                New ItemCatalogo With {.Codigo = "1", .Descripcion = "Active Report"}}
            grid.Columns.AddRange(colLocal, colEstacion, colImpresora, colLleva, colConsecutivo, colTipo)
            Controls.AddRange(New Control() {
                New Label With {.Text = "Documento", .AutoSize = True, .Location = New Point(12, 15)}, cmbDocumento,
                grid, btnBorrar, btnRefrescar, btnAplicar, btnSalir
            })
            AddHandler Load, AddressOf Cargar
            AddHandler cmbDocumento.SelectedIndexChanged, AddressOf Documento_Cambio
            AddHandler grid.DefaultValuesNeeded, AddressOf Valores_Nuevos
            AddHandler grid.EditingControlShowing, AddressOf Edicion
            AddHandler grid.CellValidating, AddressOf Celda_Validar
            AddHandler grid.DataError, Sub(s, e) e.ThrowException = False
            AddHandler btnBorrar.Click, AddressOf Borrar_Clic
            AddHandler btnRefrescar.Click, AddressOf Refrescar_Clic
            AddHandler btnAplicar.Click, AddressOf Aplicar_Clic
            AddHandler FormClosing, AddressOf Cerrando
        End Sub

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                Await CargarCatalogosAsync("")
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>SD_CargarDocumentos, los departamentos, las estaciones y las impresoras de este equipo y de la red.</summary>
        Private Async Function CargarCatalogosAsync(preferido As String) As Task
            Dim documentos = Await _repo.DocumentosAsync()
            _locales = Await _repo.LocalesAsync()
            _estaciones = Await _repo.EstacionesAsync()
            Dim red = Await _repo.ImpresorasAsync()
            _impresoras.Clear()
            _impresoras.AddRange(PrinterSettings.InstalledPrinters.Cast(Of String)().Concat(red).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
            Dim i = documentos.FindIndex(Function(d) String.Equals(d, preferido, StringComparison.OrdinalIgnoreCase))
            If i < 0 AndAlso documentos.Count > 0 Then i = 0
            _cambiando = True
            cmbDocumento.Items.Clear()
            cmbDocumento.Items.AddRange(documentos.ToArray())
            cmbDocumento.SelectedIndex = i
            _cambiando = False
            If i >= 0 Then
                Await MostrarAsync(documentos(i))
            Else
                _documento = ""
                grid.Rows.Clear()
            End If
        End Function

        ''' <summary>Agrega a la lista los códigos que la tabla tiene y el catálogo ya no, para no perderlos.</summary>
        Private Shared Function ConFaltantes(lista As List(Of ItemCatalogo), codigos As IEnumerable(Of String)) As List(Of ItemCatalogo)
            Dim resultado = lista.ToList()
            For Each c In codigos.Distinct()
                If Not resultado.Exists(Function(i) i.Codigo = c) Then resultado.Add(New ItemCatalogo With {.Codigo = c, .Descripcion = $"{c} (NO EXISTE)"})
            Next
            Return resultado
        End Function

        Private Async Function MostrarAsync(documento As String) As Task
            Dim filas = Await _repo.FilasAsync(documento)
            _documento = documento
            grid.Rows.Clear()
            colLocal.DataSource = ConFaltantes(_locales, filas.Select(Function(f) f.Local.ToString(CultureInfo.InvariantCulture)))
            colEstacion.DataSource = ConFaltantes(_estaciones, filas.Select(Function(f) f.Estacion))
            For Each f In filas
                Dim i = grid.Rows.Add(f.Local.ToString(CultureInfo.InvariantCulture), f.Estacion, f.Impresora, f.LlevaConsecutivo, f.Consecutivo.ToString(CultureInfo.InvariantCulture), f.Tipo.ToString(CultureInfo.InvariantCulture))
                grid.Rows(i).Tag = f
            Next
        End Function

        Private Sub Valores_Nuevos(sender As Object, e As DataGridViewRowEventArgs)
            e.Row.Cells(colLocal.Index).Value = ConfiguracionImpresion.Todos
            e.Row.Cells(colEstacion.Index).Value = ConfiguracionImpresion.Todos
            e.Row.Cells(colImpresora.Index).Value = ""
            e.Row.Cells(colLleva.Index).Value = False
            e.Row.Cells(colConsecutivo.Index).Value = "0"
            e.Row.Cells(colTipo.Index).Value = "0"
        End Sub

        ''' <summary>La impresora se escribe libre, con sugerencias, como el sgCellDropDown de VB6.</summary>
        Private Sub Edicion(sender As Object, e As DataGridViewEditingControlShowingEventArgs)
            Dim texto = TryCast(e.Control, TextBox)
            If texto Is Nothing Then Return
            If grid.CurrentCell.ColumnIndex = colImpresora.Index Then
                texto.AutoCompleteCustomSource = _impresoras
                texto.AutoCompleteSource = AutoCompleteSource.CustomSource
                texto.AutoCompleteMode = AutoCompleteMode.SuggestAppend
            Else
                texto.AutoCompleteMode = AutoCompleteMode.None
            End If
        End Sub

        Private Sub Celda_Validar(sender As Object, e As DataGridViewCellValidatingEventArgs)
            If e.ColumnIndex <> colConsecutivo.Index OrElse grid.Rows(e.RowIndex).IsNewRow Then Return
            Dim numero As Long
            If Not Long.TryParse(Convert.ToString(e.FormattedValue, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, numero) OrElse numero < 0 Then
                MessageBox.Show(Me, "El consecutivo debe ser un número entero, cero o mayor.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                e.Cancel = True
            End If
        End Sub

        ''' <summary>Las filas como están en la rejilla, con su original para saber qué cambió.</summary>
        Private Function Leer() As List(Of FilaImpresion)
            grid.EndEdit()
            Dim filas As New List(Of FilaImpresion)
            For Each r As DataGridViewRow In grid.Rows
                If r.IsNewRow Then Continue For
                Dim original = TryCast(r.Tag, FilaImpresion)
                Dim local As Long, consecutivo As Long, tipo As Integer
                Long.TryParse(Convert.ToString(r.Cells(colLocal.Index).Value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, local)
                Long.TryParse(Convert.ToString(r.Cells(colConsecutivo.Index).Value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, consecutivo)
                Integer.TryParse(Convert.ToString(r.Cells(colTipo.Index).Value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, tipo)
                filas.Add(New FilaImpresion With {
                    .Local = local,
                    .Estacion = Convert.ToString(r.Cells(colEstacion.Index).Value, CultureInfo.InvariantCulture),
                    .Impresora = If(Convert.ToString(r.Cells(colImpresora.Index).Value, CultureInfo.InvariantCulture), "").Trim(),
                    .LlevaConsecutivo = CBool(If(r.Cells(colLleva.Index).Value, False)),
                    .Consecutivo = consecutivo,
                    .Tipo = tipo,
                    .Original = original?.Original,
                    .Borrar = original IsNot Nothing AndAlso original.Borrar
                })
            Next
            Return filas
        End Function

        Private Function HayCambios() As Boolean
            Return _documento.Length > 0 AndAlso Leer().Any(Function(f) ConfiguracionImpresion.Sentencia(_documento, f).HasValue)
        End Function

        Private Function Descartar() As Boolean
            If Not HayCambios() Then Return True
            Return MessageBox.Show(Me, $"Hay cambios sin aplicar en {_documento}. ¿Desea descartarlos?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
        End Function

        Private Async Sub Documento_Cambio(sender As Object, e As EventArgs)
            If _cambiando Then Return
            Dim documento = TryCast(cmbDocumento.SelectedItem, String)
            If documento Is Nothing OrElse documento = _documento Then Return
            If Not Descartar() Then
                _cambiando = True
                cmbDocumento.SelectedItem = _documento
                _cambiando = False
                Return
            End If
            Try
                Await MostrarAsync(documento)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>Como CmdBor: marca o desmarca la fila. Una fila nueva se quita sin más.</summary>
        Private Sub Borrar_Clic(sender As Object, e As EventArgs)
            Dim r = grid.CurrentRow
            If r Is Nothing OrElse r.IsNewRow Then Return
            Dim f = TryCast(r.Tag, FilaImpresion)
            If f Is Nothing Then
                grid.Rows.Remove(r)
                Return
            End If
            f.Borrar = Not f.Borrar
            r.DefaultCellStyle.BackColor = If(f.Borrar, ColorBorrar, Color.Empty)
            r.DefaultCellStyle.ForeColor = If(f.Borrar, Tema.TextoTenue, Color.Empty)
        End Sub

        Private Async Sub Refrescar_Clic(sender As Object, e As EventArgs)
            If Not Descartar() Then Return
            Try
                Await CargarCatalogosAsync(_documento)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Aplicar_Clic(sender As Object, e As EventArgs)
            If _documento.Length = 0 Then
                MessageBox.Show(Me, "Debe seleccionar el tipo de documento para poder guardar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Return
            End If
            Dim filas = Leer()
            If filas.Any(Function(f) f.Original Is Nothing AndAlso String.IsNullOrWhiteSpace(f.Estacion)) Then
                MessageBox.Show(Me, "Cada fila nueva necesita una estación.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Return
            End If
            Dim llaves = filas.Where(Function(f) Not f.Borrar).GroupBy(Function(f) (f.Local, f.Estacion.ToUpperInvariant())).Where(Function(g) g.Count() > 1).ToList()
            If llaves.Count > 0 Then
                MessageBox.Show(Me, "Hay filas repetidas: cada departamento y estación puede aparecer una sola vez por documento.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Return
            End If
            UseWaitCursor = True
            btnAplicar.Enabled = False
            Try
                Dim errores = Await _repo.AplicarAsync(_documento, filas)
                Await MostrarAsync(_documento)
                If errores.Count > 0 Then
                    Dim detalle = String.Join(Environment.NewLine, errores.Select(Function(r) $"{r.Fila.Local} / {r.Fila.Estacion}: {r.Mensaje}"))
                    MessageBox.Show(Me, $"No se pudieron guardar {errores.Count} filas:{Environment.NewLine}{detalle}", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                End If
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "IMPRESION", Modulo.TituloModulo)
            Finally
                UseWaitCursor = False
                btnAplicar.Enabled = True
            End Try
        End Sub

        Private Sub Cerrando(sender As Object, e As FormClosingEventArgs)
            If e.CloseReason = CloseReason.UserClosing AndAlso Not Descartar() Then e.Cancel = True
        End Sub

    End Class

End Namespace
