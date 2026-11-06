Namespace SD1501

    Partial Public Class FrmPrintCheque

        Private tabs As TabControl
        Private pagConta As TabPage
        Private WithEvents cmbBancos As ComboBox
        Private WithEvents cmbCuentas As ComboBox
        Private gbCheque As GroupBox
        Private WithEvents txtNumero As TextBox
        Private WithEvents btnRefrescar As Button
        Private dtpFecha As DateTimePicker
        Private WithEvents txtId As TextBox
        Private WithEvents txtCodigo As TextBox
        Private WithEvents txtNombre As TextBox
        Private WithEvents txtMonto As TextBox
        Private lblLetras As Label
        Private txtDetalle As TextBox

        Private WithEvents cmbTipo As ComboBox
        Private WithEvents txtCuenta As TextBox
        Private WithEvents btnBuscarCuenta As Button
        Private txtDescripcion As TextBox
        Private cmbDebeHaber As ComboBox
        Private txtMontoLinea As TextBox
        Private WithEvents btnIncluir As Button
        Private WithEvents btnRemover As Button
        Private WithEvents btnMarcarTodo As Button
        Private WithEvents btnMarcarNada As Button
        Private lblFaltante As Label
        Private txtDebe As TextBox
        Private txtHaber As TextBox
        Private WithEvents rejilla As DataGridView

        Private cmbSalida As ComboBox
        Private WithEvents btnImprimir As Button
        Private WithEvents btnSalir As Button

        Private Sub InitializeComponent()
            tabs = New SDACA.Ui.Pestanas With {.Name = "tabs", .Location = New Point(6, 6), .Size = New Size(638, 486)}
            Dim pagEncabezado As New TabPage("Encabezado")
            pagConta = New TabPage("Registro Contable")
            tabs.TabPages.AddRange({pagEncabezado, pagConta})
            CrearEncabezado(pagEncabezado)
            CrearContabilidad(pagConta)

            cmbSalida = New ComboBox With {.Name = "cmbSalida", .AccessibleName = "Salida", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(300, 499), .Width = 150}
            cmbSalida.Items.AddRange(New Object() {"Impresora configurada", "PDF", "(No Imprimir)"})
            btnImprimir = New Button With {.Name = "btnImprimir", .Text = "&Imprimir", .Location = New Point(456, 498), .Size = New Size(92, 25), .UseVisualStyleBackColor = True}
            btnSalir = New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(552, 498), .Size = New Size(92, 25), .UseVisualStyleBackColor = True}

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(650, 532)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Impresión de Cheques"
            CancelButton = btnSalir
            Controls.AddRange(New Control() {tabs, Etiqueta("Recuerde verificar la impresora y el papel.", 6, 503), cmbSalida, btnImprimir, btnSalir})
            ResumeLayout(False)
        End Sub

        Private Sub CrearEncabezado(pagina As TabPage)
            Dim gbCuenta As New GroupBox With {.Text = "Información de la Cuenta", .Location = New Point(6, 6), .Size = New Size(618, 84)}
            cmbBancos = Lista("cmbBancos", "Banco", 88, 20, 520)
            cmbCuentas = Lista("cmbCuentas", "Cuenta #", 88, 50, 520)
            gbCuenta.Controls.AddRange(New Control() {Etiqueta("Banco", 10, 23), cmbBancos, Etiqueta("Cuenta #", 10, 53), cmbCuentas})

            gbCheque = New GroupBox With {.Name = "gbCheque", .Location = New Point(6, 94), .Size = New Size(618, 172)}
            txtNumero = Caja("txtNumero", "Numero de Cheque", 130, 18, 150)
            txtNumero.MaxLength = 16
            btnRefrescar = New Button With {.Name = "btnRefrescar", .Text = "↻", .AccessibleName = "Actualizar número", .Location = New Point(284, 17), .Size = New Size(26, 25), .UseVisualStyleBackColor = True}
            dtpFecha = New DateTimePicker With {.Name = "dtpFecha", .AccessibleName = "Fecha", .Format = DateTimePickerFormat.Short, .Location = New Point(410, 18), .Width = 150}
            txtId = Caja("txtId", "Identificación", 130, 48, 180)
            txtCodigo = Caja("txtCodigo", "Código", 410, 48, 150)
            txtNombre = Caja("txtNombre", "Nombre", 72, 78, 340)
            txtNombre.MaxLength = 128
            txtMonto = Caja("txtMonto", "Monto", 478, 78, 130)
            txtMonto.TextAlign = HorizontalAlignment.Right
            txtMonto.MaxLength = 20
            lblLetras = New Label With {.Name = "lblLetras", .Location = New Point(10, 110), .Size = New Size(598, 54)}
            gbCheque.Controls.AddRange(New Control() {
                Etiqueta("Numero de Cheque", 10, 21), txtNumero, btnRefrescar, Etiqueta("Fecha", 360, 21), dtpFecha,
                Etiqueta("Identificación", 10, 51), txtId, Etiqueta("Código", 354, 51), txtCodigo,
                Etiqueta("Nombre", 10, 81), txtNombre, Etiqueta("Monto", 428, 81), txtMonto, lblLetras
            })

            txtDetalle = New TextBox With {.Name = "txtDetalle", .AccessibleName = "Detalle", .Multiline = True, .ScrollBars = ScrollBars.Vertical, .AcceptsReturn = True, .MaxLength = 500, .Location = New Point(6, 288), .Size = New Size(618, 164)}
            pagina.Controls.AddRange(New Control() {gbCuenta, gbCheque, Etiqueta("Detalle", 6, 271), txtDetalle})
        End Sub

        Private Sub CrearContabilidad(pagina As TabPage)
            cmbTipo = Lista("cmbTipo", "Tipo de asiento", 6, 8, 618)

            Dim gbLinea As New GroupBox With {.Location = New Point(6, 34), .Size = New Size(618, 84)}
            txtCuenta = Caja("txtCuenta", "Cuenta", 62, 16, 132)
            txtCuenta.MaxLength = 16
            btnBuscarCuenta = New Button With {.Name = "btnBuscarCuenta", .Text = "...", .AccessibleName = "Buscar cuenta", .Location = New Point(198, 15), .Size = New Size(28, 25), .UseVisualStyleBackColor = True}
            txtDescripcion = Caja("txtDescripcion", "Descripción", 230, 16, 380)
            txtDescripcion.ReadOnly = True
            txtDescripcion.TabStop = False
            cmbDebeHaber = Lista("cmbDebeHaber", "Debe o haber", 10, 48, 98)
            cmbDebeHaber.Items.AddRange(New Object() {"Debe", "Haber"})
            txtMontoLinea = Caja("txtMontoLinea", "Monto", 170, 48, 132)
            txtMontoLinea.TextAlign = HorizontalAlignment.Right
            btnIncluir = New Button With {.Name = "btnIncluir", .Text = "Incluir Cuenta", .Location = New Point(312, 47), .Size = New Size(107, 25), .UseVisualStyleBackColor = True}
            btnRemover = New Button With {.Name = "btnRemover", .Text = "&Remover", .Location = New Point(424, 47), .Size = New Size(107, 25), .UseVisualStyleBackColor = True}
            btnMarcarTodo = New Button With {.Name = "btnMarcarTodo", .Text = "☑", .AccessibleName = "Marcar todas", .Location = New Point(538, 47), .Size = New Size(30, 25), .UseVisualStyleBackColor = True}
            btnMarcarNada = New Button With {.Name = "btnMarcarNada", .Text = "☐", .AccessibleName = "Desmarcar todas", .Location = New Point(576, 47), .Size = New Size(30, 25), .UseVisualStyleBackColor = True}
            gbLinea.Controls.AddRange(New Control() {
                Etiqueta("Cuenta:", 10, 19), txtCuenta, btnBuscarCuenta, txtDescripcion,
                cmbDebeHaber, Etiqueta("Monto:", 120, 51), txtMontoLinea, btnIncluir, btnRemover, btnMarcarTodo, btnMarcarNada
            })

            lblFaltante = New Label With {.Name = "lblFaltante", .AutoSize = True, .Location = New Point(6, 127)}
            txtDebe = Caja("txtDebe", "Total debe", 396, 124, 112)
            txtHaber = Caja("txtHaber", "Total haber", 512, 124, 112)
            For Each total In {txtDebe, txtHaber}
                total.ReadOnly = True
                total.TabStop = False
                total.TextAlign = HorizontalAlignment.Right
            Next

            rejilla = New DataGridView With {
                .Name = "rejilla", .Location = New Point(6, 152), .Size = New Size(618, 300), .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
                .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False
            }
            Dim importe As New DataGridViewCellStyle With {.Format = "#,##0.00", .Alignment = DataGridViewContentAlignment.MiddleRight}
            rejilla.Columns.Add(New DataGridViewCheckBoxColumn With {.HeaderText = "", .Width = 26})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "C.Contable", .Width = 113, .ReadOnly = True})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Detalle", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .ReadOnly = True})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Debe", .Width = 100, .DefaultCellStyle = importe})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Haber", .Width = 100, .DefaultCellStyle = importe})

            pagina.Controls.AddRange(New Control() {cmbTipo, gbLinea, lblFaltante, txtDebe, txtHaber, rejilla})
        End Sub

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Shared Function Caja(nombre As String, accesible As String, x As Integer, y As Integer, ancho As Integer) As TextBox
            Return New TextBox With {.Name = nombre, .AccessibleName = accesible, .Location = New Point(x, y), .Width = ancho}
        End Function

        Private Shared Function Lista(nombre As String, accesible As String, x As Integer, y As Integer, ancho As Integer) As ComboBox
            Return New ComboBox With {.Name = nombre, .AccessibleName = accesible, .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(x, y), .Width = ancho}
        End Function

    End Class

End Namespace
