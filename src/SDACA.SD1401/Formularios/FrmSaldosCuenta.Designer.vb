Namespace SD1401

    Partial Public Class FrmSaldosCuenta

        Private WithEvents cmbBancos As ComboBox
        Private WithEvents cmbCuentas As ComboBox
        Private txtMoneda As TextBox
        Private WithEvents cmbPeriodos As ComboBox
        Private rejilla As DataGridView
        Private txtSaldoActual As TextBox
        Private WithEvents btnSalir As Button

        Private Sub InitializeComponent()
            cmbBancos = Lista("cmbBancos", "Banco", 34, 510)
            cmbCuentas = Lista("cmbCuentas", "Cuenta", 66, 510)
            txtMoneda = New TextBox With {.Name = "txtMoneda", .AccessibleName = "Moneda", .ReadOnly = True, .TabStop = False, .Location = New Point(90, 98), .Width = 510}

            Dim datos As New GroupBox With {.Name = "gbDatos", .Text = "Información de la Cuenta", .Location = New Point(10, 8), .Size = New Size(614, 134)}
            datos.Controls.AddRange(New Control() {
                Etiqueta("Banco", 12, 37), cmbBancos, Etiqueta("Cuenta #", 12, 69), cmbCuentas, Etiqueta("Moneda", 12, 101), txtMoneda
            })

            cmbPeriodos = Lista("cmbPeriodos", "Periodo", 150, 170)

            rejilla = New DataGridView With {
                .Name = "rejilla", .Location = New Point(10, 184), .Size = New Size(614, 356), .ReadOnly = True,
                .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False,
                .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False
            }
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Mes", .Width = 94, .SortMode = DataGridViewColumnSortMode.NotSortable})
            rejilla.Columns.Add(Monto("Saldo Inicial"))
            rejilla.Columns.Add(Monto("Débito"))
            rejilla.Columns.Add(Monto("Crédito"))
            rejilla.Columns.Add(Monto("Saldo Final"))

            txtSaldoActual = New TextBox With {
                .Name = "txtSaldoActual", .AccessibleName = "Saldo actual", .ReadOnly = True, .TabStop = False, .TextAlign = HorizontalAlignment.Right,
                .Location = New Point(292, 550), .Width = 222
            }
            btnSalir = New Button With {.Text = "&Salir", .Name = "btnSalir", .Location = New Point(524, 548), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(634, 590)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Saldos por Cuenta"
            CancelButton = btnSalir
            Controls.AddRange(New Control() {
                datos, Etiqueta("Periodo", 22, 153), cmbPeriodos, rejilla, Etiqueta("Saldo Actual", 200, 553), txtSaldoActual, btnSalir
            })
            ResumeLayout(False)
        End Sub

        Private Shared Function Lista(nombre As String, accesible As String, y As Integer, ancho As Integer) As ComboBox
            Return New ComboBox With {.Name = nombre, .AccessibleName = accesible, .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, y), .Width = ancho}
        End Function

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Shared Function Monto(titulo As String) As DataGridViewTextBoxColumn
            Dim columna As New DataGridViewTextBoxColumn With {.HeaderText = titulo, .Width = 128, .SortMode = DataGridViewColumnSortMode.NotSortable}
            columna.DefaultCellStyle.Format = "#,##0.00"
            columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            columna.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight
            Return columna
        End Function

    End Class

End Namespace
