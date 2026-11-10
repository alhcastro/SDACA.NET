Imports CoopeNet.Ui

Partial Class FrmEntrada

    Private Const AnchoCampos As Integer = 360

    Private lblLicencia As Label
    Private WithEvents cmbPerfil As ComboBox
    Private WithEvents lnkConexion As LinkLabel
    Private WithEvents lnkAdministrar As LinkLabel
    Private pnlConexion As Panel
    Private WithEvents txtServidor As TextBox
    Private WithEvents txtPuerto As TextBox
    Private WithEvents txtCatalogo As TextBox
    Private WithEvents cmbUsuario As ComboBox
    Private WithEvents txtClave As TextBox
    Private WithEvents cmbLocal As ComboBox
    Private WithEvents btnEntrar As Button
    Private WithEvents btnSalir As Button

    Private Sub InitializeComponent()
        cmbPerfil = New ComboBox() With {.Name = "cmbPerfil", .AccessibleName = "Conexión", .DropDownStyle = ComboBoxStyle.DropDownList}
        txtServidor = New TextBox() With {.Name = "txtServidor", .AccessibleName = "Servidor"}
        txtPuerto = New TextBox() With {.Name = "txtPuerto", .AccessibleName = "Puerto"}
        txtCatalogo = New TextBox() With {.Name = "txtCatalogo", .AccessibleName = "Catálogo"}
        ' Se puede escribir otro usuario o elegir uno de los últimos que entraron con la conexión (CmbUsr).
        cmbUsuario = New ComboBox() With {
            .Name = "cmbUsuario", .AccessibleName = "Usuario", .DropDownStyle = ComboBoxStyle.DropDown,
            .AutoCompleteMode = AutoCompleteMode.SuggestAppend, .AutoCompleteSource = AutoCompleteSource.ListItems
        }
        txtClave = New TextBox() With {.Name = "txtClave", .AccessibleName = "Contraseña", .UseSystemPasswordChar = True}
        cmbLocal = New ComboBox() With {.Name = "cmbLocal", .AccessibleName = "Departamento", .DropDownStyle = ComboBoxStyle.DropDownList}
        btnEntrar = New Button() With {.Name = "btnEntrar", .Text = "&Entrar", .Size = New Size(258, 36), .Margin = New Padding(0, 0, 8, 0)}
        btnSalir = New Button() With {.Name = "btnSalir", .Text = "&Salir", .Size = New Size(94, 36), .Margin = Padding.Empty}
        lnkConexion = New LinkLabel() With {
            .Name = "lnkConexion", .AutoSize = True, .Text = "Mostrar datos de conexión", .LinkColor = Tema.Acento, .ActiveLinkColor = Tema.AcentoOscuro,
            .VisitedLinkColor = Tema.Acento, .LinkBehavior = LinkBehavior.HoverUnderline, .Anchor = AnchorStyles.Left, .Margin = Padding.Empty
        }
        lnkAdministrar = New LinkLabel() With {
            .Name = "lnkAdministrar", .AutoSize = True, .Text = "Administrar conexiones", .LinkColor = Tema.Acento, .ActiveLinkColor = Tema.AcentoOscuro,
            .VisitedLinkColor = Tema.Acento, .LinkBehavior = LinkBehavior.HoverUnderline, .Anchor = AnchorStyles.Right, .Margin = Padding.Empty
        }
        Dim enlaces As New TableLayoutPanel() With {.Size = New Size(AnchoCampos, 22), .ColumnCount = 2, .RowCount = 1, .Margin = New Padding(0, 0, 0, 12)}
        enlaces.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        enlaces.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        enlaces.Controls.Add(lnkConexion, 0, 0)
        enlaces.Controls.Add(lnkAdministrar, 1, 0)
        SuspendLayout()

        pnlConexion = New Panel() With {.Name = "pnlConexion", .Size = New Size(AnchoCampos, 116), .Margin = Padding.Empty, .Visible = False}
        pnlConexion.Controls.AddRange(New Control() {
            Rotulo("Servidor", 0, 0), Colocar(txtServidor, 0, 20, 254),
            Rotulo("Puerto", 266, 0), Colocar(txtPuerto, 266, 20, 94),
            Rotulo("Catálogo", 0, 58), Colocar(txtCatalogo, 0, 78, AnchoCampos)
        })

        Dim titulo As New Label() With {
            .Text = "Iniciar sesión", .AutoSize = True, .ForeColor = Tema.Texto, .Font = New Font(Tema.FuenteSemibold, 18.0F), .Margin = New Padding(0, 0, 0, 2)
        }
        Dim subtitulo As New Label() With {
            .Text = "Ingrese sus datos de acceso", .AutoSize = True, .ForeColor = Tema.TextoTenue, .Margin = New Padding(0, 0, 0, 20)
        }
        Dim botones As New FlowLayoutPanel() With {.Size = New Size(AnchoCampos, 36), .Margin = New Padding(0, 10, 0, 0), .WrapContents = False}
        botones.Controls.AddRange(New Control() {btnEntrar, btnSalir})

        Dim columna As New FlowLayoutPanel() With {
            .Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .Padding = New Padding(40, 32, 40, 24), .BackColor = Tema.Superficie
        }
        columna.Controls.AddRange(New Control() {
            titulo, subtitulo, Campo("Conexión", cmbPerfil, 0), enlaces, pnlConexion,
            Campo("Usuario", cmbUsuario, 6), Campo("Contraseña", txtClave, 6), Campo("Departamento", cmbLocal, 6), botones
        })

        Controls.Add(columna)
        Controls.Add(CrearMarca())

        AcceptButton = btnEntrar
        CancelButton = btnSalir
        AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        AutoScaleMode = AutoScaleMode.Dpi
        BackColor = Tema.Superficie
        ClientSize = New Size(720, 456)
        Font = New Font(Tema.FuenteBase, 9.75F)
        ForeColor = Tema.Texto
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ShowIcon = False
        StartPosition = FormStartPosition.CenterScreen
        Text = Tema.Producto & " - Inicio de sesión"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Private Function CrearMarca() As Panel
        ' El tamaño va antes que los hijos: sus anclajes se miden contra el del panel.
        Dim marca As New Panel() With {.Size = New Size(280, 456), .Dock = DockStyle.Left, .BackColor = Tema.Marino}
        Dim lblMarca As New Label() With {
            .Text = Tema.Marca, .AutoSize = True, .Location = New Point(26, 40), .ForeColor = Color.White, .Font = New Font(Tema.FuenteSemibold, 28.0F)
        }
        Dim lblProducto As New Label() With {
            .Text = "Sistema de gestión empresarial", .AutoSize = True, .Location = New Point(30, 92), .ForeColor = Tema.TextoSobreMarino
        }
        Dim franja As New Panel() With {.Location = New Point(32, 124), .Size = New Size(44, 3), .BackColor = Tema.Acento}
        lblLicencia = New Label() With {
            .Name = "lblLicencia", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(30, 380), .Size = New Size(226, 22),
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left, .ForeColor = Color.White, .Font = New Font(Tema.FuenteSemibold, 10.0F)
        }
        Dim lblProveedor As New Label() With {
            .Text = Tema.Desarrollador, .AutoSize = True, .Location = New Point(30, 404),
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left, .ForeColor = Tema.TextoSobreMarino, .Font = New Font(Tema.FuenteBase, 8.75F)
        }
        Dim logo As New LogoEmpresa() With {
            .Name = "logoEmpresa", .Location = New Point(30, 308), .Size = New Size(220, 58), .Padding = New Padding(14, 10, 14, 10),
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        }
        marca.Controls.AddRange(New Control() {lblMarca, lblProducto, franja, logo, lblLicencia, lblProveedor})
        Return marca
    End Function

    ''' <summary>Rótulo arriba y campo abajo, en un bloque del ancho de la columna.</summary>
    Private Shared Function Campo(texto As String, control As Control, margenInferior As Integer) As Panel
        Dim bloque As New Panel() With {.Size = New Size(AnchoCampos, 50), .Margin = New Padding(0, 0, 0, margenInferior)}
        bloque.Controls.AddRange(New Control() {Rotulo(texto, 0, 0), Colocar(control, 0, 20, AnchoCampos)})
        Return bloque
    End Function

    Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
        Return New Label() With {.Text = texto, .AutoSize = True, .Location = New Point(x, y), .ForeColor = Tema.TextoTenue}
    End Function

    Private Shared Function Colocar(control As Control, x As Integer, y As Integer, ancho As Integer) As Control
        control.Location = New Point(x, y)
        control.Width = ancho
        Return control
    End Function

End Class
