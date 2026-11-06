''' <summary>
''' Reemplaza a Comun\FrmBuscador.frm con SDBuscador3.ocx. Muestra una tabla ya cargada y la filtra
''' por texto en cualquier columna. Todavía no trae las opciones del OCX (mínimo de caracteres,
''' orden y cantidad de registros guardados en sdconfig.ini).
''' </summary>
Public Class FrmBuscador
    Inherits Form

    Private ReadOnly _vista As DataView
    Private ReadOnly _filtro As New TextBox()
    Private ReadOnly _rejilla As New DataGridView()

    Public Sub New(titulo As String, datos As DataTable)
        _vista = datos.DefaultView
        Text = titulo
        StartPosition = FormStartPosition.CenterParent
        ClientSize = New Size(632, 446)
        MinimizeBox = False
        ShowInTaskbar = False

        Dim etiqueta As New Label With {.Text = "Buscar:", .AutoSize = True, .Location = New Point(12, 15)}
        _filtro.SetBounds(70, 12, 550, 23)
        _filtro.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        AddHandler _filtro.TextChanged, AddressOf Filtrar
        AddHandler _filtro.KeyDown, AddressOf Filtro_KeyDown

        _rejilla.SetBounds(12, 44, 608, 354)
        _rejilla.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        _rejilla.ReadOnly = True
        _rejilla.AllowUserToAddRows = False
        _rejilla.AllowUserToDeleteRows = False
        _rejilla.MultiSelect = False
        _rejilla.RowHeadersVisible = False
        _rejilla.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _rejilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _rejilla.DataSource = _vista
        AddHandler _rejilla.CellDoubleClick, AddressOf Rejilla_CellDoubleClick

        Dim aceptar As New Button With {.Text = "&Aceptar", .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right, .UseVisualStyleBackColor = True}
        aceptar.SetBounds(444, 408, 85, 28)
        AddHandler aceptar.Click, Sub() Aceptar_Seleccion()
        Dim cancelar As New Button With {.Text = "&Cancelar", .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right, .DialogResult = DialogResult.Cancel, .UseVisualStyleBackColor = True}
        cancelar.SetBounds(535, 408, 85, 28)

        CancelButton = cancelar
        AcceptButton = aceptar
        Controls.AddRange(New Control() {etiqueta, _filtro, _rejilla, aceptar, cancelar})
    End Sub

    ''' <summary>Valor de la columna en la fila elegida, o "" si no hay. Igual que <c>Resultado</c> del original.</summary>
    Public ReadOnly Property Resultado(columna As Integer) As String
        Get
            Dim fila = _rejilla.CurrentRow
            If fila Is Nothing OrElse columna >= fila.Cells.Count Then Return ""
            Return Convert.ToString(fila.Cells(columna).Value)
        End Get
    End Property

    Private Sub Filtrar(sender As Object, e As EventArgs)
        Dim texto = EscaparLike(_filtro.Text)
        If texto.Length = 0 Then
            _vista.RowFilter = ""
            Return
        End If
        Dim condiciones = _vista.Table.Columns.Cast(Of DataColumn)().
            Select(Function(c) "CONVERT([" & c.ColumnName & "], 'System.String') LIKE '%" & texto & "%'")
        _vista.RowFilter = String.Join(" OR ", condiciones)
    End Sub

    ''' <summary>Escapa lo que DataView.RowFilter trata como comodín o como fin de texto.</summary>
    Friend Shared Function EscaparLike(texto As String) As String
        Dim b As New Text.StringBuilder()
        For Each c In texto
            Select Case c
                Case "'"c : b.Append("''")
                Case "["c, "]"c, "%"c, "*"c : b.Append("["c).Append(c).Append("]"c)
                Case Else : b.Append(c)
            End Select
        Next
        Return b.ToString()
    End Function

    Private Sub Filtro_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Down AndAlso _rejilla.Rows.Count > 0 Then
            _rejilla.Focus()
            e.Handled = True
        End If
    End Sub

    Private Sub Rejilla_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then Aceptar_Seleccion()
    End Sub

    Private Sub Aceptar_Seleccion()
        If _rejilla.CurrentRow Is Nothing Then Return
        DialogResult = DialogResult.OK
    End Sub

End Class
