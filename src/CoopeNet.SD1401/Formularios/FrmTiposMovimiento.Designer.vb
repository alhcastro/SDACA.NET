Imports CoopeNet.Ui

Namespace SD1401

    Partial Public Class FrmTiposMovimiento

        Private gbDatos As GroupBox
        Private txtCodigo As TextBox
        Private txtDescripcion As TextBox
        Private txtAbreviatura As TextBox
        Private cmbTipo As ComboBox
        Private WithEvents btnPrimero As Button
        Private WithEvents btnAnterior As Button
        Private WithEvents btnSiguiente As Button
        Private WithEvents btnUltimo As Button
        Private barra As BarraComandos

        Private Sub InitializeComponent()
            Dim ayuda As New ToolTip()
            txtCodigo = New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código", .ReadOnly = True, .TabStop = False, .Location = New Point(110, 26), .Width = 120}
            txtDescripcion = New TextBox With {.Name = "txtDescripcion", .AccessibleName = "Descripción", .MaxLength = 36, .Location = New Point(110, 58), .Width = 340}
            txtAbreviatura = New TextBox With {.Name = "txtAbreviatura", .AccessibleName = "Abreviatura", .MaxLength = 3, .Location = New Point(110, 90), .Width = 120}
            cmbTipo = New ComboBox With {.Name = "cmbTipo", .AccessibleName = "Tipo", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(110, 122), .Width = 120}
            cmbTipo.Items.AddRange(New Object() {"Resta", "Suma"})

            gbDatos = New GroupBox With {.Name = "gbDatos", .Location = New Point(10, 8), .Size = New Size(466, 160)}
            gbDatos.Controls.AddRange(New Control() {
                Etiqueta("Código:", 12, 29), txtCodigo, Etiqueta("Descripción:", 12, 61), txtDescripcion,
                Etiqueta("Abreviatura:", 12, 93), txtAbreviatura, Etiqueta("Tipo:", 12, 125), cmbTipo
            })

            btnPrimero = Navegacion("btnPrimero", "|<", "Primero", 10, ayuda)
            btnAnterior = Navegacion("btnAnterior", "<", "Anterior", 48, ayuda)
            btnSiguiente = Navegacion("btnSiguiente", ">", "Siguiente", 86, ayuda)
            btnUltimo = Navegacion("btnUltimo", ">|", "Último", 124, ayuda)
            barra = New BarraComandos With {.Name = "barra", .Location = New Point(166, 176), .Size = New Size(310, 32)}

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(486, 218)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Mantenimiento de Tipos de Movimientos"
            Controls.AddRange(New Control() {gbDatos, btnPrimero, btnAnterior, btnSiguiente, btnUltimo, barra})
            ResumeLayout(False)
        End Sub

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Shared Function Navegacion(nombre As String, texto As String, accesible As String, x As Integer, ayuda As ToolTip) As Button
            Dim boton As New Button With {.Name = nombre, .Text = texto, .AccessibleName = accesible, .Location = New Point(x, 176), .Size = New Size(36, 32), .UseVisualStyleBackColor = True}
            ayuda.SetToolTip(boton, accesible)
            Return boton
        End Function

    End Class

End Namespace
