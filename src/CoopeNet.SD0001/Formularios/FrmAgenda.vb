Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Catalogos
Imports CoopeNet.Ui

Namespace SD0001

    ''' <summary>
    ''' Agenda Telefónica. Migración de Fuentes\SD0001\FrmAgenda.frm. A la izquierda las personas activas y no
    ''' fallecidas que coinciden con lo digitado; a la derecha su ficha en HTML, que se imprime, se copia o se
    ''' guarda desde el menú, igual que en VB6. Los enlaces Nuevo, Modificar e Imprimir de la ficha pasan a
    ''' botones. Nuevo y Editar abren Datos Personales, con sus propios privilegios.
    ''' </summary>
    Public Class FrmAgenda
        Inherits Form

        Private Const LargoMinimo As Integer = 3

        Private ReadOnly _repo As AgendaRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _abrirDatosPersonales As Func(Of String, Form)
        Private ReadOnly _marco As MarcoAgenda
        Private _personas As New List(Of PersonaAgenda)
        Private _idActual As String = ""
        Private _versionLista As Integer
        Private _versionFicha As Integer

        Private ReadOnly menuAgenda As New MenuStrip With {.Name = "menuAgenda"}
        Private ReadOnly division As New SplitContainer With {.Name = "division", .Dock = DockStyle.Fill, .FixedPanel = FixedPanel.Panel1}
        Private ReadOnly txtBuscar As New TextBox With {.Name = "txtBuscar", .AccessibleName = "Nombre, código o identificación", .CharacterCasing = CharacterCasing.Upper, .Dock = DockStyle.Top}
        Private ReadOnly grid As New DataGridView With {.Name = "gridPersonas", .AccessibleName = "Personas", .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .ReadOnly = True, .MultiSelect = False,
            .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect}
        Private ReadOnly navegador As New WebBrowser With {.Name = "navegador", .AccessibleName = "Ficha", .Dock = DockStyle.Fill, .AllowWebBrowserDrop = False, .IsWebBrowserContextMenuEnabled = False, .WebBrowserShortcutsEnabled = True}
        Private ReadOnly btnNuevo As New Button With {.Name = "btnNuevo", .Text = "&Nuevo", .Size = New Size(100, 30)}
        Private ReadOnly btnModificar As New Button With {.Name = "btnModificar", .Text = "&Modificar", .Size = New Size(100, 30), .Enabled = False}
        Private ReadOnly btnImprimir As New Button With {.Name = "btnImprimir", .Text = "&Imprimir", .Size = New Size(100, 30)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .DialogResult = DialogResult.Cancel, .Size = New Size(100, 30)}
        Private ReadOnly tmrBuscar As New Timer With {.Interval = 400}

        Public Sub New(repositorio As AgendaRepositorio, local As Long, marco As MarcoAgenda, abrirDatosPersonales As Func(Of String, Form))
            _repo = repositorio
            _local = local
            _marco = marco
            _abrirDatosPersonales = abrirDatosPersonales
            Text = "Agenda Telefónica - " & Date.Today.ToString("D", CultureInfo.GetCultureInfo("es-CR"))
            ClientSize = New Size(980, 600)
            MinimumSize = New Size(720, 420)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir

            Dim archivo As New ToolStripMenuItem("&Archivo")
            archivo.DropDownItems.Add("Imprimir", Nothing, Sub() navegador.ShowPrintDialog())
            archivo.DropDownItems.Add("Vista previa", Nothing, Sub() navegador.ShowPrintPreviewDialog())
            archivo.DropDownItems.Add("Configurar página", Nothing, Sub() navegador.ShowPageSetupDialog())
            archivo.DropDownItems.Add(New ToolStripSeparator())
            archivo.DropDownItems.Add("Salir", Nothing, Sub() Close())
            Dim datos As New ToolStripMenuItem("&Datos Personales")
            datos.DropDownItems.Add("Copiar", Nothing, AddressOf Copiar)
            datos.DropDownItems.Add(New ToolStripSeparator())
            datos.DropDownItems.Add("Nuevo", Nothing, Sub() AbrirDatosPersonales(""))
            Dim editar = datos.DropDownItems.Add("Editar", Nothing, Sub() If _idActual.Length > 0 Then AbrirDatosPersonales(_idActual))
            datos.DropDownItems.Add("Exportar a HTML", Nothing, Sub() navegador.ShowSaveAsDialog())
            AddHandler datos.DropDownOpening, Sub() editar.Enabled = _idActual.Length > 0
            menuAgenda.Items.AddRange({archivo, datos})

            grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colNombre", .HeaderText = "Nombre", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .FillWeight = 200})
            grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colId", .HeaderText = "Identificación", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .FillWeight = 100})

            Dim izquierda As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 0, 4, 0)}
            Dim etiqueta As New Label With {.Text = "Buscar (nombre, código o identificación)", .AutoSize = True, .Dock = DockStyle.Top, .Padding = New Padding(0, 0, 0, 4)}
            izquierda.Controls.AddRange(New Control() {grid, txtBuscar, etiqueta})
            Dim botones As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .FlowDirection = FlowDirection.RightToLeft, .AutoSize = True, .Padding = New Padding(0, 6, 0, 0)}
            botones.Controls.AddRange(New Control() {btnSalir, btnImprimir, btnModificar, btnNuevo})
            Dim derecha As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(4, 0, 0, 0)}
            derecha.Controls.AddRange(New Control() {navegador, botones})
            division.Panel1.Controls.Add(izquierda)
            division.Panel2.Controls.Add(derecha)
            Dim marcoPantalla As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12)}
            marcoPantalla.Controls.Add(division)
            Controls.Add(marcoPantalla)
            Controls.Add(menuAgenda)
            MainMenuStrip = menuAgenda

            AddHandler Load, AddressOf Cargar
            AddHandler txtBuscar.TextChanged, AddressOf Buscar_Cambio
            AddHandler txtBuscar.KeyDown, AddressOf Buscar_Tecla
            AddHandler tmrBuscar.Tick, AddressOf Buscar_Tiempo
            AddHandler grid.SelectionChanged, AddressOf Persona_Seleccionada
            AddHandler btnNuevo.Click, Sub() AbrirDatosPersonales("")
            AddHandler btnModificar.Click, Sub() If _idActual.Length > 0 Then AbrirDatosPersonales(_idActual)
            AddHandler btnImprimir.Click, Sub() navegador.ShowPrintPreviewDialog()
        End Sub

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            division.SplitterDistance = division.Width * 2 \ 5
            Try
                _marco.Telefono = Await _repo.TelefonoLocalAsync(_local)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            Pintar("", Nothing)
            txtBuscar.Focus()
        End Sub

        Private Sub Pintar(id As String, ficha As FichaAgenda)
            _idActual = If(ficha Is Nothing, "", ficha.Id)
            btnModificar.Enabled = _idActual.Length > 0
            _marco.Fecha = Date.Now
            navegador.DocumentText = AgendaRepositorio.Html(id, ficha, _marco)
        End Sub

        Private Sub Buscar_Cambio(sender As Object, e As EventArgs)
            tmrBuscar.Stop()
            If txtBuscar.Text.Trim().Length >= LargoMinimo Then tmrBuscar.Start()
        End Sub

        Private Async Sub Buscar_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                e.SuppressKeyPress = True
                tmrBuscar.Stop()
                Await ListarAsync()
            ElseIf e.KeyCode = Keys.Down AndAlso grid.Rows.Count > 0 Then
                e.Handled = True
                grid.Focus()
            End If
        End Sub

        Private Async Sub Buscar_Tiempo(sender As Object, e As EventArgs)
            tmrBuscar.Stop()
            Await ListarAsync()
        End Sub

        Private Async Function ListarAsync() As Task
            _versionLista += 1
            Dim version = _versionLista
            UseWaitCursor = True
            Try
                Dim personas = Await _repo.ListarAsync(txtBuscar.Text)
                If version <> _versionLista Then Return
                _personas = personas
                grid.Rows.Clear()
                For Each p In _personas
                    grid.Rows.Add(p.Nombre, p.Id)
                Next
                If _personas.Count = 0 Then Pintar(txtBuscar.Text.Trim(), Nothing)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                If version = _versionLista Then UseWaitCursor = False
            End Try
        End Function

        Private Async Sub Persona_Seleccionada(sender As Object, e As EventArgs)
            Dim fila = grid.CurrentRow
            If fila Is Nothing OrElse fila.Index >= _personas.Count Then Return
            Await MostrarAsync(_personas(fila.Index).Id)
        End Sub

        Private Async Function MostrarAsync(id As String) As Task
            _versionFicha += 1
            Dim version = _versionFicha
            Try
                Dim ficha = Await _repo.FichaAsync(id)
                If version = _versionFicha Then Pintar(id, ficha)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Sub AbrirDatosPersonales(id As String)
            Using ventana = _abrirDatosPersonales(id)
                Tema.Aplicar(ventana, Modulo.TituloModulo)
                ventana.ShowDialog(Me)
            End Using
            If _idActual.Length > 0 Then Await MostrarAsync(_idActual)
        End Sub

        Private Sub Copiar(sender As Object, e As EventArgs)
            If navegador.Document Is Nothing Then Return
            navegador.Document.ExecCommand("SelectAll", False, Nothing)
            navegador.Document.ExecCommand("Copy", False, Nothing)
            navegador.Document.ExecCommand("Unselect", False, Nothing)
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then tmrBuscar.Dispose()
            MyBase.Dispose(disposing)
        End Sub

    End Class

End Namespace
