Namespace SD1501

    Partial Public Class FrmEditarCheque

        Private WithEvents cmbBancos As ComboBox
        Private WithEvents cmbCuentas As ComboBox
        Private WithEvents txtNumCheque As TextBox
        Private WithEvents btnBuscar As Button
        Private WithEvents btnGuardar As Button
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
        Private txtDetalle As TextBox

        Private WithEvents cmbTipo As ComboBox
        Private gbLinea As GroupBox
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

        Private Sub InitializeComponent()
            cmbBancos = Lista("cmbBancos", "Banco", 76, 10, 340)
            cmbCuentas = Lista("cmbCuentas", "Cuenta #", 76, 39, 340)
            txtNumCheque = Caja("txtNumCheque", "Cheque #", 76, 68, 120)
            btnBuscar = Boton("&Buscar Cheques", "btnBuscar", 428, 8, 216, 26)
            btnGuardar = Boton("&Guardar", "btnGuardar", 428, 38, 106, 26)
            btnSalir = Boton("&Salir", "btnSalir", 538, 38, 106, 26)

            gbCheque = New GroupBox With {.Name = "gbCheque", .Location = New Point(6, 96), .Size = New Size(638, 480)}
            lblConciliado = New Label With {.Text = "Conciliado", .Name = "lblConciliado", .ForeColor = CoopeNet.Ui.Tema.Acento, .AutoSize = True, .Location = New Point(560, 4)}
            tabs = New CoopeNet.Ui.Pestanas With {.Name = "tabs", .Location = New Point(8, 20), .Size = New Size(622, 452)}
            Dim pagCheque As New TabPage("Cheque")
            Dim pagDetalle As New TabPage("Detalle")
            Dim pagConta As New TabPage("Inf. Contable")
            CrearCheque(pagCheque)
            txtDetalle = New TextBox With {.Name = "txtDetalle", .AccessibleName = "Detalle", .Multiline = True, .ScrollBars = ScrollBars.Vertical, .AcceptsReturn = True, .MaxLength = 500, .Dock = DockStyle.Fill}
            pagDetalle.Controls.Add(txtDetalle)
            CrearContabilidad(pagConta)
            tabs.TabPages.AddRange({pagCheque, pagDetalle, pagConta})
            gbCheque.Controls.AddRange(New Control() {lblConciliado, tabs})

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(650, 582)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Editar el Asiento Contable del Cheque"
            CancelButton = btnSalir
            Controls.AddRange(New Control() {
                Etiqueta("Banco", 10, 13), cmbBancos, Etiqueta("Cuenta #", 10, 42), cmbCuentas, Etiqueta("Cheque #", 10, 71), txtNumCheque,
                btnBuscar, btnGuardar, btnSalir, gbCheque
            })
            ResumeLayout(False)
        End Sub

        Private Sub CrearCheque(pagina As TabPage)
            txtFecha = Lectura("txtFecha", 140, 14, 110)
            txtNombre = Lectura("txtNombre", 140, 44, 450)
            txtMonto = Lectura("txtMonto", 140, 74, 150)
            txtMonto.TextAlign = HorizontalAlignment.Right
            lblLetras = New Label With {.Name = "lblLetras", .BorderStyle = BorderStyle.Fixed3D, .Location = New Point(140, 104), .Size = New Size(450, 54)}
            txtCodigoSeguridad = Lectura("txtCodigoSeguridad", 140, 168, 450)
            txtUsuario = Lectura("txtUsuario", 140, 198, 200)
            pagina.Controls.AddRange(New Control() {
                Etiqueta("Fecha", 12, 17), txtFecha, Etiqueta("Nombre", 12, 47), txtNombre, Etiqueta("Monto", 12, 77), txtMonto,
                Etiqueta("Monto En Letras", 12, 107), lblLetras, Etiqueta("Código de Seguridad", 12, 171), txtCodigoSeguridad,
                Etiqueta("Usuario Creación", 12, 201), txtUsuario
            })
        End Sub

        Private Sub CrearContabilidad(pagina As TabPage)
            cmbTipo = Lista("cmbTipo", "Tipo de asiento", 6, 6, 600)
            gbLinea = New GroupBox With {.Name = "gbLinea", .Location = New Point(6, 30), .Size = New Size(600, 82)}
            txtCuenta = Caja("txtCuenta", "Cuenta", 62, 16, 132)
            txtCuenta.MaxLength = 16
            btnBuscarCuenta = Boton("...", "btnBuscarCuenta", 198, 15, 28, 25)
            btnBuscarCuenta.AccessibleName = "Buscar cuenta"
            txtDescripcion = Caja("txtDescripcion", "Descripción", 230, 16, 362)
            txtDescripcion.ReadOnly = True
            txtDescripcion.TabStop = False
            cmbDebeHaber = Lista("cmbDebeHaber", "Debe o haber", 10, 48, 90)
            cmbDebeHaber.Items.AddRange(New Object() {"Debe", "Haber"})
            txtMontoLinea = Caja("txtMontoLinea", "Monto", 160, 48, 120)
            txtMontoLinea.TextAlign = HorizontalAlignment.Right
            btnIncluir = Boton("Incluir Cuenta", "btnIncluir", 288, 47, 107, 25)
            btnRemover = Boton("&Remover", "btnRemover", 400, 47, 107, 25)
            btnMarcarTodo = Boton("☑", "btnMarcarTodo", 522, 47, 30, 25)
            btnMarcarTodo.AccessibleName = "Marcar todas"
            btnMarcarNada = Boton("☐", "btnMarcarNada", 560, 47, 30, 25)
            btnMarcarNada.AccessibleName = "Desmarcar todas"
            gbLinea.Controls.AddRange(New Control() {
                Etiqueta("Cuenta:", 10, 19), txtCuenta, btnBuscarCuenta, txtDescripcion,
                cmbDebeHaber, Etiqueta("Monto:", 110, 51), txtMontoLinea, btnIncluir, btnRemover, btnMarcarTodo, btnMarcarNada
            })

            lblFaltante = New Label With {.Name = "lblFaltante", .AutoSize = True, .Location = New Point(6, 120)}
            txtDebe = Caja("txtDebe", "Total debe", 378, 117, 112)
            txtHaber = Caja("txtHaber", "Total haber", 494, 117, 112)
            For Each total In {txtDebe, txtHaber}
                total.ReadOnly = True
                total.TabStop = False
                total.TextAlign = HorizontalAlignment.Right
            Next

            rejilla = New DataGridView With {
                .Name = "rejilla", .Location = New Point(6, 144), .Size = New Size(600, 274), .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
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

        Private Shared Function Lectura(nombre As String, x As Integer, y As Integer, ancho As Integer) As TextBox
            Return New TextBox With {.Name = nombre, .ReadOnly = True, .TabStop = False, .Location = New Point(x, y), .Width = ancho}
        End Function

        Private Shared Function Lista(nombre As String, accesible As String, x As Integer, y As Integer, ancho As Integer) As ComboBox
            Return New ComboBox With {.Name = nombre, .AccessibleName = accesible, .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(x, y), .Width = ancho}
        End Function

        Private Shared Function Boton(texto As String, nombre As String, x As Integer, y As Integer, ancho As Integer, alto As Integer) As Button
            Return New Button With {.Text = texto, .Name = nombre, .Location = New Point(x, y), .Size = New Size(ancho, alto), .UseVisualStyleBackColor = True}
        End Function

    End Class

End Namespace
