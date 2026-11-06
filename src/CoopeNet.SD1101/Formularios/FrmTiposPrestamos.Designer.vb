Imports System.Drawing
Imports CoopeNet.Ui

Namespace SD1101

    Partial Class FrmTiposPrestamos

        Private WithEvents cmdAnterior As Button
        Private WithEvents cmdProximo As Button
        Private WithEvents txtDescripcion As TextBox
        Private chkAprobacion As CheckBox
        Private fraDetalle As GroupBox
        Private lblCodigo As Label
        Private lblDescri As Label
        Private lblCod As Label
        Private barra As BarraComandos

        ' Medidas del .frm original, de twips a píxeles (15 twips por píxel).
        Private Sub InitializeComponent()
            Dim arial As New Font("Arial", 9.75F)
            Dim webdings As New Font("Webdings", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 2)
            Dim ayuda As New ToolTip()

            fraDetalle = New GroupBox()
            lblCod = New Label()
            lblCodigo = New Label()
            lblDescri = New Label()
            txtDescripcion = New TextBox()
            chkAprobacion = New CheckBox()
            cmdAnterior = New Button()
            cmdProximo = New Button()
            barra = New BarraComandos()
            SuspendLayout()

            lblCod.AutoSize = True
            lblCod.Font = arial
            lblCod.Location = New Point(8, 26)
            lblCod.Text = "Código:"

            lblCodigo.AutoSize = True
            lblCodigo.Font = arial
            lblCodigo.ForeColor = Tema.Acento
            lblCodigo.Location = New Point(91, 26)

            lblDescri.AutoSize = True
            lblDescri.Font = arial
            lblDescri.Location = New Point(8, 62)
            lblDescri.Text = "Descripción:"

            txtDescripcion.Font = arial
            txtDescripcion.Location = New Point(91, 58)
            txtDescripcion.Size = New Size(366, 25)
            txtDescripcion.TabIndex = 1

            chkAprobacion.CheckAlign = ContentAlignment.MiddleRight
            chkAprobacion.Font = arial
            chkAprobacion.Location = New Point(287, 98)
            chkAprobacion.Size = New Size(170, 30)
            chkAprobacion.TabIndex = 2
            chkAprobacion.Text = "Requiere Aprobación"
            chkAprobacion.UseVisualStyleBackColor = True

            fraDetalle.Location = New Point(7, 2)
            fraDetalle.Size = New Size(467, 140)
            fraDetalle.TabIndex = 0
            fraDetalle.Controls.AddRange(New Control() {lblCod, lblCodigo, lblDescri, txtDescripcion, chkAprobacion})

            cmdAnterior.Font = webdings
            cmdAnterior.Location = New Point(7, 147)
            cmdAnterior.Size = New Size(63, 33)
            cmdAnterior.TabIndex = 1
            cmdAnterior.Text = "7"
            cmdAnterior.UseVisualStyleBackColor = True
            ayuda.SetToolTip(cmdAnterior, "Anterior")

            cmdProximo.Font = webdings
            cmdProximo.Location = New Point(70, 147)
            cmdProximo.Size = New Size(63, 33)
            cmdProximo.TabIndex = 2
            cmdProximo.Text = "8"
            cmdProximo.UseVisualStyleBackColor = True
            ayuda.SetToolTip(cmdProximo, "Siguiente")

            barra.Location = New Point(134, 147)
            barra.Size = New Size(340, 33)
            barra.TabIndex = 3

            AutoScaleMode = AutoScaleMode.None
            ClientSize = New Size(482, 186)
            Controls.AddRange(New Control() {fraDetalle, cmdAnterior, cmdProximo, barra})
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterScreen
            Text = "Tipos de Préstamos"
            ResumeLayout(False)
        End Sub

    End Class

End Namespace
