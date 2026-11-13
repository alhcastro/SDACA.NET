Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Buscador del catálogo CABYS. Migración de SD0502\frmCatCatal.frm en modo "Buscar", el que abre el botón del
    ''' código de Hacienda en el Catálogo de Artículos: árbol del catálogo, búsqueda por descripción o código, y los
    ''' artículos del local que ya usan el código o la rama elegida. Asignar devuelve un código de 13 dígitos (VB6
    ''' aceptaba cualquier nivel del árbol). El modo de asignación masiva (opción 091 de Inventario) no está aquí.
    ''' </summary>
    Public Class FrmCabys
        Inherits Form

        Private Const Cargando As String = "…"

        Private ReadOnly _repo As CabysRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _inicial As String
        Private _catalogo As CatalogoCabys
        Private _seleccion As NodoCabys
        Private _versionArticulos As Integer

        ''' <summary>El código elegido al aceptar.</summary>
        Public ReadOnly Property Codigo As String = ""

        Private ReadOnly pnlBuscar As New Panel With {.Name = "pnlBuscar", .Dock = DockStyle.Top, .Height = 44}
        Private ReadOnly txtBuscar As New TextBox With {.Name = "txtBuscar", .AccessibleName = "Buscar por descripción o código", .Location = New Point(70, 12), .Width = 420}
        Private ReadOnly lblRegistros As New Label With {.Name = "lblRegistros", .AutoSize = True, .Location = New Point(500, 15)}
        Private ReadOnly division As New SplitContainer With {.Name = "division", .Dock = DockStyle.Fill, .Orientation = Orientation.Vertical}
        Private ReadOnly gridResultados As New DataGridView With {.Name = "gridResultados", .AccessibleName = "Códigos encontrados", .Dock = DockStyle.Top, .Height = 190,
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .MultiSelect = False, .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
        Private ReadOnly colCodigo As New DataGridViewTextBoxColumn With {.Name = "colCodigo", .HeaderText = "Código", .FillWeight = 34}
        Private ReadOnly colDescripcion As New DataGridViewTextBoxColumn With {.Name = "colDescripcion", .HeaderText = "Descripción", .FillWeight = 100}
        Private ReadOnly colIva As New DataGridViewTextBoxColumn With {.Name = "colIva", .HeaderText = "IVA %", .FillWeight = 14}
        Private ReadOnly arbol As New TreeView With {.Name = "arbol", .AccessibleName = "Catálogo CABYS", .Dock = DockStyle.Fill, .HideSelection = False}
        Private ReadOnly lblArticulos As New Label With {.Name = "lblArticulos", .Dock = DockStyle.Top, .Height = 26, .TextAlign = ContentAlignment.MiddleLeft}
        Private ReadOnly gridArticulos As New DataGridView With {.Name = "gridArticulos", .AccessibleName = "Artículos con este código", .Dock = DockStyle.Fill,
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .MultiSelect = False, .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
        Private ReadOnly pnlBotones As New Panel With {.Name = "pnlBotones", .Dock = DockStyle.Bottom, .Height = 50}
        Private ReadOnly lblSeleccion As New Label With {.Name = "lblSeleccion", .AutoEllipsis = True, .Location = New Point(12, 16), .Size = New Size(700, 20)}
        Private ReadOnly btnAsignar As New Button With {.Name = "btnAsignar", .Text = "&Asignar", .Enabled = False, .Location = New Point(782, 10), .Size = New Size(100, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .DialogResult = DialogResult.Cancel, .Location = New Point(888, 10), .Size = New Size(100, 30)}
        Private ReadOnly reloj As New Timer With {.Interval = 400}

        ''' <param name="inicial">El código que ya tiene el artículo, para mostrarlo al abrir.</param>
        Public Sub New(repositorio As CabysRepositorio, local As Long, inicial As String)
            _repo = repositorio
            _local = local
            _inicial = If(inicial, "").Trim()
            Text = "Catálogo CABYS"
            ClientSize = New Size(1000, 620)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnCancelar
            AcceptButton = btnAsignar

            gridResultados.Columns.AddRange(colCodigo, colDescripcion, colIva)
            colIva.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            pnlBuscar.Controls.AddRange(New Control() {New Label With {.Text = "Buscar", .AutoSize = True, .Location = New Point(12, 15)}, txtBuscar, lblRegistros})
            ' Con Dock, el último control agregado se acomoda primero: lo que llena va antes.
            division.Panel1.Controls.Add(arbol)
            division.Panel1.Controls.Add(gridResultados)
            division.Panel2.Controls.Add(gridArticulos)
            division.Panel2.Controls.Add(lblArticulos)
            btnAsignar.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            btnCancelar.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            lblSeleccion.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            pnlBotones.Controls.AddRange(New Control() {lblSeleccion, btnAsignar, btnCancelar})
            Controls.Add(division)
            Controls.Add(pnlBotones)
            Controls.Add(pnlBuscar)
            Ventanas.Recordar(Me)

            AddHandler Load, Async Sub() Await CargarAsync()
            AddHandler Shown, Sub() division.SplitterDistance = division.Width * 55 \ 100
            AddHandler FormClosed, Sub() reloj.Dispose()
            AddHandler txtBuscar.TextChanged, Sub()
                                                  reloj.Stop()
                                                  reloj.Start()
                                              End Sub
            AddHandler reloj.Tick, Sub()
                                       reloj.Stop()
                                       Buscar()
                                   End Sub
            AddHandler gridResultados.CellClick, Sub(s, e) If e.RowIndex >= 0 Then SeleccionarEnArbol(CType(gridResultados.Rows(e.RowIndex).Tag, NodoCabys))
            AddHandler gridResultados.CellDoubleClick, Sub(s, e) If e.RowIndex >= 0 Then Asignar()
            AddHandler arbol.BeforeExpand, AddressOf Arbol_Expandir
            AddHandler arbol.AfterSelect, Async Sub(s, e) Await SeleccionarAsync(CType(e.Node.Tag, NodoCabys))
            AddHandler arbol.NodeMouseDoubleClick, Sub(s, e) If CType(e.Node.Tag, NodoCabys).EsCodigo Then Asignar()
            AddHandler btnAsignar.Click, Sub() Asignar()
        End Sub

        Private Async Function CargarAsync() As Task
            UseWaitCursor = True
            lblRegistros.Text = "Cargando el catálogo…"
            Try
                _catalogo = Await _repo.CatalogoAsync()
                arbol.BeginUpdate()
                For Each n In _catalogo.Raices
                    arbol.Nodes.Add(CrearNodo(n))
                Next
                arbol.EndUpdate()
                lblRegistros.Text = $"{_catalogo.Cantidad.ToString("N0", CultureInfo.CurrentCulture)} códigos"
                If _inicial.Length > 0 Then
                    txtBuscar.Text = _inicial
                    reloj.Stop()
                    Buscar()
                    Dim nodo = _catalogo.Buscar(_inicial)
                    If nodo IsNot Nothing Then SeleccionarEnArbol(nodo)
                End If
            Catch ex As MySqlException
                lblRegistros.Text = ""
                Mensajes.NoConecta()
            Finally
                UseWaitCursor = False
            End Try
            txtBuscar.Focus()
        End Function

        ''' <summary>Un nodo con un hijo de relleno si tiene hijos: se crean al expandirlo.</summary>
        Private Shared Function CrearNodo(n As NodoCabys) As TreeNode
            Dim t As New TreeNode(n.ToString()) With {.Tag = n, .Name = n.Codigo}
            If n.Hijos.Count > 0 Then t.Nodes.Add(New TreeNode(Cargando))
            Return t
        End Function

        Private Shared Sub LlenarHijos(t As TreeNode)
            If t.Nodes.Count <> 1 OrElse t.Nodes(0).Tag IsNot Nothing Then Return
            t.Nodes.Clear()
            For Each h In CType(t.Tag, NodoCabys).Hijos
                t.Nodes.Add(CrearNodo(h))
            Next
        End Sub

        Private Sub Arbol_Expandir(sender As Object, e As TreeViewCancelEventArgs)
            arbol.BeginUpdate()
            LlenarHijos(e.Node)
            arbol.EndUpdate()
        End Sub

        ''' <summary>lstCategorias_Click: lleva el árbol al código, con su rama abierta.</summary>
        Private Sub SeleccionarEnArbol(nodo As NodoCabys)
            If nodo Is Nothing Then Return
            Dim camino As New Stack(Of NodoCabys)
            Dim n = nodo
            While n IsNot Nothing
                camino.Push(n)
                n = n.Padre
            End While
            arbol.BeginUpdate()
            For Each t As TreeNode In arbol.Nodes
                t.Collapse()
            Next
            Dim nodos = arbol.Nodes
            Dim actual As TreeNode = Nothing
            While camino.Count > 0
                Dim buscado = camino.Pop()
                actual = nodos.Cast(Of TreeNode)().FirstOrDefault(Function(t) t.Tag Is buscado)
                If actual Is Nothing Then Exit While
                If camino.Count > 0 Then
                    LlenarHijos(actual)
                    actual.Expand()
                    nodos = actual.Nodes
                End If
            End While
            arbol.EndUpdate()
            If actual IsNot Nothing Then
                arbol.SelectedNode = actual
                actual.EnsureVisible()
            End If
        End Sub

        ''' <summary>Timer1_Timer: busca con más de dos caracteres.</summary>
        Private Sub Buscar()
            If _catalogo Is Nothing Then Return
            Dim texto = txtBuscar.Text.Trim()
            gridResultados.Rows.Clear()
            If texto.Length <= 2 Then
                lblRegistros.Text = $"{_catalogo.Cantidad.ToString("N0", CultureInfo.CurrentCulture)} códigos"
                Return
            End If
            Const maximo As Integer = 500
            Dim lista = _catalogo.Coincidencias(texto, maximo + 1)
            For Each n In lista.Take(maximo)
                Dim i = gridResultados.Rows.Add(n.Codigo, n.Descripcion, If(n.Impuesto.HasValue, n.Impuesto.Value.ToString(CultureInfo.CurrentCulture), ""))
                gridResultados.Rows(i).Tag = n
            Next
            lblRegistros.Text = If(lista.Count > maximo, $"Más de {maximo} coincidencias: precise la búsqueda", $"{lista.Count} coincidencia{If(lista.Count = 1, "", "s")}")
        End Sub

        ''' <summary>TC_MouseDown con SD_CargaArticulos: los artículos que usan el código o la rama.</summary>
        Private Async Function SeleccionarAsync(nodo As NodoCabys) As Task
            If nodo Is Nothing Then Return
            _seleccion = nodo
            lblSeleccion.Text = nodo.ToString() & If(nodo.Impuesto.HasValue, $"  (IVA {nodo.Impuesto.Value} %)", "")
            btnAsignar.Enabled = nodo.EsCodigo
            _versionArticulos += 1
            Dim version = _versionArticulos
            lblArticulos.Text = "Buscando artículos…"
            Try
                Dim datos = Await _repo.ArticulosAsync(nodo.Codigo, _local)
                If version <> _versionArticulos Then
                    datos.Dispose()
                    Return
                End If
                Dim anterior = TryCast(gridArticulos.DataSource, IDisposable)
                gridArticulos.DataSource = datos
                anterior?.Dispose()
                Dim n = datos.Rows.Count
                lblArticulos.Text = If(n >= 1000, "Artículos con este código: los primeros 1000", $"Artículos con este código: {n}")
            Catch ex As MySqlException
                lblArticulos.Text = ""
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>cmdAsignar_Click.</summary>
        Private Sub Asignar()
            Dim nodo = _seleccion
            If gridResultados.Focused AndAlso gridResultados.CurrentRow IsNot Nothing Then nodo = CType(gridResultados.CurrentRow.Tag, NodoCabys)
            If nodo Is Nothing OrElse Not nodo.EsCodigo Then
                MessageBox.Show(Me, "Seleccione un código CABYS de 13 dígitos.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            _Codigo = nodo.Codigo
            DialogResult = DialogResult.OK
        End Sub

    End Class

End Namespace
