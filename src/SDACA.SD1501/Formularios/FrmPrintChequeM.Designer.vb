Namespace SD1501

    Partial Public Class FrmPrintChequeM

        Private gbDatos As GroupBox
        Private WithEvents cmbBancos As ComboBox
        Private cmbCuentas As ComboBox
        Private WithEvents txtDesde As TextBox
        Private WithEvents txtHasta As TextBox
        Private cmbSalida As ComboBox
        Private WithEvents btnImprimir As Button
        Private WithEvents btnPausa As Button
        Private WithEvents btnDetener As Button
        Private WithEvents btnSalir As Button
        Private barra As ProgressBar
        Private lblEstado As Label
        Private txtDetalle As TextBox

        Private Sub InitializeComponent()
            gbDatos = New GroupBox With {.Name = "gbDatos", .Location = New Point(6, 2), .Size = New Size(628, 108)}
            cmbBancos = New ComboBox With {.Name = "cmbBancos", .AccessibleName = "Banco", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(120, 14), .Width = 498}
            cmbCuentas = New ComboBox With {.Name = "cmbCuentas", .AccessibleName = "Cuenta #", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(120, 44), .Width = 498}
            txtDesde = New TextBox With {.Name = "txtDesde", .AccessibleName = "Imprimir del Cheque #", .Location = New Point(150, 74), .Width = 100}
            txtHasta = New TextBox With {.Name = "txtHasta", .AccessibleName = "al #", .Location = New Point(288, 74), .Width = 100}
            cmbSalida = New ComboBox With {.Name = "cmbSalida", .AccessibleName = "Salida", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(468, 74), .Width = 150}
            cmbSalida.Items.AddRange(New Object() {"Impresora configurada", "PDF"})
            gbDatos.Controls.AddRange(New Control() {
                Etiqueta("Banco", 12, 17), cmbBancos, Etiqueta("Cuenta #", 12, 47), cmbCuentas,
                Etiqueta("Imprimir del Cheque #", 12, 77), txtDesde, Etiqueta("al #", 258, 77), txtHasta,
                Etiqueta("Salida", 426, 77), cmbSalida
            })

            btnImprimir = Boton("&Imprimir", "btnImprimir", 6)
            btnPausa = Boton("&Pausa", "btnPausa", 112)
            btnPausa.Enabled = False
            btnDetener = Boton("&Detener", "btnDetener", 218)
            btnDetener.Enabled = False
            btnSalir = Boton("&Salir", "btnSalir", 534)

            barra = New ProgressBar With {.Name = "barra", .Location = New Point(6, 150), .Size = New Size(628, 20)}
            lblEstado = New Label With {.Name = "lblEstado", .Text = "Esperando...", .AutoSize = True, .Location = New Point(330, 122)}
            txtDetalle = New TextBox With {
                .Name = "txtDetalle", .AccessibleName = "Cheque en curso", .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Both, .WordWrap = False,
                .Font = New Font("Consolas", 9), .Location = New Point(6, 176), .Size = New Size(628, 300),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            }

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(640, 482)
            MinimumSize = New Size(656, 400)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Imprimir Cheques"
            Controls.AddRange(New Control() {gbDatos, btnImprimir, btnPausa, btnDetener, lblEstado, btnSalir, barra, txtDetalle})
            ResumeLayout(False)
            PerformLayout()
        End Sub

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Shared Function Boton(texto As String, nombre As String, x As Integer) As Button
            Return New Button With {.Text = texto, .Name = nombre, .Location = New Point(x, 116), .Size = New Size(100, 28), .UseVisualStyleBackColor = True}
        End Function

    End Class

End Namespace
