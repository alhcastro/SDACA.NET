Imports CoopeNet.Ui

Partial Class FrmConexiones

    Private Const Izquierda As Integer = 24
    Private Const Ancho As Integer = 432

    Private WithEvents cmbPerfil As ComboBox
    Private WithEvents btnNueva As Button
    Private WithEvents btnBorrar As Button
    Private WithEvents txtNombre As TextBox
    Private WithEvents txtServidor As TextBox
    Private WithEvents txtPuerto As TextBox
    Private WithEvents txtUsuario As TextBox
    Private WithEvents txtClave As TextBox
    Private WithEvents cmbCatalogo As ComboBox
    Private WithEvents btnBuscar As Button
    Private WithEvents btnProbar As Button
    Private lblEstado As Label
    Private chkPredeterminada As CheckBox
    Private WithEvents btnGuardar As Button
    Private WithEvents btnCerrar As Button

    Private Sub InitializeComponent()
        cmbPerfil = New ComboBox() With {.Name = "cmbPerfil", .AccessibleName = "Conexión", .DropDownStyle = ComboBoxStyle.DropDownList}
        btnNueva = New Button() With {.Name = "btnNueva", .Text = "&Nueva", .Size = New Size(90, 28)}
        btnBorrar = New Button() With {.Name = "btnBorrar", .Text = "&Eliminar", .Size = New Size(90, 28)}
        txtNombre = New TextBox() With {.Name = "txtNombre", .AccessibleName = "Nombre", .MaxLength = 60}
        txtServidor = New TextBox() With {.Name = "txtServidor", .AccessibleName = "Servidor"}
        txtPuerto = New TextBox() With {.Name = "txtPuerto", .AccessibleName = "Puerto", .MaxLength = 5}
        txtUsuario = New TextBox() With {.Name = "txtUsuario", .AccessibleName = "Usuario"}
        txtClave = New TextBox() With {.Name = "txtClave", .AccessibleName = "Contraseña", .UseSystemPasswordChar = True}
        cmbCatalogo = New ComboBox() With {.Name = "cmbCatalogo", .AccessibleName = "Catálogo", .DropDownStyle = ComboBoxStyle.DropDown}
        btnBuscar = New Button() With {.Name = "btnBuscar", .Text = "&Buscar catálogos", .Size = New Size(140, 28)}
        btnProbar = New Button() With {.Name = "btnProbar", .Text = "&Probar conexión", .Size = New Size(140, 30)}
        lblEstado = New Label() With {
            .Name = "lblEstado", .AutoSize = False, .AutoEllipsis = True, .TextAlign = ContentAlignment.MiddleLeft, .ForeColor = Tema.TextoTenue
        }
        chkPredeterminada = New CheckBox() With {.Name = "chkPredeterminada", .Text = "Usar como conexión pre&determinada", .AutoSize = True}
        btnGuardar = New Button() With {.Name = "btnGuardar", .Text = "&Guardar", .Size = New Size(110, 34)}
        btnCerrar = New Button() With {.Name = "btnCerrar", .Text = "&Cerrar", .Size = New Size(110, 34)}
        SuspendLayout()

        Dim nota As New Label() With {
            .Text = "El usuario y la contraseña solo se usan para buscar y probar: no se guardan.",
            .AutoSize = False, .Location = New Point(Izquierda, 262), .Size = New Size(Ancho, 20), .ForeColor = Tema.TextoTenue,
            .Font = New Font(Tema.FuenteBase, 8.75F)
        }
        Dim linea As New Panel() With {.Location = New Point(0, 426), .Size = New Size(480, 1), .BackColor = Tema.Borde}

        Controls.AddRange(New Control() {
            Rotulo("Conexión", Izquierda, 16), Colocar(cmbPerfil, Izquierda, 36, 240),
            Colocar(btnNueva, 274, 34, 90), Colocar(btnBorrar, 366, 34, 90),
            Rotulo("Nombre", Izquierda, 78), Colocar(txtNombre, Izquierda, 98, Ancho),
            Rotulo("Servidor", Izquierda, 136), Colocar(txtServidor, Izquierda, 156, 326),
            Rotulo("Puerto", 362, 136), Colocar(txtPuerto, 362, 156, 94),
            Rotulo("Usuario", Izquierda, 194), Colocar(txtUsuario, Izquierda, 214, 210),
            Rotulo("Contraseña", 246, 194), Colocar(txtClave, 246, 214, 210),
            nota,
            Rotulo("Catálogo", Izquierda, 290), Colocar(cmbCatalogo, Izquierda, 310, 284),
            Colocar(btnBuscar, 316, 308, 140),
            Colocar(btnProbar, Izquierda, 346, 140), Colocar(lblEstado, 174, 346, 282),
            linea,
            Colocar(chkPredeterminada, Izquierda, 392, Ancho),
            Colocar(btnGuardar, 228, 440, 110), Colocar(btnCerrar, 346, 440, 110)
        })
        lblEstado.Height = 30

        AcceptButton = btnGuardar
        CancelButton = btnCerrar
        AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        AutoScaleMode = AutoScaleMode.Dpi
        BackColor = Tema.Superficie
        ClientSize = New Size(480, 490)
        Font = New Font(Tema.FuenteBase, 9.75F)
        ForeColor = Tema.Texto
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ShowIcon = False
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        Text = "Configuración del servidor"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
        Return New Label() With {.Text = texto, .AutoSize = True, .Location = New Point(x, y), .ForeColor = Tema.TextoTenue}
    End Function

    Private Shared Function Colocar(control As Control, x As Integer, y As Integer, ancho As Integer) As Control
        control.Location = New Point(x, y)
        control.Width = ancho
        Return control
    End Function

End Class
