Namespace SD1401

    Partial Public Class FrmCalcularCierres

        Private gbDatos As GroupBox
        Private WithEvents cmbBancos As ComboBox
        Private WithEvents cmbCuentas As ComboBox
        Private txtMoneda As TextBox
        Private dtpInicio As DateTimePicker
        Private barraProgreso As ProgressBar
        Private WithEvents btnCalcular As Button
        Private WithEvents btnSalir As Button

        Private Sub InitializeComponent()
            cmbBancos = Lista("cmbBancos", "Banco", 34)
            cmbCuentas = Lista("cmbCuentas", "Cuenta", 66)
            txtMoneda = New TextBox With {.Name = "txtMoneda", .AccessibleName = "Moneda", .ReadOnly = True, .TabStop = False, .Location = New Point(90, 98), .Width = 372}
            gbDatos = New GroupBox With {.Name = "gbDatos", .Text = "Información de la Cuenta", .Location = New Point(10, 8), .Size = New Size(476, 134)}
            gbDatos.Controls.AddRange(New Control() {
                Etiqueta("Banco", 12, 37), cmbBancos, Etiqueta("Cuenta #", 12, 69), cmbCuentas, Etiqueta("Moneda", 12, 101), txtMoneda
            })

            dtpInicio = New DateTimePicker With {.Name = "dtpInicio", .AccessibleName = "Fecha de inicio", .Format = DateTimePickerFormat.Short, .Location = New Point(122, 152), .Width = 150}
            barraProgreso = New ProgressBar With {.Name = "barraProgreso", .Style = ProgressBarStyle.Marquee, .Visible = False, .Location = New Point(10, 186), .Size = New Size(256, 24)}
            btnCalcular = New Button With {.Text = "&Calcular", .Name = "btnGuardar", .Location = New Point(276, 182), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}
            btnSalir = New Button With {.Text = "&Salir", .Name = "btnSalir", .Location = New Point(386, 182), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(496, 224)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Calcular Monto de Cierres"
            CancelButton = btnSalir
            Controls.AddRange(New Control() {gbDatos, Etiqueta("Fecha de Inicio", 22, 155), dtpInicio, barraProgreso, btnCalcular, btnSalir})
            ResumeLayout(False)
        End Sub

        Private Shared Function Lista(nombre As String, accesible As String, y As Integer) As ComboBox
            Return New ComboBox With {.Name = nombre, .AccessibleName = accesible, .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, y), .Width = 372}
        End Function

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

    End Class

End Namespace
