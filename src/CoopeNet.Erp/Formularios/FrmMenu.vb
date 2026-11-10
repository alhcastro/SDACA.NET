Imports System.Globalization
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Core.Licencias
Imports CoopeNet.Core.Seguridad
Imports CoopeNet.Ui

''' <summary>
''' Menú principal. Muestra los módulos de la licencia a los que el usuario tiene acceso y, dentro
''' de cada uno, solo las opciones que su privilegio permite, como CargarModulos y Cargaropciones.
''' </summary>
Partial Public Class FrmMenu
    Inherits Form

    Private Const SeccionSinGrupo As String = "Operaciones"
    Private Const AnchoMinimoTarjeta As Integer = 250
    Private Const SeparacionTarjetas As Integer = 12
    Private Const AltoTarjeta As Integer = 68

    ''' <summary>Quien venía del árbol hace doble clic: el segundo clic no debe caer en la pantalla recién abierta.</summary>
    Private Const EsperaDobleClic As Integer = 200

    Private NotInheritable Class Seccion
        Public Property Titulo As String = ""
        Public ReadOnly Property Tarjetas As New List(Of (Opcion As OpcionMenu, Modulo As IModulo))
    End Class

    Private ReadOnly _ayuda As New ToolTip()
    Private ReadOnly _sesion As Sesion
    Private ReadOnly _modulos As List(Of IModulo)
    Private ReadOnly _preferencias As Preferencias
    Private _actual As IModulo
    Private _abriendo As Boolean

    Public Sub New(sesion As Sesion, modulos As IReadOnlyList(Of IModulo), licencia As ILicencia, preferencias As Preferencias)
        _sesion = sesion
        _preferencias = preferencias
        InitializeComponent()
        If preferencias.MenuMaximizado Then WindowState = FormWindowState.Maximized
        Tema.ColorearVentana(Me)
        PrepararModoColor()
        Text = $"{Tema.Producto} - {licencia.Registro.NombreCorto}"
        lblEmpresa.Text = licencia.Registro.NombreCorto
        lblUsuario.Text = $"{sesion.Nombre} {sesion.Apellidos} ({sesion.Login})".Replace("  ", " ")
        lblLocal.Text = sesion.LocalDescripcion
        lblEstacion.Text = $"{sesion.Estacion} - {sesion.Catalogo}"

        _modulos = modulos.Where(Function(m) licencia.TieneModulo(m.Codigo) AndAlso sesion.Puede(m.Privilegio)).ToList()
        For Each modulo In _modulos
            Dim boton As New BotonLateral(modulo) With {.Size = New Size(lateral.Width, 52)}
            AddHandler boton.Click, Sub() Seleccionar(modulo)
            listaModulos.Controls.Add(boton)
        Next

        If _modulos.Count = 0 Then
            lblTitulo.Text = "Sin módulos"
            txtBuscar.Visible = False
            MostrarVacio("No hay módulos migrados disponibles para este usuario en este departamento.")
        Else
            ' El módulo de la última vez, si este usuario todavía lo tiene; si no, el primero.
            Dim anterior = _modulos.Find(Function(m) String.Equals(m.Codigo, preferencias.MenuModulo, StringComparison.OrdinalIgnoreCase))
            Seleccionar(If(anterior, _modulos(0)))
        End If
    End Sub

    Private Sub Seleccionar(modulo As IModulo)
        _actual = modulo
        For Each boton In listaModulos.Controls.OfType(Of BotonLateral)()
            boton.Activo = boton.Modulo Is modulo
        Next
        If txtBuscar.TextLength > 0 Then txtBuscar.Clear() Else Mostrar()
    End Sub

    Private Sub TxtBuscar_TextChanged(sender As Object, e As EventArgs) Handles txtBuscar.TextChanged
        If _actual IsNot Nothing Then Mostrar()
    End Sub

    Private Sub TxtBuscar_KeyDown(sender As Object, e As KeyEventArgs) Handles txtBuscar.KeyDown
        If e.KeyCode = Keys.Escape Then
            txtBuscar.Clear()
            e.SuppressKeyPress = True
        ElseIf e.KeyCode = Keys.Down OrElse e.KeyCode = Keys.Enter Then
            Dim primera = contenido.Controls.OfType(Of TarjetaOpcion)().FirstOrDefault()
            primera?.Focus()
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub FrmMenu_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ' Sin foco inicial: con foco, el buscador esconde su texto de ayuda.
        ActiveControl = Nothing
    End Sub

    ' ---- Modo claro u oscuro ----

    Private ReadOnly _menuTema As New ContextMenuStrip With {.Renderer = New RenderizadorMenu(), .ShowImageMargin = False, .ShowCheckMargin = True}

    Private Sub PrepararModoColor()
        Tema.PintarBoton(btnTema, EstiloBoton.Normal)
        For Each opcion In {(PreferenciaTema.Claro, "Claro"), (PreferenciaTema.Oscuro, "Oscuro"), (PreferenciaTema.Sistema, "Según Windows")}
            Dim preferencia = opcion.Item1
            Dim item As New ToolStripMenuItem(opcion.Item2) With {.Tag = preferencia}
            AddHandler item.Click, Sub() ElegirModo(preferencia)
            _menuTema.Items.Add(item)
        Next
        AddHandler Tema.Cambio, AddressOf Tema_Cambio
        AddHandler FormClosed, Sub() RemoveHandler Tema.Cambio, AddressOf Tema_Cambio
        MostrarModo()
    End Sub

    Private Sub MostrarModo()
        btnTema.Text = If(Tema.Oscuro, ChrW(&HE708), ChrW(&HE706)).ToString()
        _ayuda.SetToolTip(btnTema, "Modo de color: " & If(Tema.Preferencia = PreferenciaTema.Sistema, "según Windows", Tema.Preferencia.ToString().ToLowerInvariant()))
        For Each item In _menuTema.Items.OfType(Of ToolStripMenuItem)()
            item.Checked = CType(item.Tag, PreferenciaTema) = Tema.Preferencia
        Next
    End Sub

    Private Sub BtnTema_Click(sender As Object, e As EventArgs) Handles btnTema.Click
        _menuTema.Show(btnTema, New Point(btnTema.Width - _menuTema.PreferredSize.Width, btnTema.Height))
    End Sub

    Private Sub ElegirModo(preferencia As PreferenciaTema)
        _preferencias.TemaModo = Tema.TextoPreferencia(preferencia)
        _preferencias.Guardar()
        Tema.Establecer(preferencia)
        MostrarModo()
    End Sub

    ''' <summary>Repinta el menú y las ventanas abiertas; con "Según Windows" puede llegar desde otro hilo.</summary>
    Private Sub Tema_Cambio(anterior As Paleta)
        If InvokeRequired Then
            BeginInvoke(Sub() Tema_Cambio(anterior))
            Return
        End If
        For Each ventana In Application.OpenForms.Cast(Of Form)().ToList()
            Tema.Recolorear(ventana, anterior, ocultarTitulo:=ventana IsNot Me)
        Next
        Tema.PintarBoton(btnTema, EstiloBoton.Normal)
        MostrarModo()
    End Sub

    Private Sub FrmMenu_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If _actual IsNot Nothing Then _preferencias.MenuModulo = _actual.Codigo
        ' Cerrar desde la barra de tareas con la ventana minimizada no dice nada de cómo la quiere.
        If WindowState <> FormWindowState.Minimized Then
            _preferencias.MenuMaximizado = WindowState = FormWindowState.Maximized
        End If
        _preferencias.Guardar()
    End Sub

    Private Sub Mostrar()
        Dim buscando = txtBuscar.Text.Trim().Length > 0
        Dim secciones As New List(Of Seccion)
        If buscando Then
            For Each modulo In _modulos
                Recolectar(modulo.Opciones, modulo.Descripcion, modulo, secciones)
            Next
        Else
            Recolectar(_actual.Opciones, "", _actual, secciones)
        End If
        lblTitulo.Text = If(buscando, "Resultados de la búsqueda", _actual.Descripcion)

        contenido.SuspendLayout()
        Dim anteriores = contenido.Controls.Cast(Of Control)().ToArray()
        contenido.Controls.Clear()
        For Each control In anteriores
            control.Dispose()
        Next
        Dim separacion = LogicalToDeviceUnits(SeparacionTarjetas)
        For Each seccion In secciones
            Dim titulo As New Label With {
                .Text = seccion.Titulo.ToUpperInvariant(), .AutoSize = False, .Height = LogicalToDeviceUnits(34),
                .TextAlign = ContentAlignment.BottomLeft, .ForeColor = Tema.TextoTenue, .Font = New Font(Tema.FuenteSemibold, 8.5F),
                .Margin = New Padding(0, 0, 0, 8)
            }
            contenido.Controls.Add(titulo)
            contenido.SetFlowBreak(titulo, True)
            For Each tarjeta In seccion.Tarjetas
                Dim control As New TarjetaOpcion(tarjeta.Opcion, tarjeta.Modulo) With {
                    .Height = LogicalToDeviceUnits(AltoTarjeta), .Margin = New Padding(0, 0, separacion, separacion)
                }
                _ayuda.SetToolTip(control, tarjeta.Opcion.Etiqueta)
                AddHandler control.Click, AddressOf Tarjeta_Click
                contenido.Controls.Add(control)
            Next
        Next
        AcomodarTarjetas()
        contenido.ResumeLayout()

        Dim total = secciones.Sum(Function(s) s.Tarjetas.Count)
        Dim pendientes = secciones.Sum(Function(s) s.Tarjetas.Where(Function(t) t.Opcion.Crear Is Nothing).Count())
        lblResumen.Text = $"{total} {If(total = 1, "opción", "opciones")}" & If(pendientes > 0, $" · {pendientes} pendientes de migrar", "")
        If total = 0 Then
            MostrarVacio(If(txtBuscar.TextLength > 0, $"Ninguna opción coincide con ""{txtBuscar.Text}"".", "Este usuario no tiene opciones disponibles en este módulo."))
        Else
            lblVacio.Visible = False
        End If
    End Sub

    Private Sub MostrarVacio(texto As String)
        lblVacio.Text = texto
        lblVacio.Visible = True
    End Sub

    ''' <summary>Agrupa las opciones permitidas por sección. Un grupo sin privilegio oculta todo lo que contiene.</summary>
    Private Sub Recolectar(opciones As IEnumerable(Of OpcionMenu), ruta As String, modulo As IModulo, secciones As List(Of Seccion))
        For Each opcion In opciones.Where(Function(o) _sesion.Puede(o.Privilegio))
            If opcion.EsGrupo Then
                Recolectar(opcion.Hijos, If(ruta.Length = 0, opcion.Etiqueta, ruta & " / " & opcion.Etiqueta), modulo, secciones)
            ElseIf Coincide(opcion.Etiqueta) Then
                Dim titulo = If(ruta.Length = 0, SeccionSinGrupo, ruta)
                Dim seccion = secciones.Find(Function(s) s.Titulo = titulo)
                If seccion Is Nothing Then
                    seccion = New Seccion With {.Titulo = titulo}
                    secciones.Add(seccion)
                End If
                seccion.Tarjetas.Add((opcion, modulo))
            End If
        Next
    End Sub

    ''' <summary>Sin distinguir mayúsculas ni tildes: "impresion" encuentra "Impresión".</summary>
    Private Function Coincide(etiqueta As String) As Boolean
        Dim texto = txtBuscar.Text.Trim()
        If texto.Length = 0 Then Return True
        Return CultureInfo.CurrentCulture.CompareInfo.IndexOf(etiqueta, texto, CompareOptions.IgnoreCase Or CompareOptions.IgnoreNonSpace) >= 0
    End Function

    ''' <summary>
    ''' Reparte las tarjetas en columnas. Se calcula aquí, con la barra vertical siempre descontada,
    ''' porque FlowLayoutPanel no reacomoda las filas cuando esa barra aparece y le quita ancho.
    ''' </summary>
    Private Sub AcomodarTarjetas()
        Dim separacion = LogicalToDeviceUnits(SeparacionTarjetas)
        Dim disponible = contenido.Width - contenido.Padding.Left - SystemInformation.VerticalScrollBarWidth - separacion
        Dim columnas = Math.Max(1, disponible \ (LogicalToDeviceUnits(AnchoMinimoTarjeta) + separacion))
        Dim ancho = Math.Max(disponible \ columnas - separacion, 1)
        Dim controles = contenido.Controls.Cast(Of Control)().ToList()
        Dim columna = 0
        For i = 0 To controles.Count - 1
            If TypeOf controles(i) Is Label Then
                controles(i).Width = disponible
                columna = 0
                Continue For
            End If
            controles(i).Width = ancho
            columna += 1
            Dim finDeSeccion = i = controles.Count - 1 OrElse TypeOf controles(i + 1) Is Label
            Dim finDeFila = columna = columnas OrElse finDeSeccion
            contenido.SetFlowBreak(controles(i), finDeFila)
            If finDeFila Then columna = 0
        Next
    End Sub

    Private Sub Contenido_Resize(sender As Object, e As EventArgs) Handles contenido.Resize
        AcomodarTarjetas()
        contenido.PerformLayout()
        ' Al reducir la ventana queda el área de desplazamiento del ancho anterior.
        If contenido.HorizontalScroll.Visible Then
            Dim vertical = contenido.VerticalScroll.Value
            contenido.AutoScroll = False
            contenido.AutoScroll = True
            contenido.AutoScrollPosition = New Point(0, vertical)
        End If
    End Sub

    Private Async Sub Tarjeta_Click(sender As Object, e As EventArgs)
        If _abriendo Then Return
        _abriendo = True
        Try
            Await Task.Delay(EsperaDobleClic)
            Dim tarjeta = CType(sender, TarjetaOpcion)
            Abrir(tarjeta.Opcion, tarjeta.Modulo)
        Finally
            _abriendo = False
        End Try
    End Sub

    Private Sub Abrir(opcion As OpcionMenu, modulo As IModulo)
        If Not _sesion.Puede(opcion.Privilegio) Then Return
        If opcion.Crear Is Nothing Then
            MessageBox.Show(Me, "Esta opción todavía no está migrada. Use el sistema anterior.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Using formulario = opcion.Crear(_sesion)
            Tema.Aplicar(formulario, modulo.Descripcion)
            formulario.ShowDialog(Me)
        End Using
    End Sub

End Class
