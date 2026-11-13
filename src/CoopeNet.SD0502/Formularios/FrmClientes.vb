Imports System.Data
Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Clientes: las condiciones de venta de cada cliente en el local (precio, exoneración, vendedor, descuento,
    ''' contactos y ruta). Migración de SD0502\frmClientes.frm (Mantenimiento, opción 053, privilegio 5038).
    ''' </summary>
    ''' <remarks>
    ''' Como en VB6 (SDSizer y MODOVENTANAS), la ventana se agranda o maximiza con los controles en proporción y
    ''' recuerda si quedó maximizada. Diferencias: Borrar solo borra si se responde Sí (en VB6 la pregunta no
    ''' detenía el borrado) y solo en el local de la pantalla; Guardar pide que el cliente esté en Datos
    ''' personales, porque VB6 guardaba identificaciones vacías o no registradas.
    ''' </remarks>
    Public Class FrmClientes
        Inherits Form

        Private Const NoRegistrado As String = "<<NO REGISTRADO>>"
        Private Const SinDescuento As String = "SIN DESCUENTO"
        Private Const DescuentoMaximo As String = "DESCUENTO MAXIMO DEL ARTICULO"
        Private Const SinRuta As String = "0 - No coincide en ninguna ruta"
        Private Shared ReadOnly Cultura As CultureInfo = CultureInfo.CurrentCulture

        Private ReadOnly _repo As ClientesRepositorio
        Private ReadOnly _local As Long
        Private _catalogos As New CatalogosCliente()
        ''' <summary>iLastReg: el último cliente cargado, para volver a él con Cancelar.</summary>
        Private _ultimo As String = ""
        Private _cargando As Boolean
        Private _editando As Boolean

        ' ---- Cliente ----
        Private ReadOnly txtCliente As New TextBox With {.Name = "txtCliente", .AccessibleName = "Identificación del cliente", .MaxLength = 16, .Location = New Point(70, 10), .Width = 160}
        Private ReadOnly btnCliente As New Button With {.Name = "btnCliente", .Text = "&Buscar", .Location = New Point(240, 9), .Size = New Size(72, 27)}
        Private ReadOnly lblCliente As New Label With {.Name = "lblCliente", .AutoSize = False, .Location = New Point(6, 42), .Size = New Size(600, 25)}
        Private ReadOnly tabs As New Pestanas With {.Name = "tabs", .Location = New Point(12, 74), .Size = New Size(608, 318)}
        Private ReadOnly tabPrecio As New TabPage("Precio") With {.Name = "tabPrecio"}
        Private ReadOnly tabContactos As New TabPage("Contactos") With {.Name = "tabContactos"}
        Private ReadOnly tabOtros As New TabPage("Otros") With {.Name = "tabOtros"}

        ' ---- Precio ----
        Private ReadOnly cmbPrecio As New ComboBox With {.Name = "cmbPrecio", .AccessibleName = "Precio a facturar", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(150, 14), .Width = 260}
        Private ReadOnly cmbExonerado As New ComboBox With {.Name = "cmbExonerado", .AccessibleName = "Exonerado", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(150, 48), .Width = 150}
        Private ReadOnly cmbIva As New ComboBox With {.Name = "cmbIva", .AccessibleName = "IVA", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(350, 48), .Width = 60}
        Private ReadOnly txtVendedor As New TextBox With {.Name = "txtVendedor", .AccessibleName = "Agente o vendedor", .MaxLength = 16, .Location = New Point(150, 82), .Width = 150}
        Private ReadOnly btnVendedor As New Button With {.Name = "btnVendedor", .Text = "Buscar", .Location = New Point(308, 81), .Size = New Size(72, 27)}
        Private ReadOnly lblVendedor As New Label With {.Name = "lblVendedor", .AutoSize = False, .Location = New Point(15, 114), .Size = New Size(560, 23)}
        Private ReadOnly cmbDescuento As New ComboBox With {.Name = "cmbDescuento", .AccessibleName = "Descuento fijo", .DropDownStyle = ComboBoxStyle.DropDown, .Location = New Point(150, 144), .Width = 260}
        Private ReadOnly txtReferencia As New TextBox With {.Name = "txtReferencia", .AccessibleName = "Referencia o código", .MaxLength = 16, .Location = New Point(150, 178), .Width = 150}
        Private ReadOnly txtActividad As New TextBox With {.Name = "txtActividad", .AccessibleName = "Actividad económica", .MaxLength = 10, .Location = New Point(150, 212), .Width = 150}

        ' ---- Contactos ----
        Private ReadOnly txtContacto1 As New TextBox With {.Name = "txtContacto1", .AccessibleName = "Identificación del contacto 1", .MaxLength = 16, .Location = New Point(110, 24), .Width = 130}
        Private ReadOnly btnContacto1 As New Button With {.Name = "btnContacto1", .Text = "Buscar", .Location = New Point(248, 23), .Size = New Size(72, 27)}
        Private ReadOnly lblContacto1 As New Label With {.Name = "lblContacto1", .AutoSize = False, .Location = New Point(10, 56), .Size = New Size(540, 23)}
        Private ReadOnly txtCargo1 As New TextBox With {.Name = "txtCargo1", .AccessibleName = "Cargo del contacto 1", .MaxLength = 64, .Location = New Point(110, 84), .Width = 210}
        Private ReadOnly txtContacto2 As New TextBox With {.Name = "txtContacto2", .AccessibleName = "Identificación del contacto 2", .MaxLength = 16, .Location = New Point(110, 24), .Width = 130}
        Private ReadOnly btnContacto2 As New Button With {.Name = "btnContacto2", .Text = "Buscar", .Location = New Point(248, 23), .Size = New Size(72, 27)}
        Private ReadOnly lblContacto2 As New Label With {.Name = "lblContacto2", .AutoSize = False, .Location = New Point(10, 56), .Size = New Size(540, 23)}
        Private ReadOnly txtCargo2 As New TextBox With {.Name = "txtCargo2", .AccessibleName = "Cargo del contacto 2", .MaxLength = 64, .Location = New Point(110, 84), .Width = 210}

        ' ---- Otros ----
        Private ReadOnly cmbRuta As New ComboBox With {.Name = "cmbRuta", .AccessibleName = "Ruta predeterminada", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(150, 14), .Width = 430}
        Private ReadOnly txtHorario As New TextBox With {.Name = "txtHorario", .AccessibleName = "Horario del cliente", .MaxLength = 128, .Multiline = True, .Location = New Point(150, 52), .Size = New Size(430, 50)}
        Private ReadOnly txtVisita As New TextBox With {.Name = "txtVisita", .AccessibleName = "Horario de visita", .MaxLength = 128, .Multiline = True, .Location = New Point(150, 110), .Size = New Size(430, 50)}
        Private ReadOnly txtNotas As New TextBox With {.Name = "txtNotas", .AccessibleName = "Notas u observaciones", .Multiline = True, .AcceptsReturn = True, .ScrollBars = ScrollBars.Vertical, .Location = New Point(150, 168), .Size = New Size(430, 110)}

        ' ---- Navegación y barra ----
        Private ReadOnly btnPrimero As New Button With {.Name = "btnPrimero", .Text = "<<", .AccessibleName = "Primer cliente", .Location = New Point(12, 402), .Size = New Size(45, 34)}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "<", .AccessibleName = "Cliente anterior", .Location = New Point(58, 402), .Size = New Size(45, 34)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = ">", .AccessibleName = "Cliente siguiente", .Location = New Point(104, 402), .Size = New Size(45, 34)}
        Private ReadOnly btnUltimo As New Button With {.Name = "btnUltimo", .Text = ">>", .AccessibleName = "Último cliente", .Location = New Point(150, 402), .Size = New Size(45, 34)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(205, 402), .Size = New Size(415, 34)}

        Public Sub New(repo As ClientesRepositorio, local As Long)
            _repo = repo
            _local = local
            Text = "Mantenimiento de clientes"
            ClientSize = New Size(632, 448)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            tabPrecio.Controls.AddRange(New Control() {
                Rotulo("Precio a facturar", 15, 17), cmbPrecio,
                Rotulo("Exonerado", 15, 51), cmbExonerado, Rotulo("IVA", 318, 51), cmbIva,
                Rotulo("Agente - Vendedor", 15, 85), txtVendedor, btnVendedor, lblVendedor,
                Rotulo("% descuento fijo", 15, 147), cmbDescuento, Rotulo("%", 414, 147),
                Rotulo("Referencia / código", 15, 181), txtReferencia,
                New Label With {.Text = "Ej. Número de proveedor en caso de la CCSS", .AutoSize = False, .Location = New Point(310, 176), .Size = New Size(270, 34)},
                Rotulo("Act. Económica", 15, 215), txtActividad
            })
            tabContactos.Controls.AddRange(New Control() {
                Contacto("Contacto #1", 10, txtContacto1, btnContacto1, lblContacto1, txtCargo1),
                Contacto("Contacto #2", 136, txtContacto2, btnContacto2, lblContacto2, txtCargo2)
            })
            tabOtros.Controls.AddRange(New Control() {
                Rotulo("Ruta predeterminada", 10, 17), cmbRuta,
                Rotulo("Horario del cliente", 10, 55), txtHorario,
                Rotulo("Horario de visita", 10, 113), txtVisita,
                Rotulo("Notas/observaciones", 10, 171), txtNotas
            })
            tabs.TabPages.AddRange({tabPrecio, tabContactos, tabOtros})
            Controls.AddRange(New Control() {
                Rotulo("Cliente", 6, 13), txtCliente, btnCliente, lblCliente, tabs,
                btnPrimero, btnAnterior, btnSiguiente, btnUltimo, barra
            })

            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler btnPrimero.Click, Async Sub() Await NavegarAsync(DireccionNavegacion.Primero)
            AddHandler btnAnterior.Click, Async Sub() Await NavegarAsync(DireccionNavegacion.Anterior)
            AddHandler btnSiguiente.Click, Async Sub() Await NavegarAsync(DireccionNavegacion.Siguiente)
            AddHandler btnUltimo.Click, Async Sub() Await NavegarAsync(DireccionNavegacion.Ultimo)
            AddHandler btnCliente.Click, Async Sub() Await BuscarPersonaAsync(txtCliente, lblCliente, "Seleccione al cliente", soloFisicas:=False)
            AddHandler btnVendedor.Click, Async Sub() Await BuscarVendedorAsync()
            AddHandler btnContacto1.Click, Async Sub() Await BuscarPersonaAsync(txtContacto1, lblContacto1, "Seleccione al primer contacto", soloFisicas:=True)
            AddHandler btnContacto2.Click, Async Sub() Await BuscarPersonaAsync(txtContacto2, lblContacto2, "Seleccione al segundo contacto", soloFisicas:=True)
            For Each par In {(txtCliente, lblCliente), (txtVendedor, lblVendedor), (txtContacto1, lblContacto1), (txtContacto2, lblContacto2)}
                Dim caja = par.Item1
                Dim lblNombre = par.Item2
                AddHandler caja.KeyDown, Async Sub(s, e)
                                             If e.KeyCode <> Keys.Enter Then Return
                                             e.SuppressKeyPress = True
                                             Await IdentificacionEscritaAsync(caja, lblNombre)
                                         End Sub
            Next
            AddHandler cmbDescuento.KeyPress, AddressOf Descuento_Tecla

            Limpiar(True)
            Habilitar(False)
            Ventanas.Recordar(Me)
        End Sub

        Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Shared Function Contacto(titulo As String, y As Integer, id As TextBox, buscar As Button, nombre As Label, cargo As TextBox) As GroupBox
            Dim grupo As New GroupBox With {.Name = "gb" & id.Name, .Text = titulo, .Location = New Point(10, y), .Size = New Size(578, 120)}
            grupo.Controls.AddRange(New Control() {Rotulo("Identificación", 10, 27), id, buscar, nombre, Rotulo("Cargo", 10, 87), cargo})
            Return grupo
        End Function

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            ' Antes de MyBase.OnLoad, que restaura el último tamaño: las medidas base son las de diseño.
            ' La negrita va después de Tema y antes de Proporcional, para que crezca con la ventana.
            lblCliente.Font = New Font(Font, FontStyle.Bold)
            Proporcional.Aplicar(Me)
            MyBase.OnLoad(e)
            Try
                _catalogos = Await _repo.CatalogosAsync(_local)
                cmbIva.Items.AddRange(_catalogos.SujetosIva.Cast(Of Object)().ToArray())
                Limpiar(True)
            Catch ex As MySqlException
                Mensajes.NoConecta()
                barra.Enabled = False
            End Try
        End Sub

        ' ---- Datos en pantalla ----

        ''' <summary>SD_Limpiar. Con <paramref name="cliente"/> también borra la identificación del cliente.</summary>
        Private Sub Limpiar(cliente As Boolean)
            cmbPrecio.Items.Clear()
            cmbPrecio.Items.Add("0. Precio asignado por el sistema")
            For i = 0 To 5
                cmbPrecio.Items.Add($"{i + 1}. {If(i < _catalogos.EtiquetasPrecio.Count, _catalogos.EtiquetasPrecio(i), "")}")
            Next
            cmbPrecio.SelectedIndex = 0
            If cmbExonerado.Items.Count = 0 Then cmbExonerado.Items.AddRange({"NO", "SI", "No Suj.(Muni, CCSS)"})
            cmbExonerado.SelectedIndex = 0
            cmbIva.SelectedIndex = If(cmbIva.Items.Count > 0, 0, -1)
            If cmbDescuento.Items.Count = 0 Then cmbDescuento.Items.AddRange({SinDescuento, DescuentoMaximo})
            Descuento = SinDescuento

            If cliente Then txtCliente.Text = ""
            For Each caja In {txtVendedor, txtContacto1, txtContacto2, txtReferencia, txtActividad, txtCargo1, txtCargo2, txtHorario, txtVisita, txtNotas}
                caja.Text = ""
            Next
            For Each lblNombre In {lblCliente, lblVendedor, lblContacto1, lblContacto2}
                lblNombre.Text = NoRegistrado
            Next
            MostrarRutas(New List(Of RutaCliente)(), 0)
            tabs.SelectedTab = tabPrecio
        End Sub

        ''' <summary>El texto del combo de descuento, sin dejarlo seleccionado (el combo editable lo selecciona al asignarlo).</summary>
        Private WriteOnly Property Descuento As String
            Set(value As String)
                cmbDescuento.Text = value
                cmbDescuento.SelectionLength = 0
            End Set
        End Property

        Private Sub MostrarRutas(rutas As List(Of RutaCliente), zona As Long)
            cmbRuta.Items.Clear()
            If rutas.Count = 0 Then
                cmbRuta.Items.Add(SinRuta)
            Else
                cmbRuta.Items.AddRange(rutas.ToArray())
            End If
            cmbRuta.SelectedIndex = 0
            For i = 0 To cmbRuta.Items.Count - 1
                If TypeOf cmbRuta.Items(i) Is RutaCliente AndAlso CType(cmbRuta.Items(i), RutaCliente).Ruta = zona Then cmbRuta.SelectedIndex = i
            Next
        End Sub

        ''' <summary>SD_Estado. La identificación del cliente solo se escribe al crear o al editar (VB6 igual).</summary>
        Private Sub Habilitar(editando As Boolean)
            _editando = editando
            For Each control In New Control() {txtCliente, btnCliente, cmbPrecio, cmbExonerado, cmbIva, txtVendedor, btnVendedor, cmbDescuento,
                                               txtReferencia, txtActividad, txtContacto1, btnContacto1, txtCargo1, txtContacto2, btnContacto2, txtCargo2,
                                               cmbRuta, txtHorario, txtVisita, txtNotas}
                control.Enabled = editando
            Next
            For Each boton In {btnPrimero, btnAnterior, btnSiguiente, btnUltimo}
                boton.Enabled = Not editando
            Next
        End Sub

        Private Sub Mostrar(c As ClientePos)
            cmbPrecio.SelectedIndex = If(c.PrecioFac >= 0 AndAlso c.PrecioFac < cmbPrecio.Items.Count, c.PrecioFac, 0)
            cmbExonerado.SelectedIndex = CInt(c.Exonerado)
            If Not cmbIva.Items.Contains(c.Iva) Then cmbIva.Items.Add(c.Iva)
            cmbIva.SelectedItem = c.Iva
            txtVendedor.Text = c.Vendedor
            Descuento = If(c.Descuento = 0, SinDescuento, If(c.Descuento < 0, DescuentoMaximo, c.Descuento.ToString(Cultura)))
            txtReferencia.Text = c.NumCliente
            txtActividad.Text = c.ActividadEconomica
            txtHorario.Text = c.Horario
            txtVisita.Text = c.Visita
            txtNotas.Text = c.Notas
            txtContacto1.Text = c.Contacto1
            txtCargo1.Text = c.Cargo1
            txtContacto2.Text = c.Contacto2
            txtCargo2.Text = c.Cargo2
        End Sub

        ''' <summary>
        ''' SD_Cargar_Cliente: muestra las condiciones del cliente en el local y las rutas de su dirección. Si no
        ''' está registrado como cliente, con <paramref name="enBlanco"/> deja la ficha en blanco.
        ''' </summary>
        Private Async Function CargarClienteAsync(id As String, enBlanco As Boolean) As Task
            If _cargando Then Return
            _cargando = True
            Try
                Dim c = Await _repo.CargarAsync(_local, id)
                If c IsNot Nothing Then
                    txtCliente.Text = id
                    _ultimo = id
                    lblCliente.Text = Await NombreAsync(id)
                    Mostrar(c)
                    lblVendedor.Text = Await NombreAsync(c.Vendedor)
                    lblContacto1.Text = Await NombreAsync(c.Contacto1)
                    lblContacto2.Text = Await NombreAsync(c.Contacto2)
                ElseIf enBlanco Then
                    Limpiar(False)
                End If
                MostrarRutas(Await _repo.RutasAsync(_local, id), If(c?.Zona, 0))
            Finally
                _cargando = False
            End Try
        End Function

        Private Async Function NombreAsync(id As String) As Task(Of String)
            If id.Trim().Length = 0 Then Return NoRegistrado
            Dim nombre = Await _repo.NombreAsync(id.Trim())
            Return If(nombre.Length = 0, NoRegistrado, nombre)
        End Function

        ' ---- Identificaciones ----

        ''' <summary>
        ''' sdtID_KeyPress con Enter y SD_CargarNombre: resuelve la identificación (código de asociado, cédula o
        ''' código de cliente; si hay varias, las ofrece) y muestra el nombre. En el cliente, además, carga sus
        ''' condiciones si ya está registrado.
        ''' </summary>
        Private Async Function IdentificacionEscritaAsync(caja As TextBox, lblNombre As Label) As Task
            Try
                If caja Is txtCliente Then Limpiar(False)
                Dim id = Await ResolverAsync(caja.Text)
                If id.Length = 0 Then
                    MessageBox.Show(Me, "No se encontró la identificación solicitada...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    lblNombre.Text = NoRegistrado
                    Return
                End If
                Await PersonaElegidaAsync(caja, lblNombre, id)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Function PersonaElegidaAsync(caja As TextBox, lblNombre As Label, id As String) As Task
            caja.Text = id
            lblNombre.Text = Await NombreAsync(id)
            If caja Is txtCliente Then Await CargarClienteAsync(id, False)
        End Function

        ''' <summary>SD_Obtener_ID: vacío si no hay coincidencias o si se cancela el buscador.</summary>
        Private Async Function ResolverAsync(clave As String) As Task(Of String)
            If clave.Trim().Length = 0 Then Return ""
            Using tabla = Await _repo.CoincidenciasAsync(clave)
                If tabla.Rows.Count = 0 Then Return ""
                If tabla.Rows.Count = 1 Then Return Convert.ToString(tabla.Rows(0)(0), CultureInfo.InvariantCulture)
                Using buscador As New FrmBuscador("Seleccione la identificación", tabla)
                    If buscador.ShowDialog(Me) <> DialogResult.OK Then Return ""
                    Return buscador.Resultado(0)
                End Using
            End Using
        End Function

        ''' <summary>CmdBuscId 0, 2 y 3: elige una persona de Datos personales; la identificación es la columna 2.</summary>
        Private Async Function BuscarPersonaAsync(caja As TextBox, lblNombre As Label, titulo As String, soloFisicas As Boolean) As Task
            Try
                Using datos = Await _repo.PersonasAsync(soloFisicas)
                    Await ElegirAsync(caja, lblNombre, titulo, datos)
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>CmdBuscId 1: elige el agente o vendedor entre los usuarios del sistema.</summary>
        Private Async Function BuscarVendedorAsync() As Task
            Try
                Using datos = Await _repo.VendedoresAsync()
                    Await ElegirAsync(txtVendedor, lblVendedor, "Seleccione al vendedor", datos)
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Function ElegirAsync(caja As TextBox, lblNombre As Label, titulo As String, datos As DataTable) As Task
            Using buscador As New FrmBuscador(titulo, datos)
                If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(2).Length = 0 Then Return
                If caja Is txtCliente Then Limpiar(False)
                Await PersonaElegidaAsync(caja, lblNombre, buscador.Resultado(2))
            End Using
        End Function

        ''' <summary>El descuento fijo es un porcentaje: en el texto del combo solo entran dígitos y el punto decimal.</summary>
        Private Sub Descuento_Tecla(sender As Object, e As KeyPressEventArgs)
            If Char.IsControl(e.KeyChar) OrElse Char.IsDigit(e.KeyChar) Then Return
            If e.KeyChar.ToString() = Cultura.NumberFormat.NumberDecimalSeparator AndAlso Not cmbDescuento.Text.Contains(e.KeyChar) Then Return
            e.Handled = True
        End Sub

        Private Function DescuentoElegido() As Double
            Select Case cmbDescuento.Text.Trim()
                Case DescuentoMaximo : Return ClientesRepositorio.DescuentoMaximo
                Case SinDescuento, "" : Return 0
            End Select
            Dim valor As Double
            Return If(Double.TryParse(cmbDescuento.Text.Trim(), NumberStyles.Number, Cultura, valor), Math.Max(valor, 0), 0)
        End Function

        ''' <summary>Horario y visita son de una línea en VB6 (varchar sin saltos).</summary>
        Private Shared Function UnaLinea(texto As String) As String
            Return texto.Replace(vbCrLf, " ").Replace(vbLf, " ").Replace(vbCr, " ").Trim()
        End Function

        ' ---- Navegación ----

        ''' <summary>CmdMandos_Click.</summary>
        Private Async Function NavegarAsync(direccion As DireccionNavegacion) As Task
            Try
                Dim id = Await _repo.VecinoAsync(_local, txtCliente.Text, direccion)
                If id.Length = 0 Then
                    Limpiar(True)
                Else
                    Await CargarClienteAsync(id, True)
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ' ---- Barra ----

        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        Limpiar(True)
                        Habilitar(True)
                        txtCliente.Focus()
                    Case BotonBarra.Editar
                        ' VB6: la identificación debe tener más de tres caracteres.
                        If txtCliente.Text.Trim().Length <= 3 Then Return True
                        Habilitar(True)
                        txtCliente.Enabled = False
                        btnCliente.Enabled = False
                        cmbPrecio.Focus()
                    Case BotonBarra.Guardar
                        If Not Await GuardarAsync() Then Return True
                        Habilitar(False)
                    Case BotonBarra.Cancelar
                        If _ultimo.Length = 0 Then
                            Limpiar(True)
                        Else
                            Await CargarClienteAsync(_ultimo, True)
                        End If
                        Habilitar(False)
                    Case BotonBarra.Borrar
                        Await BorrarAsync()
                    Case BotonBarra.Buscar
                        Await BuscarAsync()
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CLIENTES", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SD_Guardar.</summary>
        Private Async Function GuardarAsync() As Task(Of Boolean)
            Dim id = txtCliente.Text.Trim()
            If id.Length = 0 OrElse (Await _repo.NombreAsync(id)).Length = 0 Then
                MessageBox.Show(Me, "El cliente debe estar registrado en Datos personales.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtCliente.Focus()
                Return False
            End If
            Dim c As New ClientePos With {
                .Cliente = id, .PrecioFac = Math.Max(cmbPrecio.SelectedIndex, 0),
                .Exonerado = CType(Math.Max(cmbExonerado.SelectedIndex, 0), ExoneracionCliente),
                .Vendedor = txtVendedor.Text.Trim(), .Descuento = DescuentoElegido(), .NumCliente = txtReferencia.Text.Trim(),
                .Zona = If(TryCast(cmbRuta.SelectedItem, RutaCliente)?.Ruta, 0),
                .Horario = UnaLinea(txtHorario.Text), .Visita = UnaLinea(txtVisita.Text),
                .Contacto1 = txtContacto1.Text.Trim(), .Cargo1 = txtCargo1.Text.Trim(),
                .Contacto2 = txtContacto2.Text.Trim(), .Cargo2 = txtCargo2.Text.Trim(),
                .Notas = txtNotas.Text, .Iva = If(cmbIva.SelectedItem Is Nothing, 0, CInt(cmbIva.SelectedItem)),
                .ActividadEconomica = txtActividad.Text.Trim()
            }
            Await _repo.GuardarAsync(c, _local)
            _ultimo = id
            MessageBox.Show(Me, "Se ha guardado el registro exitosamente...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return True
        End Function

        ''' <summary>SDBoton_Borrar: quita al cliente del local; su ficha de Datos personales queda.</summary>
        Private Async Function BorrarAsync() As Task
            Dim id = txtCliente.Text.Trim()
            If id.Length = 0 OrElse (Await _repo.CargarAsync(_local, id)) Is Nothing Then Return
            If MessageBox.Show(Me, "¿Desea borrar la información de este cliente?" & vbCrLf & "Sus datos personales no se borran.",
                               Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Await _repo.BorrarAsync(id, _local)
            _ultimo = ""
            Limpiar(True)
            MessageBox.Show(Me, "Los datos del cliente han sido borrados exitosamente", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Function

        ''' <summary>Buscar de la barra: los clientes del local; la identificación es la columna 1.</summary>
        Private Async Function BuscarAsync() As Task
            Using datos = Await _repo.BuscarAsync(_local)
                Using buscador As New FrmBuscador("Seleccione el cliente", datos)
                    If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(1).Length = 0 Then Return
                    Await CargarClienteAsync(buscador.Resultado(1), True)
                End Using
            End Using
        End Function

    End Class

End Namespace
