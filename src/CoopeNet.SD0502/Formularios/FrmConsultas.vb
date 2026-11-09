Imports System.IO
Imports MySqlConnector
Imports CoopeNet.Core.Datos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Consultas guardadas. Migración de SD0502\REP\frmConsultas.frm (ConsultasMod), que abre "Histórico utilidades"
    ''' del Catálogo de Artículos con la consulta 1 filtrada por el artículo. La pestaña Consultas ejecuta las de
    ''' posconsultas y las exporta; Editar escribe, prueba y guarda el SQL.
    ''' </summary>
    ''' <remarks>
    ''' Toda consulta corre como una sola sentencia SELECT y sin crear tablas (ver ConsultasGuardadasRepositorio).
    ''' El editor muestra a lo sumo 1000 filas.
    ''' </remarks>
    Public Class FrmConsultas
        Inherits Form

        Private ReadOnly _repo As ConsultasGuardadasRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _consultaInicial As Long
        Private ReadOnly _filtroInicial As String
        Private ReadOnly _archivoTemporal As String
        ''' <summary>La columna por la que busca el cuadro de búsqueda (iColumn).</summary>
        Private _columna As String = ""
        Private _sql As String = ""
        Private _consulta As ConsultaGuardada
        Private _pintando As Boolean
        Private _version As Integer

        Private ReadOnly tabs As New Pestanas With {.Name = "tabs", .Location = New Point(12, 8), .Size = New Size(976, 640), .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}
        Private ReadOnly tabConsultas As New TabPage("Consultas")
        Private ReadOnly tabEditar As New TabPage("Editar")

        ' ---- Consultas ----
        Private ReadOnly cmbConsultas As New ComboBox With {.Name = "cmbConsultas", .AccessibleName = "Consulta", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(80, 12), .Width = 720}
        Private ReadOnly btnRefrescar As New Button With {.Name = "btnRefrescar", .Text = "&Refrescar", .Location = New Point(810, 10), .Size = New Size(140, 28)}
        Private ReadOnly txtBuscar As New TextBox With {.Name = "txtBuscar", .AccessibleName = "Buscar", .Location = New Point(12, 48), .Width = 380}
        Private ReadOnly lblBuscar As New Label With {.Name = "lblBuscar", .Text = "Busca por:", .AutoSize = True, .Location = New Point(402, 51)}
        Private ReadOnly grid As DataGridView = Rejilla("grid", "Resultado")
        Private ReadOnly lblRegistros As New Label With {.Name = "lblRegistros", .AutoSize = True}
        Private ReadOnly btnExcel As New Button With {.Name = "btnExcel", .Text = "Enviar a &Excel", .Size = New Size(140, 28)}

        ' ---- Editar ----
        Private ReadOnly cmbEditar As New ComboBox With {.Name = "cmbEditar", .AccessibleName = "Consulta a editar", .DropDownStyle = ComboBoxStyle.DropDown, .Location = New Point(80, 12), .Width = 560}
        Private ReadOnly btnGuardar As New Button With {.Name = "btnGuardar", .Text = "&Guardar", .Location = New Point(650, 10), .Size = New Size(140, 28)}
        Private ReadOnly btnEjecutar As New Button With {.Name = "btnEjecutar", .Text = "Ejecutar (F5)", .Location = New Point(800, 10), .Size = New Size(150, 28)}
        Private ReadOnly txtSql As New TextBox With {.Name = "txtSql", .AccessibleName = "SQL", .Multiline = True, .ScrollBars = ScrollBars.Both, .WordWrap = False, .AcceptsReturn = True, .AcceptsTab = True,
            .Font = New Font("Consolas", 10), .Location = New Point(12, 46), .Size = New Size(938, 150)}
        Private ReadOnly txtPrefijo As New TextBox With {.Name = "txtPrefijo", .AccessibleName = "Filtro de tablas", .Location = New Point(80, 206), .Width = 180}
        Private ReadOnly btnTablas As New Button With {.Name = "btnTablas", .Text = "Refrescar", .Location = New Point(266, 204), .Size = New Size(100, 28)}
        Private ReadOnly btnAgregar As New Button With {.Name = "btnAgregar", .Text = "Agregar", .Location = New Point(850, 204), .Size = New Size(100, 28)}
        Private ReadOnly lstTablas As New ListBox With {.Name = "lstTablas", .AccessibleName = "Tablas", .Location = New Point(12, 238), .Size = New Size(354, 150), .IntegralHeight = False}
        Private ReadOnly lstCampos As New ListBox With {.Name = "lstCampos", .AccessibleName = "Campos", .Location = New Point(376, 238), .Size = New Size(574, 150), .IntegralHeight = False,
            .SelectionMode = SelectionMode.MultiExtended, .Font = New Font("Consolas", 9)}
        Private ReadOnly txtFiltroPrueba As New TextBox With {.Name = "txtFiltroPrueba", .AccessibleName = "Buscar en la prueba", .Location = New Point(80, 398), .Width = 380}
        Private ReadOnly gridPrueba As DataGridView = Rejilla("gridPrueba", "Resultado de la prueba")
        Private ReadOnly lblPrueba As New Label With {.Name = "lblPrueba", .AutoSize = True, .Location = New Point(470, 401)}

        ''' <param name="consulta">El código que se ejecuta al abrir (iConsulta); 0 para ninguno.</param>
        ''' <param name="filtro">El texto que se busca en la primera columna (iFiltro).</param>
        Public Sub New(repositorio As ConsultasGuardadasRepositorio, local As Long, directorioDatos As String, consulta As Long, filtro As String)
            _repo = repositorio
            _local = local
            _consultaInicial = consulta
            _filtroInicial = If(filtro, "").Trim()
            _archivoTemporal = Path.Combine(If(directorioDatos, ""), "temp", "tmpConsulta.txt")

            Text = "Consultas"
            ClientSize = New Size(1000, 660)
            MinimumSize = New Size(860, 600)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            KeyPreview = True

            grid.SetBounds(12, 80, 938, 480)
            lblRegistros.Location = New Point(12, 574)
            btnExcel.Location = New Point(810, 568)
            tabConsultas.Controls.AddRange(New Control() {New Label With {.Text = "Consulta", .AutoSize = True, .Location = New Point(12, 15)}, cmbConsultas, btnRefrescar,
                                                          txtBuscar, lblBuscar, grid, lblRegistros, btnExcel})

            gridPrueba.SetBounds(12, 430, 938, 160)
            tabEditar.Controls.AddRange(New Control() {New Label With {.Text = "Consultas", .AutoSize = True, .Location = New Point(12, 15)}, cmbEditar, btnGuardar, btnEjecutar, txtSql,
                                                       New Label With {.Text = "Filtro", .AutoSize = True, .Location = New Point(12, 209)}, txtPrefijo, btnTablas, btnAgregar, lstTablas, lstCampos,
                                                       New Label With {.Text = "Buscar", .AutoSize = True, .Location = New Point(12, 401)}, txtFiltroPrueba, lblPrueba, gridPrueba})
            tabs.TabPages.AddRange({tabConsultas, tabEditar})
            Controls.Add(tabs)
            ' Dentro de las pestañas se ancla al cargar, cuando las páginas ya tienen su tamaño; antes que
            ' Ventanas.Recordar, que restaura el tamaño también en Load.
            AddHandler Load, Sub() AnclarPestanas()
            Ventanas.Recordar(Me)

            AddHandler Load, AddressOf Cargar
            AddHandler FormClosing, Sub() GuardarTemporal()
            AddHandler cmbConsultas.SelectedIndexChanged, Sub() _consulta = TryCast(cmbConsultas.SelectedItem, ConsultaGuardada)
            AddHandler btnRefrescar.Click, Async Sub() Await RefrescarAsync()
            AddHandler txtBuscar.TextChanged, Async Sub() Await FiltrarAsync()
            AddHandler grid.ColumnHeaderMouseClick, Sub(s, e) ElegirColumna(e.ColumnIndex)
            AddHandler btnExcel.Click, AddressOf Excel_Clic
            AddHandler cmbEditar.SelectedIndexChanged, Async Sub() Await CargarSqlAsync()
            AddHandler btnGuardar.Click, Async Sub() Await GuardarAsync()
            AddHandler btnEjecutar.Click, Async Sub() Await EjecutarAsync()
            AddHandler btnTablas.Click, Async Sub() Await TablasAsync()
            AddHandler lstTablas.SelectedIndexChanged, Async Sub() Await CamposAsync()
            AddHandler lstTablas.DoubleClick, Sub() AgregarTabla()
            AddHandler lstCampos.DoubleClick, Sub() If lstCampos.SelectedItem IsNot Nothing Then Insertar(NombreCampo(CStr(lstCampos.SelectedItem)))
            AddHandler btnAgregar.Click, Sub() Insertar(String.Join(", ", lstCampos.SelectedItems.Cast(Of String)().Select(AddressOf NombreCampo)))
            AddHandler txtFiltroPrueba.TextChanged, Sub() FiltrarPrueba()
        End Sub

        Private Sub AnclarPestanas()
            Dim todo = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            Dim ancho = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            Dim derecha = AnchorStyles.Top Or AnchorStyles.Right
            grid.Anchor = todo
            gridPrueba.Anchor = todo
            For Each c As Control In New Control() {cmbConsultas, txtSql, lstCampos, cmbEditar}
                c.Anchor = ancho
            Next
            For Each c As Control In New Control() {btnRefrescar, btnAgregar, btnGuardar, btnEjecutar}
                c.Anchor = derecha
            Next
            lblRegistros.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
            btnExcel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        End Sub

        Private Shared Function Rejilla(nombre As String, titulo As String) As DataGridView
            Return New DataGridView With {.Name = nombre, .AccessibleName = titulo, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
                .AllowUserToResizeRows = False, .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells}
        End Function

        ''' <summary>Form_Load: las consultas, las tablas, el último SQL del editor y la consulta pedida.</summary>
        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                Await ConsultasAsync()
                Await TablasAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return
            End Try
            Try
                If File.Exists(_archivoTemporal) Then txtSql.Text = File.ReadAllText(_archivoTemporal)
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
            End Try
            If _consultaInicial > 0 Then
                Dim indice = cmbConsultas.Items.Cast(Of ConsultaGuardada)().ToList().FindIndex(Function(c) c.Codigo = _consultaInicial)
                If indice >= 0 Then
                    cmbConsultas.SelectedIndex = indice
                    If Await RefrescarAsync() Then txtBuscar.Text = _filtroInicial
                End If
            End If
        End Sub

        ''' <summary>SD_Combos.</summary>
        Private Async Function ConsultasAsync() As Task
            Dim lista = Await _repo.ListarAsync()
            _pintando = True
            cmbConsultas.Items.Clear()
            cmbEditar.Items.Clear()
            cmbConsultas.Items.AddRange(lista.ToArray())
            cmbEditar.Items.AddRange(lista.ToArray())
            _pintando = False
            If lista.Count = 0 Then
                MessageBox.Show(Me, "No se encontraron consultas registradas", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            cmbConsultas.SelectedIndex = 0
            cmbEditar.SelectedIndex = 0
        End Function

        ' ---- Consultas ----

        Private Sub Ocupado(si As Boolean)
            UseWaitCursor = si
            btnRefrescar.Enabled = Not si
            btnEjecutar.Enabled = Not si
        End Sub

        Private Sub MostrarError(ex As Exception)
            MessageBox.Show(Me, $"Error al intentar mostrar los datos: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        ''' <summary>cmdRefres_Click: ejecuta la consulta elegida, ordenada por la primera columna. Devuelve False si falló.</summary>
        Private Async Function RefrescarAsync() As Task(Of Boolean)
            If _consulta Is Nothing Then Return False
            Ocupado(True)
            Try
                _sql = Await _repo.SqlAsync(_consulta.Codigo)
                Dim tabla = Await _repo.EjecutarAsync(_sql, _local, True, "", "", Limite())
                _columna = If(tabla.Columns.Count > 0, tabla.Columns(0).ColumnName, "")
                Mostrar(tabla)
                _pintando = True
                txtBuscar.Clear()
                _pintando = False
                Return True
            Catch ex As Exception When TypeOf ex Is MySqlException OrElse TypeOf ex Is ArgumentException
                Mostrar(New DataTable())
                MostrarError(ex)
                Return False
            Finally
                Ocupado(False)
            End Try
        End Function

        Private Function Limite() As Integer?
            Return If(_consulta IsNot Nothing AndAlso _consulta.SinLimite, CType(Nothing, Integer?), ConsultasGuardadasRepositorio.LimiteFilas)
        End Function

        Private Sub Mostrar(tabla As DataTable)
            Dim anterior = TryCast(grid.DataSource, DataView)?.Table
            If tabla.Columns.Count > 0 Then tabla.DefaultView.Sort = $"[{tabla.Columns(0).ColumnName.Replace("]", "\]")}] ASC"
            grid.DataSource = tabla.DefaultView
            anterior?.Dispose()
            grid.Enabled = tabla.Rows.Count > 0
            lblBuscar.Text = $"Busca por: {_columna}"
            lblRegistros.Text = $"{tabla.Rows.Count} Registros."
        End Sub

        ''' <summary>cmdFiltrado: vuelve a consultar con las filas cuya columna contiene el texto.</summary>
        Private Async Function FiltrarAsync() As Task
            If _pintando OrElse _sql.Length = 0 OrElse _columna.Length = 0 Then Return
            _version += 1
            Dim version = _version
            Try
                Dim texto = txtBuscar.Text.Trim()
                Dim tabla = Await _repo.EjecutarAsync(_sql, _local, True, If(texto.Length = 0, "", _columna), texto, If(texto.Length = 0, Limite(), ConsultasGuardadasRepositorio.LimiteFilas))
                If version = _version Then Mostrar(tabla) Else tabla.Dispose()
            Catch ex As Exception When TypeOf ex Is MySqlException OrElse TypeOf ex Is ArgumentException
                If version = _version Then MostrarError(ex)
            End Try
        End Function

        ''' <summary>grdCons_ColHeadClick: ordena por la columna y busca por ella.</summary>
        Private Sub ElegirColumna(indice As Integer)
            If indice < 0 Then Return
            _columna = grid.Columns(indice).DataPropertyName
            lblBuscar.Text = $"Busca por: {_columna}"
        End Sub

        ''' <summary>cmdExcel_Click: guarda lo que se ve como .xlsx o .txt y lo abre.</summary>
        Private Sub Excel_Clic(sender As Object, e As EventArgs)
            Dim vista = TryCast(grid.DataSource, DataView)
            If vista Is Nothing OrElse vista.Count = 0 Then Return
            Using dialogo As New SaveFileDialog With {.Filter = ExportacionTabla.Filtro, .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                .FileName = $"{If(_consulta?.Nombre, "Consulta")}.xlsx", .OverwritePrompt = True}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                Try
                    Using tabla = vista.ToTable()
                        ExportacionTabla.Guardar(tabla, dialogo.FileName)
                    End Using
                    MessageBox.Show(Me, $"Los datos fueron exportados exitosamente a la siguiente direccion:{vbCrLf}{vbCrLf}{dialogo.FileName}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Process.Start(New ProcessStartInfo(dialogo.FileName) With {.UseShellExecute = True})
                Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is ComponentModel.Win32Exception
                    MessageBox.Show(Me, $"Ocurrió una excepción, no se pudo realizar la exportación a {dialogo.FileName} por la siguente causa:{vbCrLf}{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                End Try
            End Using
        End Sub

        ' ---- Editar ----

        Private Function CodigoEditado() As Long
            Dim item = TryCast(cmbEditar.SelectedItem, ConsultaGuardada)
            If item IsNot Nothing AndAlso cmbEditar.Text = item.ToString() Then Return item.Codigo
            Return Core.Vb6.Val(cmbEditar.Text.Split("-"c)(0))
        End Function

        Private Function NombreEditado() As String
            Dim guion = cmbEditar.Text.IndexOf("-"c)
            Return If(guion < 0, "", cmbEditar.Text.Substring(guion + 1).Trim())
        End Function

        ''' <summary>cmbNConsultas_Click: carga el SQL de la consulta en el editor.</summary>
        Private Async Function CargarSqlAsync() As Task
            Dim item = TryCast(cmbEditar.SelectedItem, ConsultaGuardada)
            If _pintando OrElse item Is Nothing Then Return
            Try
                txtSql.Text = Await _repo.SqlAsync(item.Codigo)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>
        ''' cmdGuardar_Click: guarda el SQL con el código y el nombre escritos ("29 - NOMBRE" crea una consulta nueva) y
        ''' lo replica.
        ''' </summary>
        Private Async Function GuardarAsync() As Task
            Dim codigo = CodigoEditado()
            Dim nombre = NombreEditado()
            If codigo <= 0 OrElse nombre.Length = 0 Then
                MessageBox.Show(Me, "Escriba la consulta como ""código - nombre"", por ejemplo ""29 - VENTAS DEL DÍA"".", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                cmbEditar.Focus()
                Return
            End If
            Try
                ConsultasGuardadasRepositorio.UnaSentencia(txtSql.Text)
            Catch ex As ArgumentException
                MessageBox.Show(Me, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End Try
            If MessageBox.Show(Me, $"¿Desea guardar la consulta {codigo} - {nombre}?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
            Try
                Await _repo.GuardarAsync(codigo, nombre, txtSql.Text, _local)
                Await ConsultasAsync()
                Dim indice = cmbEditar.Items.Cast(Of ConsultaGuardada)().ToList().FindIndex(Function(c) c.Codigo = codigo)
                If indice >= 0 Then
                    _pintando = True
                    cmbEditar.SelectedIndex = indice
                    _pintando = False
                End If
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "posconsultas", Modulo.TituloModulo)
            End Try
        End Function

        ''' <summary>cmdEjecutar_Click (F5): guarda el texto en temp\tmpConsulta.txt y muestra el resultado abajo.</summary>
        Private Async Function EjecutarAsync() As Task
            GuardarTemporal()
            Ocupado(True)
            Try
                Dim tabla = Await _repo.EjecutarAsync(txtSql.Text, _local, False, "", "", ConsultasGuardadasRepositorio.LimiteFilas)
                Dim anterior = TryCast(gridPrueba.DataSource, DataView)
                gridPrueba.DataSource = tabla.DefaultView
                anterior?.Table?.Dispose()
                lblPrueba.Text = $"{tabla.Rows.Count} Registros{If(tabla.Rows.Count >= ConsultasGuardadasRepositorio.LimiteFilas, " (se muestran las primeras 1000)", "")}."
                FiltrarPrueba()
            Catch ex As Exception When TypeOf ex Is MySqlException OrElse TypeOf ex Is ArgumentException
                MostrarError(ex)
            Finally
                Ocupado(False)
            End Try
        End Function

        ''' <summary>El cuadro de búsqueda del SDBuscador: filtra en memoria por cualquier columna.</summary>
        Private Sub FiltrarPrueba()
            Dim vista = TryCast(gridPrueba.DataSource, DataView)
            If vista Is Nothing Then Return
            Dim texto = txtFiltroPrueba.Text.Trim().Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]")
            If texto.Length = 0 Then
                vista.RowFilter = ""
                Return
            End If
            vista.RowFilter = String.Join(" OR ", vista.Table.Columns.Cast(Of DataColumn)().Select(Function(c) $"CONVERT([{c.ColumnName.Replace("]", "\]")}], 'System.String') LIKE '%{texto}%'"))
        End Sub

        Private Sub GuardarTemporal()
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(_archivoTemporal))
                File.WriteAllText(_archivoTemporal, txtSql.Text)
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
            End Try
        End Sub

        ''' <summary>SD_Tablas con el filtro escrito.</summary>
        Private Async Function TablasAsync() As Task
            Try
                Dim tablas = Await _repo.TablasAsync(txtPrefijo.Text.Trim())
                lstTablas.Items.Clear()
                lstTablas.Items.AddRange(tablas.Cast(Of Object)().ToArray())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Function CamposAsync() As Task
            If lstTablas.SelectedItem Is Nothing Then Return
            Try
                Dim campos = Await _repo.CamposAsync(CStr(lstTablas.SelectedItem))
                lstCampos.Items.Clear()
                lstCampos.Items.AddRange(campos.Cast(Of Object)().ToArray())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>lstTablas_DblClick: agrega la tabla, con "SELECT * FROM " si el editor está vacío.</summary>
        Private Sub AgregarTabla()
            If lstTablas.SelectedItem Is Nothing Then Return
            If txtSql.TextLength = 0 Then txtSql.Text = "SELECT * FROM "
            Insertar(CStr(lstTablas.SelectedItem))
        End Sub

        Private Shared Function NombreCampo(linea As String) As String
            Return linea.Split("-"c)(0).Trim()
        End Function

        ''' <summary>VB6 agregaba al final; aquí va donde está el cursor, que al no tocarlo es el final.</summary>
        Private Sub Insertar(texto As String)
            If texto.Length = 0 Then Return
            If Not txtSql.Focused Then txtSql.SelectionStart = txtSql.TextLength
            txtSql.SelectedText = texto
            txtSql.Focus()
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.F5 Then
                tabs.SelectedTab = tabEditar
                btnEjecutar.PerformClick()
                Return True
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

    End Class

End Namespace
