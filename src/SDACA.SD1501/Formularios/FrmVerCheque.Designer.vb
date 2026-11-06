Namespace SD1501

    Partial Public Class FrmVerCheque

        Private WithEvents cmbBancos As ComboBox
        Private WithEvents cmbCuentas As ComboBox
        Private WithEvents txtNumCheque As TextBox
        Private WithEvents btnBuscar As Button
        Private WithEvents btnSalir As Button
        Private gbCheque As GroupBox
        Private lblConciliado As Label
        Private tabs As TabControl
        Private txtFecha As TextBox
        Private txtNombre As TextBox
        Private txtMonto As TextBox
        Private lblLetras As Label
        Private txtCodigoSeguridad As TextBox
        Private txtUsuario As TextBox
        Private lblDetalle As Label
        Private rejilla As DataGridView

        Private Sub InitializeComponent()
            cmbBancos = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Name = "cmbBancos", .AccessibleName = "Banco"}
            cmbCuentas = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Name = "cmbCuentas", .AccessibleName = "Cuenta #"}
            txtNumCheque = New TextBox With {.Name = "txtNumCheque", .AccessibleName = "Ver Cheque #"}
            btnBuscar = New Button With {.Text = "&Buscar Cheques Emitidos", .Name = "btnBuscar", .UseVisualStyleBackColor = True}
            btnSalir = New Button With {.Text = "&Salir", .Name = "btnSalir", .UseVisualStyleBackColor = True}
            gbCheque = New GroupBox With {.Name = "gbCheque", .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}
            lblConciliado = New Label With {.Text = "Conciliado", .Name = "lblConciliado", .ForeColor = SDACA.Ui.Tema.Acento, .AutoSize = True, .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            tabs = New SDACA.Ui.Pestanas With {.Name = "tabs", .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}
            txtFecha = Lectura("txtFecha")
            txtNombre = Lectura("txtNombre")
            txtMonto = Lectura("txtMonto")
            txtMonto.TextAlign = HorizontalAlignment.Right
            lblLetras = New Label With {.Name = "lblLetras", .BorderStyle = BorderStyle.Fixed3D}
            txtCodigoSeguridad = Lectura("txtCodigoSeguridad")
            txtUsuario = Lectura("txtUsuario")
            lblDetalle = New Label With {.Name = "lblDetalle", .Dock = DockStyle.Fill, .Padding = New Padding(6)}
            rejilla = New DataGridView With {
                .Name = "rejilla", .Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
                .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False
            }

            Dim importe As New DataGridViewCellStyle With {.Format = "#,##0.00", .Alignment = DataGridViewContentAlignment.MiddleRight}
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "C.Contable", .Width = 110})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Detalle", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Debe", .Width = 100, .DefaultCellStyle = importe})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Haber", .Width = 100, .DefaultCellStyle = importe})

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(640, 470)
            MinimumSize = New Size(656, 509)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Consulta de Cheques"
            CancelButton = btnSalir

            Controls.Add(Etiqueta("Banco", 12, 15))
            cmbBancos.SetBounds(90, 12, 300, 23)
            Controls.Add(Etiqueta("Cuenta #", 12, 44))
            cmbCuentas.SetBounds(90, 41, 300, 23)
            Controls.Add(Etiqueta("Ver Cheque #", 12, 73))
            txtNumCheque.SetBounds(90, 70, 120, 23)
            btnBuscar.SetBounds(410, 12, 218, 28)
            btnSalir.SetBounds(410, 46, 218, 28)
            Controls.AddRange(New Control() {cmbBancos, cmbCuentas, txtNumCheque, btnBuscar, btnSalir})

            gbCheque.SetBounds(12, 102, 616, 356)
            lblConciliado.Location = New Point(540, 4)
            tabs.SetBounds(10, 22, 596, 324)

            Dim pagCheque As New TabPage("Cheque")
            pagCheque.Controls.Add(Etiqueta("Fecha", 12, 17))
            txtFecha.SetBounds(140, 14, 110, 23)
            pagCheque.Controls.Add(Etiqueta("Nombre", 12, 47))
            txtNombre.SetBounds(140, 44, 430, 23)
            pagCheque.Controls.Add(Etiqueta("Monto", 12, 77))
            txtMonto.SetBounds(140, 74, 150, 23)
            pagCheque.Controls.Add(Etiqueta("Monto En Letras", 12, 107))
            lblLetras.SetBounds(140, 104, 430, 54)
            pagCheque.Controls.Add(Etiqueta("Código de Seguridad", 12, 171))
            txtCodigoSeguridad.SetBounds(140, 168, 430, 23)
            pagCheque.Controls.Add(Etiqueta("Usuario Creación", 12, 201))
            txtUsuario.SetBounds(140, 198, 200, 23)
            pagCheque.Controls.AddRange(New Control() {txtFecha, txtNombre, txtMonto, lblLetras, txtCodigoSeguridad, txtUsuario})

            Dim pagDetalle As New TabPage("Detalle")
            pagDetalle.Controls.Add(lblDetalle)
            Dim pagConta As New TabPage("Inf. Contable")
            pagConta.Controls.Add(rejilla)

            tabs.TabPages.AddRange({pagCheque, pagDetalle, pagConta})
            gbCheque.Controls.Add(lblConciliado)
            gbCheque.Controls.Add(tabs)
            Controls.Add(gbCheque)
            ResumeLayout(False)
            PerformLayout()
        End Sub

        Private Shared Function Lectura(nombre As String) As TextBox
            Return New TextBox With {.Name = nombre, .ReadOnly = True, .TabStop = False}
        End Function

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

    End Class

End Namespace
