Imports CoopeNet.Ui

Partial Class FrmMenu

    Private lateral As Panel
    Private lblEmpresa As Label
    Private listaModulos As FlowLayoutPanel
    Private lblUsuario As Label
    Private lblLocal As Label
    Private lblEstacion As Label
    Private cabecera As Panel
    Private lblTitulo As Label
    Private lblResumen As Label
    Private WithEvents txtBuscar As TextBox
    Private WithEvents btnTema As Button
    Private WithEvents contenido As FlowLayoutPanel
    Private lblVacio As Label

    Private Sub InitializeComponent()
        SuspendLayout()

        CrearLateral()
        CrearCabecera()

        contenido = New FlowLayoutPanel With {
            .Name = "contenido", .Dock = DockStyle.Fill, .AutoScroll = True, .BackColor = Tema.Fondo, .Padding = New Padding(28, 6, 0, 20)
        }
        lblVacio = New Label With {
            .Name = "lblVacio", .Dock = DockStyle.Top, .Height = 80, .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = Tema.TextoTenue, .BackColor = Tema.Fondo, .Visible = False
        }

        Controls.Add(contenido)
        Controls.Add(lblVacio)
        Controls.Add(cabecera)
        Controls.Add(lateral)

        AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        AutoScaleMode = AutoScaleMode.Dpi
        BackColor = Tema.Fondo
        ForeColor = Tema.Texto
        ClientSize = New Size(1120, 700)
        MinimumSize = New Size(860, 560)
        Font = New Font(Tema.FuenteBase, 9.75F)
        ShowIcon = False
        StartPosition = FormStartPosition.CenterScreen
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Private Sub CrearLateral()
        lateral = New Panel With {.Name = "lateral", .Dock = DockStyle.Left, .Width = 260, .BackColor = Tema.Marino}

        Dim marca As New Panel With {.Dock = DockStyle.Top, .Height = 112, .BackColor = Tema.Marino}
        Dim lblMarca As New Label With {
            .Text = Tema.Marca, .AutoSize = True, .Location = New Point(17, 20), .ForeColor = Color.White, .Font = New Font(Tema.FuenteSemibold, 20.0F)
        }
        Dim lblProducto As New Label With {
            .Text = "ERP", .AutoSize = True, .Location = New Point(152, 34), .ForeColor = Tema.TextoSobreMarino, .Font = New Font(Tema.FuenteBase, 10.0F)
        }
        lblEmpresa = New Label With {
            .Name = "lblEmpresa", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(20, 64), .Size = New Size(228, 18),
            .ForeColor = Tema.TextoSobreMarino, .Font = New Font(Tema.FuenteBase, 8.75F)
        }
        Dim lblModulos As New Label With {
            .Text = "MÓDULOS", .AutoSize = True, .Location = New Point(20, 92), .ForeColor = Tema.TextoSobreMarino, .Font = New Font(Tema.FuenteSemibold, 8.0F)
        }
        marca.Controls.AddRange(New Control() {lblMarca, lblProducto, lblEmpresa, lblModulos})

        listaModulos = New FlowLayoutPanel With {
            .Name = "listaModulos", .Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = False,
            .AutoScroll = True, .BackColor = Tema.Marino
        }

        Dim pie As New Panel With {.Dock = DockStyle.Bottom, .Height = 96, .BackColor = Tema.Marino}
        Dim linea As New Panel With {.Dock = DockStyle.Top, .Height = 1, .BackColor = Tema.MarinoClaro}
        lblUsuario = EtiquetaPie("lblUsuario", 14, Color.White, New Font(Tema.FuenteSemibold, 9.5F))
        lblLocal = EtiquetaPie("lblLocal", 38, Tema.TextoSobreMarino, New Font(Tema.FuenteBase, 8.75F))
        lblEstacion = EtiquetaPie("lblEstacion", 60, Tema.TextoSobreMarino, New Font(Tema.FuenteBase, 8.75F))
        pie.Controls.AddRange(New Control() {linea, lblUsuario, lblLocal, lblEstacion})

        lateral.Controls.Add(listaModulos)
        lateral.Controls.Add(pie)
        lateral.Controls.Add(marca)
        lateral.Controls.Add(New LogoEmpresa With {.Name = "logoEmpresa", .Dock = DockStyle.Top, .Height = 60, .Padding = New Padding(20, 12, 20, 12)})
    End Sub

    Private Shared Function EtiquetaPie(nombre As String, y As Integer, color As Color, fuente As Font) As Label
        Return New Label With {
            .Name = nombre, .AutoSize = False, .AutoEllipsis = True, .Location = New Point(20, y), .Size = New Size(228, 20), .ForeColor = color, .Font = fuente
        }
    End Function

    Private Sub CrearCabecera()
        ' El tamaño va antes que los hijos: sus anclajes se miden contra el del panel.
        cabecera = New Panel With {.Name = "cabecera", .Size = New Size(860, 92), .Dock = DockStyle.Top, .BackColor = Tema.Fondo}
        lblTitulo = New Label With {
            .Name = "lblTitulo", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(26, 18), .Size = New Size(498, 36),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right, .ForeColor = Tema.Texto, .Font = New Font(Tema.FuenteSemibold, 17.0F)
        }
        lblResumen = New Label With {
            .Name = "lblResumen", .AutoSize = True, .Location = New Point(28, 56), .ForeColor = Tema.TextoTenue
        }
        txtBuscar = New TextBox With {
            .Name = "txtBuscar", .AccessibleName = "Buscar opción", .PlaceholderText = "Buscar opción...",
            .Location = New Point(574, 30), .Size = New Size(260, 26), .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
            .BackColor = Tema.Superficie
        }
        btnTema = New Button With {
            .Name = "btnTema", .AccessibleName = "Modo de color", .Location = New Point(536, 29), .Size = New Size(30, 28),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right, .Font = New Font(Tema.FuenteIconos, 11.0F), .TabStop = True
        }
        ' Los botones primero: quedan encima si un texto largo llega a alcanzarlos.
        cabecera.Controls.AddRange(New Control() {btnTema, txtBuscar, lblTitulo, lblResumen})
    End Sub

End Class
