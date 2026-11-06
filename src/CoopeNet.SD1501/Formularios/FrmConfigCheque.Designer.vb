Imports CoopeNet.Core.Cheques

Namespace SD1501

    Partial Public Class FrmConfigCheque

        Private panelPaso1 As Panel
        Private panelPaso2 As Panel
        Private WithEvents btnAnterior As Button
        Private WithEvents btnSiguiente As Button
        Private WithEvents btnAplicar As Button
        Private WithEvents btnSalir As Button

        Private WithEvents cmbBancos As ComboBox
        Private WithEvents txtAlto As TextBox
        Private WithEvents txtAncho As TextBox
        Private cmbForma As ComboBox
        Private cmbPosicion As ComboBox
        Private chkDetalle As CheckBox
        Private chkContabilidad As CheckBox
        Private txtAltoDetalle As TextBox
        Private txtAltoContabilidad As TextBox

        Private WithEvents rbCheque As RadioButton
        Private WithEvents rbDetalle As RadioButton
        Private WithEvents rbContabilidad As RadioButton
        Private vista As Panel
        Private etiquetas As Label()
        Private opciones As RadioButton()
        Private gbImprime As GroupBox
        Private WithEvents chkImprime As CheckBox
        Private WithEvents txtArriba As TextBox
        Private WithEvents txtIzquierda As TextBox
        Private WithEvents txtAltoCampo As TextBox
        Private WithEvents txtAnchoCampo As TextBox
        Private txtFuente As TextBox
        Private txtTamano As TextBox
        Private chkNegrita As CheckBox
        Private chkCursiva As CheckBox
        Private WithEvents cmbAlineacion As ComboBox
        Private WithEvents cmbFormato As ComboBox
        Private WithEvents btnFuente As Button
        Private WithEvents btnPredeterminados As Button

        Private Sub InitializeComponent()
            btnAnterior = Boton("&Anterior", "btnAnterior", 4)
            btnSiguiente = Boton("Si&guiente", "btnSiguiente", 180)
            btnAplicar = Boton("A&plicar", "btnAplicar", 356)
            btnSalir = Boton("&Salir", "btnSalir", 532)

            panelPaso1 = New Panel With {.Name = "panelPaso1", .Location = New Point(4, 4), .Size = New Size(681, 488)}
            panelPaso2 = New Panel With {.Name = "panelPaso2", .Location = New Point(4, 4), .Size = New Size(681, 488)}
            CrearPaso1()
            CrearPaso2()

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(688, 545)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Configuración de la impresión del cheque"
            CancelButton = btnSalir
            Controls.AddRange(New Control() {panelPaso1, panelPaso2, btnAnterior, btnSiguiente, btnAplicar, btnSalir})
            ResumeLayout(False)
        End Sub

        Private Sub CrearPaso1()
            cmbBancos = Lista("cmbBancos", "Banco", 170, 12, 499)
            txtAlto = Medida("txtAlto", "Alto del Cheque (cm)", 170, 46)
            txtAncho = Medida("txtAncho", "Ancho del Cheque (cm)", 561, 46)
            cmbForma = Lista("cmbForma", "Forma de Impresión", 170, 80, 170)
            cmbForma.Items.AddRange(New Object() {"Vertical", "Horizontal", "Centrada", "Derecha", "Izquierda"})
            cmbPosicion = Lista("cmbPosicion", "Posición en la hoja", 498, 80, 171)
            cmbPosicion.Items.AddRange(New Object() {"Abajo", "Arriba"})

            chkDetalle = New CheckBox With {.Name = "chkDetalle", .Text = "Imprimir Detalle del Cheque", .AutoSize = True, .Location = New Point(11, 124)}
            txtAltoDetalle = Medida("txtAltoDetalle", "Alto (cm)", 124, 150)
            chkContabilidad = New CheckBox With {.Name = "chkContabilidad", .Text = "Imprimir Detalle Contable del Cheque", .AutoSize = True, .Location = New Point(353, 124)}
            txtAltoContabilidad = Medida("txtAltoContabilidad", "Alto (cm)", 466, 150)

            panelPaso1.Controls.AddRange(New Control() {
                New GroupBox With {.Text = "Paso #1 - Información General del Cheque", .Dock = DockStyle.Top, .Height = 2, .FlatStyle = FlatStyle.Flat},
                Etiqueta("Banco", 11, 15), cmbBancos,
                Etiqueta("Alto del Cheque (cm)", 11, 49), txtAlto,
                Etiqueta("Ancho del Cheque (cm)", 353, 49), txtAncho,
                Etiqueta("Forma de Impresión", 11, 83), cmbForma,
                Etiqueta("Posición en la hoja", 353, 83), cmbPosicion,
                chkDetalle, Etiqueta("Alto (cm)", 49, 153), txtAltoDetalle,
                chkContabilidad, Etiqueta("Alto (cm)", 391, 153), txtAltoContabilidad
            })
        End Sub

        Private Sub CrearPaso2()
            rbCheque = New RadioButton With {.Name = "rbCheque", .Text = "Cheque", .AutoSize = True, .Location = New Point(396, 0), .Checked = True}
            rbDetalle = New RadioButton With {.Name = "rbDetalle", .Text = "Detalle", .AutoSize = True, .Location = New Point(487, 0)}
            rbContabilidad = New RadioButton With {.Name = "rbContabilidad", .Text = "Contabilidad", .AutoSize = True, .Location = New Point(578, 0)}
            Dim tipos As New Panel With {.Location = New Point(0, 2), .Size = New Size(681, 20)}
            tipos.Controls.AddRange(New Control() {
                New Label With {.Text = "Paso #2 - Configurar el Encabezado del Cheque", .AutoSize = True, .Location = New Point(0, 2)},
                rbCheque, rbDetalle, rbContabilidad})

            ' 10005 x 4125 twips, a 15 twips por píxel.
            vista = New Panel With {.Name = "vista", .Location = New Point(7, 24), .Size = New Size(667, 275), .BackColor = Color.White, .BorderStyle = BorderStyle.None}

            Dim nombres = {"Fecha", "Nombre", "Monto", "Monto en Letras", "Cod. Seguridad", "Fecha", "# Cheque", "Detalle", "C. Contable", "Descripcion", "Debe", "Haber"}
            Dim inicio = {0, 0, 0, 0, 0, 5, 5, 5, 8, 8, 8, 8}
            Dim campos As New GroupBox With {.Text = "Campos", .Location = New Point(7, 302), .Size = New Size(135, 184)}
            etiquetas = New Label(11) {}
            opciones = New RadioButton(11) {}
            For i = 0 To 11
                etiquetas(i) = New Label With {.Name = "etiqueta" & i, .Tag = i, .AutoSize = False}
                AddHandler etiquetas(i).Click, AddressOf Etiqueta_Click
                opciones(i) = New RadioButton With {
                    .Name = "opcion" & i, .Tag = i, .Text = nombres(i), .AutoSize = True,
                    .Location = New Point(12, 22 + (i - inicio(i)) * 32), .Visible = False
                }
                AddHandler opciones(i).CheckedChanged, AddressOf Opcion_CheckedChanged
            Next
            vista.Controls.AddRange(etiquetas)
            campos.Controls.AddRange(opciones)

            gbImprime = New GroupBox With {.Name = "gbImprime", .Location = New Point(145, 302), .Size = New Size(529, 184)}
            chkImprime = New CheckBox With {.Name = "chkImprime", .Text = "Imprimir", .AutoSize = True, .Location = New Point(8, 15)}
            btnPredeterminados = New Button With {.Name = "btnPredeterminados", .Text = "Valores Predeterminados", .Location = New Point(354, 13), .Size = New Size(168, 23), .UseVisualStyleBackColor = True}

            Dim posicion As New GroupBox With {.Text = "Posición", .Location = New Point(4, 38), .Size = New Size(225, 142)}
            txtArriba = Medida("txtArriba", "Top", 80, 22)
            txtIzquierda = Medida("txtIzquierda", "Izquierda", 80, 52)
            txtAltoCampo = Medida("txtAltoCampo", "Alto", 80, 82)
            txtAnchoCampo = Medida("txtAnchoCampo", "Ancho", 80, 112)
            posicion.Controls.AddRange(New Control() {
                Etiqueta("Top", 16, 25), txtArriba, Etiqueta("cm", 186, 25),
                Etiqueta("Izquierda", 16, 55), txtIzquierda, Etiqueta("cm", 186, 55),
                Etiqueta("Alto", 16, 85), txtAltoCampo, Etiqueta("cm", 186, 85),
                Etiqueta("Ancho", 16, 115), txtAnchoCampo, Etiqueta("cm", 186, 115)
            })

            Dim estilo As New GroupBox With {.Text = "Estilo", .Location = New Point(231, 38), .Size = New Size(291, 142)}
            btnFuente = New Button With {.Name = "btnFuente", .Text = "&Fuente", .Location = New Point(8, 22), .Size = New Size(62, 23), .UseVisualStyleBackColor = True}
            txtFuente = New TextBox With {.Name = "txtFuente", .AccessibleName = "Fuente", .ReadOnly = True, .Location = New Point(80, 22), .Width = 202}
            txtTamano = New TextBox With {.Name = "txtTamano", .AccessibleName = "Tamaño", .ReadOnly = True, .TextAlign = HorizontalAlignment.Right, .Location = New Point(80, 52), .Width = 47}
            chkNegrita = New CheckBox With {.Name = "chkNegrita", .Text = "Negrita", .AutoSize = True, .AutoCheck = False, .Location = New Point(140, 54)}
            chkCursiva = New CheckBox With {.Name = "chkCursiva", .Text = "Cursiva", .AutoSize = True, .AutoCheck = False, .Location = New Point(217, 54)}
            cmbAlineacion = Lista("cmbAlineacion", "Alineación", 80, 82, 202)
            cmbAlineacion.Items.AddRange(New Object() {"Izquierda", "Derecha", "Centrada"})
            cmbFormato = Lista("cmbFormato", "Formato", 80, 112, 202)
            estilo.Controls.AddRange(New Control() {
                btnFuente, txtFuente, Etiqueta("Tamaño", 8, 55), txtTamano, chkNegrita, chkCursiva,
                Etiqueta("Alineación", 8, 85), cmbAlineacion, Etiqueta("Formato", 8, 115), cmbFormato
            })

            gbImprime.Controls.AddRange(New Control() {chkImprime, btnPredeterminados, posicion, estilo})
            panelPaso2.Controls.AddRange(New Control() {tipos, vista, campos, gbImprime})
        End Sub

        Private Function Boton(texto As String, nombre As String, izquierda As Integer) As Button
            Return New Button With {.Text = texto, .Name = nombre, .Location = New Point(izquierda, 498), .Size = New Size(153, 41), .UseVisualStyleBackColor = True}
        End Function

        Private Shared Function Etiqueta(texto As String, izquierda As Integer, arriba As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(izquierda, arriba)}
        End Function

        Private Shared Function Medida(nombre As String, etiquetaAccesible As String, izquierda As Integer, arriba As Integer) As TextBox
            Return New TextBox With {
                .Name = nombre, .AccessibleName = etiquetaAccesible, .TextAlign = HorizontalAlignment.Right,
                .Location = New Point(izquierda, arriba), .Width = 100
            }
        End Function

        Private Shared Function Lista(nombre As String, etiquetaAccesible As String, izquierda As Integer, arriba As Integer, ancho As Integer) As ComboBox
            Return New ComboBox With {
                .Name = nombre, .AccessibleName = etiquetaAccesible, .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(izquierda, arriba), .Width = ancho
            }
        End Function

    End Class

End Namespace
