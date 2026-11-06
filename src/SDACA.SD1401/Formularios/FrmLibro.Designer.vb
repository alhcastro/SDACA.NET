Imports SDACA.Ui

Namespace SD1401

    Partial Public Class FrmLibro

        Private gbCuenta As GroupBox
        Private WithEvents cmbBancos As ComboBox
        Private WithEvents cmbCuentas As ComboBox
        Private txtMoneda As TextBox
        Private txtSaldo As TextBox
        Private lblInfo As Label
        Private pestanas As Pestanas
        Private gbMovimiento As Panel
        Private cmbTipo As ComboBox
        Private WithEvents dtpFecha As DateTimePicker
        Private txtDocumento As TextBox
        Private txtMonto As TextBox
        Private txtDetalle As TextBox
        Private gbConciliacion As Panel
        Private WithEvents chkConciliado As CheckBox
        Private dtpBanco As DateTimePicker
        Private txtDetalleBanco As TextBox
        Private dtpBuscar As DateTimePicker
        Private barra As BarraComandos

        Private Sub InitializeComponent()
            cmbBancos = Lista("cmbBancos", "Banco", 90, 34, 560)
            cmbCuentas = Lista("cmbCuentas", "Cuenta", 90, 66, 560)
            txtMoneda = New TextBox With {.Name = "txtMoneda", .AccessibleName = "Moneda", .ReadOnly = True, .TabStop = False, .Location = New Point(90, 98), .Width = 220}
            txtSaldo = New TextBox With {
                .Name = "txtSaldo", .AccessibleName = "Saldo actual", .ReadOnly = True, .TabStop = False, .TextAlign = HorizontalAlignment.Right,
                .Location = New Point(430, 98), .Width = 220
            }
            gbCuenta = New GroupBox With {.Name = "gbCuenta", .Text = "Información de la Cuenta", .Location = New Point(10, 8), .Size = New Size(664, 134)}
            gbCuenta.Controls.AddRange(New Control() {
                Etiqueta("Banco", 12, 37), cmbBancos, Etiqueta("Cuenta #", 12, 69), cmbCuentas,
                Etiqueta("Moneda", 12, 101), txtMoneda, Etiqueta("Saldo Actual", 340, 101), txtSaldo
            })

            cmbTipo = Lista("cmbTipo", "Tipo", 150, 14, 300)
            dtpFecha = New DateTimePicker With {.Name = "dtpFecha", .AccessibleName = "Fecha del movimiento", .Format = DateTimePickerFormat.Short, .Location = New Point(150, 46), .Width = 150}
            txtDocumento = New TextBox With {.Name = "txtDocumento", .AccessibleName = "Número de documento", .MaxLength = 16, .Location = New Point(150, 78), .Width = 200}
            txtMonto = New TextBox With {.Name = "txtMonto", .AccessibleName = "Monto", .TextAlign = HorizontalAlignment.Right, .Location = New Point(150, 110), .Width = 200}
            txtDetalle = New TextBox With {
                .Name = "txtDetalle", .AccessibleName = "Detalle", .Multiline = True, .ScrollBars = ScrollBars.Vertical, .AcceptsReturn = True,
                .Location = New Point(150, 142), .Size = New Size(490, 74)
            }
            gbMovimiento = New Panel With {.Name = "gbMovimiento", .Dock = DockStyle.Fill}
            gbMovimiento.Controls.AddRange(New Control() {
                Etiqueta("Tipo", 12, 17), cmbTipo, Etiqueta("Fecha del Movimiento", 12, 49), dtpFecha, Etiqueta("# Doc. Referencia", 12, 81), txtDocumento,
                Etiqueta("Monto", 12, 113), txtMonto, Etiqueta("Detalle", 12, 145), txtDetalle
            })

            chkConciliado = New CheckBox With {.Name = "chkConciliado", .Text = "Conciliado", .AutoSize = True, .Location = New Point(12, 16)}
            dtpBanco = New DateTimePicker With {
                .Name = "dtpBanco", .AccessibleName = "Fecha de registro del banco", .Format = DateTimePickerFormat.Short, .Enabled = False, .Location = New Point(200, 46), .Width = 150
            }
            txtDetalleBanco = New TextBox With {
                .Name = "txtDetalleBanco", .AccessibleName = "Detalle del banco", .MaxLength = 128, .Enabled = False, .Location = New Point(200, 78), .Width = 440
            }
            gbConciliacion = New Panel With {.Name = "gbConciliacion", .Dock = DockStyle.Fill}
            gbConciliacion.Controls.AddRange(New Control() {
                chkConciliado, Etiqueta("Fecha de Registro del Banco", 12, 49), dtpBanco, Etiqueta("Detalle del Banco", 12, 81), txtDetalleBanco
            })

            Dim paginaMovimiento As New TabPage With {.Text = "Datos del Movimiento", .UseVisualStyleBackColor = True}
            paginaMovimiento.Controls.Add(gbMovimiento)
            Dim paginaConciliacion As New TabPage With {.Text = "Conciliación", .UseVisualStyleBackColor = True}
            paginaConciliacion.Controls.Add(gbConciliacion)
            pestanas = New Pestanas With {.Name = "pestanas", .Location = New Point(10, 150), .Size = New Size(664, 262)}
            pestanas.TabPages.AddRange(New TabPage() {paginaMovimiento, paginaConciliacion})

            lblInfo = New Label With {.Name = "lblInfo", .AutoSize = True, .ForeColor = Tema.Peligro, .Location = New Point(12, 418)}
            dtpBuscar = New DateTimePicker With {.Name = "dtpBuscar", .AccessibleName = "Buscar desde", .Format = DateTimePickerFormat.Short, .Location = New Point(96, 442), .Width = 120}
            barra = New BarraComandos With {.Name = "barra", .Modo = ModoBarra.Todos, .Location = New Point(234, 438), .Size = New Size(440, 32)}

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(684, 480)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Libro de Movimientos Bancarios"
            Controls.AddRange(New Control() {gbCuenta, pestanas, lblInfo, Etiqueta("Buscar desde", 12, 445), dtpBuscar, barra})
            ResumeLayout(False)
        End Sub

        Private Shared Function Lista(nombre As String, accesible As String, x As Integer, y As Integer, ancho As Integer) As ComboBox
            Return New ComboBox With {.Name = nombre, .AccessibleName = accesible, .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(x, y), .Width = ancho}
        End Function

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

    End Class

End Namespace
