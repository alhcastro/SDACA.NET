Namespace SD1401

    Partial Public Class FrmCierreMes

        Private WithEvents cmbBancos As ComboBox
        Private WithEvents cmbCuentas As ComboBox
        Private txtMoneda As TextBox
        Private WithEvents dtpMes As DateTimePicker
        Private lblEstado As Label
        Private WithEvents btnCerrarMes As Button
        Private WithEvents btnAbrirMes As Button
        Private WithEvents btnSalir As Button

        Private Sub InitializeComponent()
            cmbBancos = Lista("cmbBancos", "Banco", 34)
            cmbCuentas = Lista("cmbCuentas", "Cuenta", 66)
            txtMoneda = New TextBox With {.Name = "txtMoneda", .AccessibleName = "Moneda", .ReadOnly = True, .TabStop = False, .Location = New Point(90, 98), .Width = 372}
            dtpMes = New DateTimePicker With {
                .Name = "dtpMes", .AccessibleName = "Mes", .Format = DateTimePickerFormat.Custom, .CustomFormat = "MMMM yyyy", .ShowUpDown = True,
                .Location = New Point(90, 130), .Width = 170
            }
            lblEstado = New Label With {.Name = "lblEstado", .AutoSize = True, .Location = New Point(272, 133)}

            Dim datos As New GroupBox With {.Name = "gbDatos", .Text = "Información de la Cuenta", .Location = New Point(10, 8), .Size = New Size(476, 166)}
            datos.Controls.AddRange(New Control() {
                Etiqueta("Banco", 37), cmbBancos, Etiqueta("Cuenta #", 69), cmbCuentas, Etiqueta("Moneda", 101), txtMoneda,
                Etiqueta("Mes", 133), dtpMes, lblEstado
            })

            btnAbrirMes = Boton("&Abrir Mes", "btnAbrirMes", 166)
            btnCerrarMes = Boton("&Cerrar Mes", "btnCerrarMes", 276)
            btnSalir = Boton("&Salir", "btnSalir", 386)

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(496, 224)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Cerrar Mes"
            CancelButton = btnSalir
            Controls.AddRange(New Control() {datos, btnAbrirMes, btnCerrarMes, btnSalir})
            ResumeLayout(False)
        End Sub

        Private Shared Function Lista(nombre As String, accesible As String, y As Integer) As ComboBox
            Return New ComboBox With {.Name = nombre, .AccessibleName = accesible, .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, y), .Width = 372}
        End Function

        Private Shared Function Etiqueta(texto As String, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(12, y)}
        End Function

        Private Shared Function Boton(texto As String, nombre As String, x As Integer) As Button
            Return New Button With {.Text = texto, .Name = nombre, .Location = New Point(x, 182), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}
        End Function

    End Class

End Namespace
