Namespace SD1501

    Partial Public Class FrmEntrega

        Private cmbFiltro As ComboBox
        Private WithEvents txtFiltro As TextBox
        Private rejillaPendientes As DataGridView
        Private WithEvents btnAgregar As Button
        Private WithEvents btnRemover As Button
        Private rejillaEntrega As DataGridView
        Private txtRecibidoPor As TextBox
        Private WithEvents btnEntregar As Button

        Private Sub InitializeComponent()
            cmbFiltro = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Name = "cmbFiltro", .AccessibleName = "Filtrar por", .Width = 145}
            txtFiltro = New TextBox With {.Name = "txtFiltro", .AccessibleName = "Filtrar", .Dock = DockStyle.Fill}
            rejillaPendientes = Rejilla("rejillaPendientes")
            btnAgregar = Boton("&Agregar", "btnAgregar")
            btnRemover = Boton("&Remover", "btnRemover")
            rejillaEntrega = Rejilla("rejillaEntrega")
            txtRecibidoPor = New TextBox With {.Name = "txtRecibidoPor", .AccessibleName = "Recibido por", .MaxLength = 128, .Dock = DockStyle.Fill}
            btnEntregar = Boton("&Entregar", "btnEntregar")

            Dim filtro As New TableLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True, .ColumnCount = 3}
            filtro.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtro.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtro.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            filtro.Controls.Add(Etiqueta("Filtrar"), 0, 0)
            filtro.Controls.Add(cmbFiltro, 1, 0)
            filtro.Controls.Add(txtFiltro, 2, 0)

            Dim botones As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}
            botones.Controls.AddRange(New Control() {btnAgregar, btnRemover})

            Dim pie As New TableLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True, .ColumnCount = 3}
            pie.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            pie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            pie.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            pie.Controls.Add(Etiqueta("Recibido por:"), 0, 0)
            pie.Controls.Add(txtRecibidoPor, 1, 0)
            pie.Controls.Add(btnEntregar, 2, 0)

            Dim marco As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 5, .Padding = New Padding(6)}
            marco.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            marco.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            marco.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            marco.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            marco.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            marco.Controls.Add(filtro, 0, 0)
            marco.Controls.Add(rejillaPendientes, 0, 1)
            marco.Controls.Add(botones, 0, 2)
            marco.Controls.Add(rejillaEntrega, 0, 3)
            marco.Controls.Add(pie, 0, 4)

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(764, 469)
            MinimumSize = New Size(640, 400)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Entrega de Cheques"
            Controls.Add(marco)
            ResumeLayout(False)
        End Sub

        Private Shared Function Etiqueta(texto As String) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Anchor = AnchorStyles.Left}
        End Function

        Private Shared Function Boton(texto As String, nombre As String) As Button
            Return New Button With {.Text = texto, .Name = nombre, .Width = 145, .Height = 25, .UseVisualStyleBackColor = True}
        End Function

        Private Shared Function Rejilla(nombre As String) As DataGridView
            Dim r As New DataGridView With {
                .Name = nombre, .Dock = DockStyle.Fill, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
                .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False
            }
            Dim fecha As New DataGridViewCellStyle With {.Format = "dd/MMM/yyyy"}
            Dim importe As New DataGridViewCellStyle With {.Format = "#,##0.00", .Alignment = DataGridViewContentAlignment.MiddleRight}
            r.Columns.Add(New DataGridViewCheckBoxColumn With {.HeaderText = "", .Width = 27})
            r.Columns.Add(Columna("Cheque", 70, Nothing))
            r.Columns.Add(Columna("Fecha", 85, fecha))
            r.Columns.Add(Columna("ID", 85, Nothing))
            r.Columns.Add(Columna("Codigo", 55, Nothing))
            r.Columns.Add(Columna("Nombre", 240, Nothing))
            r.Columns.Add(Columna("Monto", 95, importe))
            r.Columns.Add(Columna("Detalle", 60, Nothing))
            r.Columns(r.Columns.Count - 1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            Return r
        End Function

        Private Shared Function Columna(titulo As String, ancho As Integer, estilo As DataGridViewCellStyle) As DataGridViewTextBoxColumn
            Dim c As New DataGridViewTextBoxColumn With {.HeaderText = titulo, .Width = ancho, .ReadOnly = True}
            If estilo IsNot Nothing Then c.DefaultCellStyle = estilo
            Return c
        End Function

    End Class

End Namespace
