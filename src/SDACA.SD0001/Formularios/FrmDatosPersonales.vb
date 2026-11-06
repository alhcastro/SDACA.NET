Imports MySqlConnector
Imports SDACA.Core.Catalogos
Imports SDACA.Ui

Namespace SD0001

    ''' <summary>
    ''' Datos Personales. Migración de Fuentes\SD0001\FrmDatosPersonales.frm con los controles NumLoc y
    ''' SDLoc. Se lista, se busca, se agrega y se edita; como en VB6 no se borran personas. La entrada
    ''' desde otros módulos (ClsDP) y el buscador del padrón todavía no están.
    ''' </summary>
    Public Class FrmDatosPersonales
        Inherits Form

        Private Enum ModoPantalla
            Normal
            Nuevo
            Editar
        End Enum

        ''' <summary>Catálogos que dependen de la provincia, ya leídos para pintarlos de una vez.</summary>
        Private NotInheritable Class UbicacionLeida
            Public Property Provincia As String = ""
            Public Property Canton As String = ""
            Public Property Distrito As String = ""
            Public Property Sector As String = ""
            Public Property Cantones As New List(Of ItemCatalogo)
            Public Property Distritos As New List(Of ItemCatalogo)
            Public Property Sectores As New List(Of ItemCatalogo)
        End Class

        Private Const TamanoMaximoFoto As Integer = 262144
        Private Const LargoMaximoId As Integer = 16

        Private ReadOnly _repo As DatosPersonalesRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _puedeEditar As Boolean
        Private ReadOnly _esAdministrador As Boolean
        Private ReadOnly _puedeImagenes As Boolean
        Private ReadOnly _puedeCalcular As Boolean
        Private _etiquetaAsociado As String = "Cliente"
        Private _modo As ModoPantalla = ModoPantalla.Normal
        Private _persona As Persona
        Private _representantes As New List(Of RepresentanteLegal)
        Private _codigos As New List(Of CodigoPersona)
        Private _pintando As Boolean
        Private _version As Integer

        ' Lista
        Private ReadOnly txtFiltro As New TextBox With {.Name = "txtFiltro", .AccessibleName = "Filtro de personas", .Location = New Point(10, 10), .Width = 270}
        Private ReadOnly lblConteo As New Label With {.Name = "lblConteo", .AutoSize = False, .Location = New Point(10, 38), .Size = New Size(270, 18)}
        Private ReadOnly lstPersonas As New ListBox With {.Name = "lstPersonas", .AccessibleName = "Personas", .IntegralHeight = False, .Location = New Point(10, 58), .Size = New Size(270, 576)}
        Private WithEvents tmrFiltro As New System.Windows.Forms.Timer With {.Interval = 300}

        ' Identificación
        Private ReadOnly gbPersonal As New GroupBox With {.Name = "gbPersonal", .Text = "Identificación", .Location = New Point(290, 8), .Size = New Size(700, 166)}
        Private ReadOnly cmbTipoId As New ComboBox With {.Name = "cmbTipoId", .AccessibleName = "Tipo de identificación", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(100, 24), .Width = 220}
        Private ReadOnly txtId As New TextBox With {.Name = "txtId", .AccessibleName = "Identificación", .Location = New Point(330, 24), .Width = 190}
        Private ReadOnly lblApellidos As New Label With {.Name = "lblApellidos", .Text = "Apellidos", .AutoSize = True, .Location = New Point(12, 61)}
        Private ReadOnly txtApellido1 As New TextBox With {.Name = "txtApellido1", .AccessibleName = "Primer apellido", .MaxLength = 32, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(100, 58), .Width = 205}
        Private ReadOnly txtApellido2 As New TextBox With {.Name = "txtApellido2", .AccessibleName = "Segundo apellido", .MaxLength = 32, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(315, 58), .Width = 205}
        Private ReadOnly lblNombre As New Label With {.Name = "lblNombre", .Text = "Nombre", .AutoSize = True, .Location = New Point(12, 95)}
        Private ReadOnly txtNombre As New TextBox With {.Name = "txtNombre", .AccessibleName = "Nombre", .MaxLength = 100, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(100, 92), .Width = 420}
        Private ReadOnly picFoto As New PictureBox With {.Name = "picFoto", .AccessibleName = "Fotografía", .BorderStyle = BorderStyle.FixedSingle, .SizeMode = PictureBoxSizeMode.Zoom, .Location = New Point(540, 20), .Size = New Size(146, 104)}
        Private ReadOnly btnAgregarFoto As New Button With {.Name = "btnAgregarFoto", .Text = "Cambiar", .Location = New Point(540, 130), .Size = New Size(70, 26)}
        Private ReadOnly btnQuitarFoto As New Button With {.Name = "btnQuitarFoto", .Text = "Quitar", .Location = New Point(616, 130), .Size = New Size(70, 26)}

        ' Pestañas
        Private ReadOnly tabs As New Pestanas With {.Name = "tabs", .Location = New Point(290, 180), .Size = New Size(700, 410)}
        Private ReadOnly tabFicha As New TabPage("Ficha General")
        Private ReadOnly tabUbicacion As New TabPage("Ubicación y Contacto")
        Private ReadOnly tabRepresentantes As New TabPage("Representante Legal")
        Private ReadOnly tabCodigos As New TabPage("Códigos")

        ' Ficha General
        Private ReadOnly lblAlias As New Label With {.Text = "Conocido como", .AutoSize = True, .Location = New Point(12, 19)}
        Private ReadOnly txtAlias As New TextBox With {.Name = "txtAlias", .AccessibleName = "Conocido como", .MaxLength = 16, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(130, 16), .Width = 200}
        Private ReadOnly cmbGenero As New ComboBox With {.Name = "cmbGenero", .AccessibleName = "Género", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 50), .Width = 200}
        Private ReadOnly lblNacimiento As New Label With {.Text = "Fecha Nac.", .AutoSize = True, .Location = New Point(12, 87)}
        Private ReadOnly dtpNacimiento As New DateTimePicker With {.Name = "dtpNacimiento", .AccessibleName = "Fecha de nacimiento", .Format = DateTimePickerFormat.Short, .Location = New Point(130, 84), .Width = 200}
        Private ReadOnly lblEstadoCivil As New Label With {.Text = "Estado civil", .AutoSize = True, .Location = New Point(12, 121)}
        Private ReadOnly cmbEstadoCivil As New ComboBox With {.Name = "cmbEstadoCivil", .AccessibleName = "Estado civil", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 118), .Width = 200}
        Private ReadOnly cmbReside As New ComboBox With {.Name = "cmbReside", .AccessibleName = "Reside en", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 152), .Width = 200}
        Private ReadOnly lblNacionalidad As New Label With {.Text = "Nacionalidad", .AutoSize = True, .Location = New Point(12, 189)}
        Private ReadOnly cmbNacionalidad As New ComboBox With {.Name = "cmbNacionalidad", .AccessibleName = "Nacionalidad", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 186), .Width = 200}
        Private ReadOnly lblPuesto As New Label With {.Text = "Puesto", .AutoSize = True, .Location = New Point(12, 223)}
        Private ReadOnly txtPuesto As New TextBox With {.Name = "txtPuesto", .AccessibleName = "Puesto", .MaxLength = 64, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(130, 220), .Width = 530}
        Private ReadOnly lblCompania As New Label With {.Text = "Compañía", .AutoSize = True, .Location = New Point(12, 257)}
        Private ReadOnly txtCompania As New TextBox With {.Name = "txtCompania", .AccessibleName = "Compañía", .MaxLength = 64, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(130, 254), .Width = 530}
        Private ReadOnly optActivo As New RadioButton With {.Name = "optActivo", .Text = "Activo", .AutoSize = True, .Location = New Point(130, 288)}
        Private ReadOnly optInactivo As New RadioButton With {.Name = "optInactivo", .Text = "Inactivo", .AutoSize = True, .Location = New Point(220, 288)}
        Private ReadOnly lblDefuncion As New Label With {.Text = "Fecha defunción", .AutoSize = True, .Location = New Point(12, 323), .Visible = False}
        Private ReadOnly txtDefuncion As New TextBox With {.Name = "txtDefuncion", .AccessibleName = "Fecha de defunción", .ReadOnly = True, .TabStop = False, .Location = New Point(130, 320), .Width = 200, .Visible = False}

        ' Ubicación y contacto
        Private ReadOnly cmbProvincia As New ComboBox With {.Name = "cmbProvincia", .AccessibleName = "Provincia", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 14), .Width = 225}
        Private ReadOnly cmbCanton As New ComboBox With {.Name = "cmbCanton", .AccessibleName = "Cantón", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 46), .Width = 225}
        Private ReadOnly cmbDistrito As New ComboBox With {.Name = "cmbDistrito", .AccessibleName = "Distrito", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 78), .Width = 225}
        Private ReadOnly cmbSector As New ComboBox With {.Name = "cmbSector", .AccessibleName = "Sector", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 110), .Width = 225}
        Private ReadOnly btnPredeterminados As New Button With {.Name = "btnPredeterminados", .Text = "Valores predeterminados", .Location = New Point(90, 142), .Size = New Size(225, 28)}
        Private ReadOnly txtDireccion As New TextBox With {.Name = "txtDireccion", .AccessibleName = "Dirección por señas", .MaxLength = 255, .Multiline = True, .ScrollBars = ScrollBars.Vertical, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(12, 200), .Size = New Size(303, 165)}
        Private ReadOnly gridContactos As New DataGridView With {.Name = "gridContactos", .AccessibleName = "Teléfonos y correos", .Location = New Point(330, 14), .Size = New Size(350, 351),
            .AllowUserToDeleteRows = True, .RowHeadersVisible = False, .MultiSelect = False, .SelectionMode = DataGridViewSelectionMode.CellSelect}
        Private ReadOnly colTipoContacto As New DataGridViewComboBoxColumn With {.Name = "colTipoContacto", .HeaderText = "Tipo", .ValueMember = "Tipo", .DisplayMember = "Descripcion", .Width = 150, .FlatStyle = FlatStyle.Flat}
        Private ReadOnly colDatoContacto As New DataGridViewTextBoxColumn With {.Name = "colDatoContacto", .HeaderText = "Dato", .MaxInputLength = 64, .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill}

        ' Representante legal
        Private ReadOnly txtRepId As New TextBox With {.Name = "txtRepId", .AccessibleName = "Identificación del representante", .MaxLength = LargoMaximoId, .Location = New Point(130, 14), .Width = 180}
        Private ReadOnly btnBuscarRep As New Button With {.Name = "btnBuscarRep", .Text = "Buscar", .Location = New Point(318, 12), .Size = New Size(80, 28)}
        Private ReadOnly lblRepNombre As New Label With {.Name = "lblRepNombre", .AutoSize = False, .Location = New Point(404, 17), .Size = New Size(276, 20)}
        Private ReadOnly txtRepPersoneria As New TextBox With {.Name = "txtRepPersoneria", .AccessibleName = "Referencia de personería", .MaxLength = 255, .Location = New Point(130, 46), .Width = 268}
        Private ReadOnly dtpRepFecha As New DateTimePicker With {.Name = "dtpRepFecha", .AccessibleName = "Fecha de personería", .Format = DateTimePickerFormat.Short, .Location = New Point(130, 78), .Width = 180}
        Private ReadOnly btnAgregarRep As New Button With {.Name = "btnAgregarRep", .Text = "Agregar", .Location = New Point(318, 76), .Size = New Size(80, 28)}
        Private ReadOnly gridRepresentantes As New DataGridView With {.Name = "gridRepresentantes", .AccessibleName = "Representantes legales", .Location = New Point(12, 114), .Size = New Size(580, 226),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .RowHeadersVisible = False, .MultiSelect = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect}
        Private ReadOnly btnRepArriba As New Button With {.Name = "btnRepArriba", .Text = "Subir", .AccessibleName = "Subir representante", .Location = New Point(600, 114), .Size = New Size(80, 28)}
        Private ReadOnly btnRepAbajo As New Button With {.Name = "btnRepAbajo", .Text = "Bajar", .AccessibleName = "Bajar representante", .Location = New Point(600, 148), .Size = New Size(80, 28)}
        Private ReadOnly btnRepBorrar As New Button With {.Name = "btnRepBorrar", .Text = "Borrar", .AccessibleName = "Marcar o desmarcar para borrar", .Location = New Point(600, 182), .Size = New Size(80, 28)}

        ' Códigos
        Private ReadOnly gridCodigos As New DataGridView With {.Name = "gridCodigos", .AccessibleName = "Códigos internos asignados", .Location = New Point(12, 14), .Size = New Size(520, 351),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .RowHeadersVisible = False, .MultiSelect = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect}
        Private ReadOnly btnAgregarCodigo As New Button With {.Name = "btnAgregarCodigo", .Text = "Agregar código", .Location = New Point(542, 14), .Size = New Size(138, 30)}
        Private ReadOnly btnEliminarCodigo As New Button With {.Name = "btnEliminarCodigo", .Text = "Eliminar código", .Location = New Point(542, 50), .Size = New Size(138, 30)}

        Private ReadOnly btnImagenes As New Button With {.Name = "btnImagenes", .Text = "Imágenes", .Location = New Point(290, 600), .Size = New Size(100, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(530, 600), .Size = New Size(460, 32)}

        Public Sub New(repositorio As DatosPersonalesRepositorio, uid As Long, local As Long, puedeEditar As Boolean, esAdministrador As Boolean, puedeImagenes As Boolean, puedeCalcular As Boolean)
            _repo = repositorio
            _uid = uid
            _local = local
            _puedeEditar = puedeEditar
            _esAdministrador = esAdministrador
            _puedeImagenes = puedeImagenes
            _puedeCalcular = puedeCalcular
            Text = "Datos Personales"
            ClientSize = New Size(1000, 644)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            gbPersonal.Controls.AddRange(New Control() {
                New Label With {.Text = "Identificación", .AutoSize = True, .Location = New Point(12, 27)}, cmbTipoId, txtId,
                lblApellidos, txtApellido1, txtApellido2, lblNombre, txtNombre, picFoto, btnAgregarFoto, btnQuitarFoto
            })
            tabFicha.Controls.AddRange(New Control() {
                lblAlias, txtAlias, New Label With {.Text = "Género", .AutoSize = True, .Location = New Point(12, 53)}, cmbGenero,
                lblNacimiento, dtpNacimiento, lblEstadoCivil, cmbEstadoCivil,
                New Label With {.Text = "Reside en", .AutoSize = True, .Location = New Point(12, 155)}, cmbReside,
                lblNacionalidad, cmbNacionalidad, lblPuesto, txtPuesto, lblCompania, txtCompania,
                New Label With {.Text = "Estado", .AutoSize = True, .Location = New Point(12, 290)}, optActivo, optInactivo,
                lblDefuncion, txtDefuncion
            })
            tabUbicacion.Controls.AddRange(New Control() {
                New Label With {.Text = "Provincia", .AutoSize = True, .Location = New Point(12, 17)}, cmbProvincia,
                New Label With {.Text = "Cantón", .AutoSize = True, .Location = New Point(12, 49)}, cmbCanton,
                New Label With {.Text = "Distrito", .AutoSize = True, .Location = New Point(12, 81)}, cmbDistrito,
                New Label With {.Text = "Sector", .AutoSize = True, .Location = New Point(12, 113)}, cmbSector, btnPredeterminados,
                New Label With {.Text = "Dirección por señas", .AutoSize = True, .Location = New Point(12, 180)}, txtDireccion, gridContactos
            })
            tabRepresentantes.Controls.AddRange(New Control() {
                New Label With {.Text = "Id. Representante", .AutoSize = True, .Location = New Point(12, 17)}, txtRepId, btnBuscarRep, lblRepNombre,
                New Label With {.Text = "Ref. Personería", .AutoSize = True, .Location = New Point(12, 49)}, txtRepPersoneria,
                New Label With {.Text = "Fecha Personería", .AutoSize = True, .Location = New Point(12, 81)}, dtpRepFecha, btnAgregarRep,
                gridRepresentantes, btnRepArriba, btnRepAbajo, btnRepBorrar,
                New Label With {.Text = "Registre el representante en Datos Personales antes de seleccionarlo.", .AutoSize = True, .Location = New Point(12, 348)}
            })
            tabCodigos.Controls.AddRange(New Control() {gridCodigos, btnAgregarCodigo, btnEliminarCodigo})
            ' Representante Legal arranca en el control para que Tema la vista y la escale; AplicarTipo la quita.
            tabs.TabPages.AddRange({tabFicha, tabUbicacion, tabRepresentantes, tabCodigos})

            gridContactos.Columns.AddRange(colTipoContacto, colDatoContacto)
            gridRepresentantes.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colRepId", .HeaderText = "Id.", .Width = 110})
            gridRepresentantes.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colRepNombre", .HeaderText = "Nombre", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            gridRepresentantes.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colRepPersoneria", .HeaderText = "Personería", .Width = 130})
            gridRepresentantes.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colRepFecha", .HeaderText = "Fecha", .Width = 90})
            gridCodigos.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colCodigo", .HeaderText = "Código", .Width = 170})
            gridCodigos.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colTipoCodigo", .HeaderText = "Tipo", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})

            Controls.AddRange(New Control() {txtFiltro, lblConteo, lstPersonas, gbPersonal, tabs, btnImagenes, barra})

            barra.AlHacerClic = AddressOf Barra_Clic
            barra.Estado(BotonBarra.Borrar, False)
            barra.Estado(BotonBarra.Nuevo, _puedeEditar)
            barra.Estado(BotonBarra.Editar, _puedeEditar)
            btnPredeterminados.Visible = _esAdministrador

            AddHandler Load, AddressOf Cargar
            AddHandler FormClosed, Sub() tmrFiltro.Dispose()
            AddHandler txtFiltro.TextChanged, Sub()
                                                  tmrFiltro.Stop()
                                                  tmrFiltro.Start()
                                              End Sub
            AddHandler lstPersonas.SelectedIndexChanged, AddressOf Persona_Seleccionada
            AddHandler cmbTipoId.SelectedIndexChanged, AddressOf TipoId_Cambio
            AddHandler txtId.KeyPress, AddressOf Id_Tecla
            AddHandler txtId.Leave, AddressOf Id_Salir
            AddHandler cmbProvincia.SelectedIndexChanged, AddressOf Ubicacion_Cambio
            AddHandler cmbCanton.SelectedIndexChanged, AddressOf Ubicacion_Cambio
            AddHandler cmbDistrito.SelectedIndexChanged, AddressOf Ubicacion_Cambio
            AddHandler btnPredeterminados.Click, AddressOf Predeterminados_Clic
            AddHandler btnAgregarFoto.Click, AddressOf AgregarFoto_Clic
            AddHandler btnQuitarFoto.Click, AddressOf QuitarFoto_Clic
            AddHandler btnImagenes.Click, AddressOf Imagenes_Clic
            AddHandler gridContactos.DefaultValuesNeeded, Sub(s, e) e.Row.Cells(colTipoContacto.Index).Value = 0
            AddHandler gridContactos.DataError, Sub(s, e) e.ThrowException = False
            AddHandler txtRepId.Leave, AddressOf RepId_Salir
            AddHandler btnBuscarRep.Click, AddressOf BuscarRep_Clic
            AddHandler btnAgregarRep.Click, AddressOf AgregarRep_Clic
            AddHandler gridRepresentantes.SelectionChanged, AddressOf Representante_Seleccionado
            AddHandler btnRepArriba.Click, Sub() MoverRepresentante(-1)
            AddHandler btnRepAbajo.Click, Sub() MoverRepresentante(1)
            AddHandler btnRepBorrar.Click, AddressOf BorrarRep_Clic
            AddHandler gridCodigos.SelectionChanged, Sub() ValidarCodigos()
            AddHandler btnAgregarCodigo.Click, AddressOf AgregarCodigo_Clic
            AddHandler btnEliminarCodigo.Click, AddressOf EliminarCodigo_Clic
        End Sub

        ''' <summary>Enter pasa al siguiente campo, como keybd_event 9 en VB6.</summary>
        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.Enter Then
                Dim actual = ActiveControl
                Dim esCampo = TypeOf actual Is ComboBox OrElse TypeOf actual Is DateTimePicker OrElse
                              (TypeOf actual Is TextBox AndAlso Not CType(actual, TextBox).Multiline)
                If esCampo AndAlso TypeOf actual IsNot IDataGridViewEditingControl Then
                    SelectNextControl(actual, True, True, True, True)
                    Return True
                End If
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                _etiquetaAsociado = Await _repo.EtiquetaAsociadoAsync(_local)
                cmbTipoId.Items.AddRange((Await _repo.TiposIdentificacionAsync()).ToArray())
                Dim paises = (Await _repo.PaisesAsync()).ToArray()
                cmbReside.Items.AddRange(paises)
                cmbNacionalidad.Items.AddRange(paises)
                cmbEstadoCivil.Items.AddRange((Await _repo.EstadosCivilesAsync()).ToArray())
                colTipoContacto.DataSource = Await _repo.TiposContactoAsync()
                cmbProvincia.Items.AddRange((Await _repo.ProvinciasAsync()).ToArray())
                Await LimpiarAsync()
                Await ListarAsync()
                Editable()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ' ---- Lista ----

        Private Async Function ListarAsync() As Task
            Dim resultado = Await _repo.ListarAsync(txtFiltro.Text)
            _pintando = True
            lstPersonas.Items.Clear()
            lstPersonas.Items.AddRange(resultado.Filas.ToArray())
            _pintando = False
            lblConteo.Text = $"Mostrando {resultado.Filas.Count} de {resultado.Total:N0}"
        End Function

        Private Async Sub tmrFiltro_Tick(sender As Object, e As EventArgs) Handles tmrFiltro.Tick
            tmrFiltro.Stop()
            Try
                Await ListarAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Persona_Seleccionada(sender As Object, e As EventArgs)
            Dim fila = TryCast(lstPersonas.SelectedItem, PersonaLista)
            If _pintando OrElse fila Is Nothing Then Return
            Try
                Await CargarPersonaAsync(fila.Id)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ' ---- Combos ----

        Private Shared Sub Seleccionar(combo As ComboBox, codigo As String)
            codigo = If(codigo, "").Trim()
            combo.SelectedIndex = combo.Items.Cast(Of ItemCatalogo)().ToList().FindIndex(Function(i) i.Codigo.Trim() = codigo)
        End Sub

        Private Shared Function Valor(combo As ComboBox) As String
            Dim item = TryCast(combo.SelectedItem, ItemCatalogo)
            Return If(item Is Nothing, "", item.Codigo.Trim())
        End Function

        Private ReadOnly Property TipoActual As TipoIdentificacion
            Get
                Return TryCast(cmbTipoId.SelectedItem, TipoIdentificacion)
            End Get
        End Property

        Private Sub FijarTipo(tipo As Integer)
            _pintando = True
            cmbTipoId.SelectedIndex = cmbTipoId.Items.Cast(Of TipoIdentificacion)().ToList().FindIndex(Function(t) t.Tipo = tipo)
            _pintando = False
            AplicarTipo()
        End Sub

        ''' <summary>
        ''' Los géneros vacíos y el 3 que hay en la base se muestran como Masculino, igual que en VB6.
        ''' Una persona jurídica siempre guarda el 2.
        ''' </summary>
        Private Sub FijarGenero(genero As String)
            cmbGenero.Items.Clear()
            If genero = "2" Then
                cmbGenero.Items.Add("Persona Jurídica")
                cmbGenero.SelectedIndex = 0
            Else
                cmbGenero.Items.AddRange({"Masculino", "Femenino"})
                cmbGenero.SelectedIndex = If(genero = "1", 1, 0)
            End If
        End Sub

        Private Function GeneroActual() As String
            Select Case CStr(cmbGenero.SelectedItem)
                Case "Femenino" : Return "1"
                Case "Persona Jurídica" : Return "2"
                Case Else : Return "0"
            End Select
        End Function

        ''' <summary>Equivale a Property Let TipoID: una persona jurídica cambia etiquetas y campos.</summary>
        Private Sub AplicarTipo()
            Dim tipo = TipoActual
            Dim juridica = tipo IsNot Nothing AndAlso tipo.Tipo.ToString() = Persona.TipoJuridica
            lblNombre.Text = If(juridica, "Compañía", "Nombre")
            lblAlias.Text = If(juridica, "Nom. Fantasía", "Conocido como")
            lblNacimiento.Text = If(juridica, "Constituida el", "Fecha Nac.")
            lblNacionalidad.Text = If(juridica, "País", "Nacionalidad")
            For Each c As Control In New Control() {lblApellidos, txtApellido1, txtApellido2, lblEstadoCivil, cmbEstadoCivil, lblPuesto, txtPuesto, lblCompania, txtCompania}
                c.Visible = Not juridica
            Next
            If juridica AndAlso Not tabs.TabPages.Contains(tabRepresentantes) Then
                tabs.TabPages.Insert(2, tabRepresentantes)
            ElseIf Not juridica AndAlso tabs.TabPages.Contains(tabRepresentantes) Then
                tabs.TabPages.Remove(tabRepresentantes)
            End If
            If tipo IsNot Nothing Then txtId.MaxLength = Math.Min(tipo.Maximo, LargoMaximoId)
        End Sub

        Private Sub TipoId_Cambio(sender As Object, e As EventArgs)
            If _pintando Then Return
            AplicarTipo()
            If _modo = ModoPantalla.Nuevo Then FijarGenero(If(TipoActual?.Tipo.ToString() = Persona.TipoJuridica, "2", "0"))
        End Sub

        ' ---- Ubicación ----

        Private Shared Function Vigente(lista As List(Of ItemCatalogo), codigo As String, completar As Boolean) As String
            codigo = If(codigo, "").Trim()
            If lista.Any(Function(i) i.Codigo.Trim() = codigo) Then Return codigo
            Return If(completar AndAlso lista.Count > 0, lista(0).Codigo.Trim(), "")
        End Function

        ''' <summary>Con <paramref name="completar"/> toma el primero de cada lista, como al cambiar un combo.</summary>
        Private Async Function LeerUbicacionAsync(provincia As String, canton As String, distrito As String, sector As String, completar As Boolean) As Task(Of UbicacionLeida)
            Dim u As New UbicacionLeida With {.Provincia = If(provincia, "").Trim()}
            If u.Provincia.Length = 0 Then Return u
            u.Cantones = Await _repo.CantonesAsync(u.Provincia)
            u.Canton = Vigente(u.Cantones, canton, completar)
            If u.Canton.Length = 0 Then Return u
            u.Distritos = Await _repo.DistritosAsync(u.Provincia, u.Canton)
            u.Distrito = Vigente(u.Distritos, distrito, completar)
            If u.Distrito.Length = 0 Then Return u
            u.Sectores = Await _repo.SectoresAsync(u.Provincia, u.Canton, u.Distrito)
            u.Sector = Vigente(u.Sectores, sector, completar)
            Return u
        End Function

        Private Sub PintarUbicacion(u As UbicacionLeida)
            Dim antes = _pintando
            _pintando = True
            Seleccionar(cmbProvincia, u.Provincia)
            cmbCanton.Items.Clear()
            cmbCanton.Items.AddRange(u.Cantones.ToArray())
            Seleccionar(cmbCanton, u.Canton)
            cmbDistrito.Items.Clear()
            cmbDistrito.Items.AddRange(u.Distritos.ToArray())
            Seleccionar(cmbDistrito, u.Distrito)
            cmbSector.Items.Clear()
            cmbSector.Items.AddRange(u.Sectores.ToArray())
            Seleccionar(cmbSector, u.Sector)
            _pintando = antes
        End Sub

        Private Async Function UbicacionPredeterminadaAsync() As Task(Of UbicacionLeida)
            Dim codigos = Await _repo.UbicacionPredeterminadaAsync(_local)
            Return Await LeerUbicacionAsync(codigos(0), codigos(1), codigos(2), codigos(3), False)
        End Function

        Private Async Sub Ubicacion_Cambio(sender As Object, e As EventArgs)
            If _pintando Then Return
            Try
                Dim canton = If(sender Is cmbProvincia, "", Valor(cmbCanton))
                Dim distrito = If(sender Is cmbDistrito, Valor(cmbDistrito), "")
                PintarUbicacion(Await LeerUbicacionAsync(Valor(cmbProvincia), canton, distrito, "", True))
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Predeterminados_Clic(sender As Object, e As EventArgs)
            If Valor(cmbProvincia) = "" OrElse Valor(cmbCanton) = "" OrElse Valor(cmbDistrito) = "" OrElse Valor(cmbSector) = "" Then
                MessageBox.Show(Me, "No se pueden establecer los valores como predeterminados porque están incompletos.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            If MessageBox.Show(Me, "¿Desea establecer los valores de localización como predeterminados?", Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Try
                Await _repo.GuardarUbicacionPredeterminadaAsync(Valor(cmbProvincia), Valor(cmbCanton), Valor(cmbDistrito), Valor(cmbSector), _uid, _local)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "PREDETERMINADOS", Modulo.TituloModulo)
            End Try
        End Sub

        ' ---- Mostrar ----

        Private Sub PintarFoto(imagen As Image)
            Dim anterior = picFoto.Image
            picFoto.Image = imagen
            anterior?.Dispose()
        End Sub

        Private Sub PintarContactos(contactos As IEnumerable(Of ContactoPersona))
            gridContactos.Rows.Clear()
            Dim tipos = TryCast(colTipoContacto.DataSource, List(Of TipoContacto))
            For Each c In contactos
                ' Un tipo que ya no está en el catálogo rompería la celda: se muestra como el primero.
                Dim tipo = If(tipos IsNot Nothing AndAlso tipos.Any(Function(t) t.Tipo = c.Tipo), c.Tipo, 0)
                gridContactos.Rows.Add(tipo, c.Dato)
            Next
        End Sub

        Private Sub PintarRepresentantes()
            gridRepresentantes.Rows.Clear()
            For Each r In _representantes
                Dim i = gridRepresentantes.Rows.Add(r.IdRep, r.Nombre, r.Personeria, r.Fecha.ToShortDateString())
                gridRepresentantes.Rows(i).DefaultCellStyle.ForeColor = If(r.Borrar, Tema.Peligro, Tema.Texto)
            Next
        End Sub

        Private Sub PintarCodigos()
            gridCodigos.Rows.Clear()
            For Each c In _codigos
                gridCodigos.Rows.Add(c.Codigo, c.Tipo)
            Next
            ValidarCodigos()
        End Sub

        Private Async Function LimpiarAsync() As Task
            _version += 1
            Dim version = _version
            Dim ubicacion = Await UbicacionPredeterminadaAsync()
            If version <> _version Then Return
            _persona = Nothing
            _pintando = True
            txtId.Clear()
            txtApellido1.Clear()
            txtApellido2.Clear()
            txtNombre.Clear()
            txtAlias.Clear()
            txtPuesto.Clear()
            txtCompania.Clear()
            txtDireccion.Clear()
            txtDefuncion.Clear()
            dtpNacimiento.Value = Date.Today
            optActivo.Checked = True
            Seleccionar(cmbReside, "CR")
            Seleccionar(cmbNacionalidad, "CR")
            cmbEstadoCivil.SelectedIndex = If(cmbEstadoCivil.Items.Count > 0, 0, -1)
            PintarUbicacion(ubicacion)
            _pintando = False
            FijarTipo(0)
            FijarGenero("0")
            MostrarDefuncion(False)
            PintarFoto(Nothing)
            PintarContactos(Array.Empty(Of ContactoPersona)())
            _representantes = New List(Of RepresentanteLegal)
            _codigos = New List(Of CodigoPersona)
            PintarRepresentantes()
            PintarCodigos()
            LimpiarRepresentante()
        End Function

        Private Sub MostrarDefuncion(visible As Boolean)
            lblDefuncion.Visible = visible
            txtDefuncion.Visible = visible
        End Sub

        ''' <summary>Devuelve False si la persona no existe. Una carga más nueva descarta la anterior.</summary>
        Private Async Function CargarPersonaAsync(id As String) As Task(Of Boolean)
            _version += 1
            Dim version = _version
            Dim p = Await _repo.ObtenerAsync(id)
            If p Is Nothing Then Return False
            Dim contactos = Await _repo.ContactosAsync(id)
            Dim representantes = Await _repo.RepresentantesAsync(id)
            Dim codigos = Await _repo.CodigosAsync(id, _etiquetaAsociado)
            Dim foto = Await _repo.ObtenerImagenAsync(id, TipoImagen.Fotografia)
            Dim ubicacion = Await LeerUbicacionAsync(p.Prov, p.Cant, p.Dist, p.Caserio, False)
            If version <> _version Then Return True

            _persona = p
            _representantes = representantes
            _codigos = codigos
            _pintando = True
            txtId.Text = p.Id
            txtApellido1.Text = p.Apellido1
            txtApellido2.Text = p.Apellido2
            txtNombre.Text = p.Nombre
            txtAlias.Text = p.Alias
            txtPuesto.Text = p.Puesto
            txtCompania.Text = p.Cia
            txtDireccion.Text = p.Dir
            dtpNacimiento.Value = p.FNac
            optActivo.Checked = p.Activo = "1"
            optInactivo.Checked = p.Activo <> "1"
            Seleccionar(cmbReside, p.Pres)
            Seleccionar(cmbNacionalidad, p.Nac)
            Seleccionar(cmbEstadoCivil, p.EstCiv)
            PintarUbicacion(ubicacion)
            _pintando = False
            FijarTipo(CInt(Core.Vb6.Val(p.Tipo)))
            FijarGenero(p.Genero)
            txtDefuncion.Text = If(p.Fallecido, p.FDef.ToShortDateString(), "")
            MostrarDefuncion(p.Fallecido)
            PintarFoto(FrmImagenes.CrearImagen(foto))
            PintarContactos(contactos)
            PintarRepresentantes()
            PintarCodigos()
            LimpiarRepresentante()
            tabs.SelectedIndex = 0
            Editable()

            If ubicacion.Sector.Length = 0 Then
                MessageBox.Show(Me, "Uno o más datos de la localización no son válidos." & vbCrLf & "El sistema cargará los valores predeterminados." & vbCrLf &
                                "Para verificarlos, use la pestaña de ubicación.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Dim predeterminada = Await UbicacionPredeterminadaAsync()
                If version = _version Then PintarUbicacion(predeterminada)
            End If
            Return True
        End Function

        ''' <summary>Equivale a SD_Validar: qué se puede tocar según el modo de la pantalla.</summary>
        Private Sub Editable()
            Dim activo = _modo <> ModoPantalla.Normal
            Dim editando = _modo = ModoPantalla.Editar
            cmbTipoId.Enabled = _modo = ModoPantalla.Nuevo
            txtId.ReadOnly = _modo <> ModoPantalla.Nuevo
            For Each t In {txtApellido1, txtApellido2, txtNombre, txtAlias, txtPuesto, txtCompania, txtDireccion, txtRepPersoneria, txtRepId}
                t.ReadOnly = Not activo
            Next
            For Each c As Control In New Control() {cmbGenero, dtpNacimiento, cmbEstadoCivil, cmbReside, cmbNacionalidad, optActivo, optInactivo,
                                                    cmbProvincia, cmbCanton, cmbDistrito, cmbSector, btnPredeterminados, dtpRepFecha,
                                                    btnBuscarRep, btnAgregarRep, btnRepArriba, btnRepAbajo, btnRepBorrar}
                c.Enabled = activo
            Next
            gridContactos.ReadOnly = Not activo
            gridContactos.AllowUserToAddRows = activo
            gridContactos.AllowUserToDeleteRows = activo
            btnAgregarFoto.Enabled = editando
            btnQuitarFoto.Enabled = editando AndAlso picFoto.Image IsNot Nothing
            btnImagenes.Enabled = _persona IsNot Nothing
            lstPersonas.Enabled = Not activo
            txtFiltro.Enabled = Not activo
            ValidarCodigos()
        End Sub

        ''' <summary>Los códigos se graban al momento: solo con la persona ya guardada y viva.</summary>
        Private Sub ValidarCodigos()
            Dim editando = _modo = ModoPantalla.Editar AndAlso _persona IsNot Nothing
            btnAgregarCodigo.Enabled = editando AndAlso Not _persona.Fallecido
            Dim actual = CodigoActual()
            btnEliminarCodigo.Enabled = editando AndAlso actual IsNot Nothing AndAlso actual.Eliminable
        End Sub

        Private Function CodigoActual() As CodigoPersona
            Dim fila = gridCodigos.CurrentRow
            Return If(fila Is Nothing OrElse fila.Index >= _codigos.Count, Nothing, _codigos(fila.Index))
        End Function

        ' ---- Identificación ----

        Private ReadOnly Property IdActual As String
            Get
                Return txtId.Text.Replace("-", "").Trim()
            End Get
        End Property

        Private Sub Id_Tecla(sender As Object, e As KeyPressEventArgs)
            If _modo = ModoPantalla.Nuevo AndAlso TipoActual IsNot Nothing AndAlso TipoActual.SoloDigitos AndAlso Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) Then e.Handled = True
        End Sub

        ''' <summary>Al digitar una identificación que ya existe, pasa a editarla; si no, la busca en el padrón.</summary>
        Private Async Sub Id_Salir(sender As Object, e As EventArgs)
            If _modo <> ModoPantalla.Nuevo OrElse IdActual.Length = 0 Then Return
            Try
                Dim id = IdActual
                If Await _repo.ExisteAsync(id) Then
                    _modo = ModoPantalla.Editar
                    Await CargarPersonaAsync(id)
                    Return
                End If
                Dim padron = Await _repo.BuscarEnPadronAsync(id)
                If padron Is Nothing OrElse _modo <> ModoPantalla.Nuevo OrElse IdActual <> id Then Return
                FijarTipo(0)
                txtApellido1.Text = padron.Apellido1
                txtApellido2.Text = padron.Apellido2
                txtNombre.Text = padron.Nombre
                FijarGenero(padron.Genero)
                dtpNacimiento.Value = padron.FNac
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ' ---- Barra ----

        Private Function PersonaDigitada() As Persona
            Return New Persona With {
                .Id = IdActual, .Tipo = If(TipoActual Is Nothing, "0", TipoActual.Tipo.ToString()), .Nombre = txtNombre.Text, .Apellido1 = txtApellido1.Text, .Apellido2 = txtApellido2.Text,
                .Alias = txtAlias.Text, .Genero = GeneroActual(), .FNac = dtpNacimiento.Value.Date, .Prov = Valor(cmbProvincia), .Cant = Valor(cmbCanton), .Dist = Valor(cmbDistrito),
                .Caserio = Valor(cmbSector), .Dir = txtDireccion.Text, .Puesto = txtPuesto.Text, .Cia = txtCompania.Text, .Pres = Valor(cmbReside), .Nac = Valor(cmbNacionalidad),
                .EstCiv = Valor(cmbEstadoCivil), .Categoria = If(_persona Is Nothing, "300", _persona.Categoria), .Activo = If(optActivo.Checked, "1", "0")
            }
        End Function

        Private Function ContactosDigitados() As List(Of ContactoPersona)
            gridContactos.EndEdit()
            Return gridContactos.Rows.Cast(Of DataGridViewRow)().
                Where(Function(r) Not r.IsNewRow).
                Select(Function(r) New ContactoPersona With {
                    .Tipo = If(r.Cells(colTipoContacto.Index).Value Is Nothing, 0, CInt(r.Cells(colTipoContacto.Index).Value)),
                    .Dato = If(r.Cells(colDatoContacto.Index).Value?.ToString(), "")
                }).ToList()
        End Function

        Private Function DatosValidos() As Boolean
            Dim aviso = DatosPersonalesRepositorio.ValidarIdentificacion(IdActual, TipoActual)
            If aviso.Length > 0 Then
                MessageBox.Show(Me, aviso, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Return False
            End If
            If txtNombre.Text.Trim().Length = 0 Then
                MessageBox.Show(Me, "Los datos requeridos son el número de identificación y el nombre. Verifíquelos antes de guardar.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtNombre.Focus()
                Return False
            End If
            Return True
        End Function

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        _modo = ModoPantalla.Nuevo
                        Await LimpiarAsync()
                        lstPersonas.ClearSelected()
                        Editable()
                        tabs.SelectedIndex = 0
                        txtId.Focus()
                    Case BotonBarra.Editar
                        If _persona Is Nothing Then Return True
                        _modo = ModoPantalla.Editar
                        Editable()
                        txtNombre.Focus()
                    Case BotonBarra.Guardar
                        If Not DatosValidos() Then Return True
                        Dim persona = PersonaDigitada()
                        Await _repo.GuardarAsync(persona, ContactosDigitados(), _representantes, _uid, _local)
                        _modo = ModoPantalla.Normal
                        Await CargarPersonaAsync(persona.Id)
                        Await ListarAsync()
                        Editable()
                    Case BotonBarra.Cancelar
                        _modo = ModoPantalla.Normal
                        Await LimpiarAsync()
                        lstPersonas.ClearSelected()
                        Editable()
                    Case BotonBarra.Buscar
                        Using datos = Await _repo.BuscarAsync()
                            Using buscador As New FrmBuscador("Búsqueda de personas", datos)
                                If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(1).Length > 0 Then
                                    Await CargarPersonaAsync(buscador.Resultado(1))
                                End If
                            End Using
                        End Using
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "DATOS PERSONALES", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ' ---- Fotografía e imágenes ----

        Private Async Sub AgregarFoto_Clic(sender As Object, e As EventArgs)
            Using dialogo As New OpenFileDialog With {.Title = "Seleccione la fotografía", .Filter = "Archivos de imágenes (.jpg)|*.jpg"}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                Dim contenido = IO.File.ReadAllBytes(dialogo.FileName)
                If contenido.Length >= TamanoMaximoFoto Then
                    MessageBox.Show(Me, "El tamaño del archivo excede los 256 kb permitidos.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Dim imagen = FrmImagenes.CrearImagen(contenido)
                If imagen Is Nothing Then
                    MessageBox.Show(Me, "El archivo no es una imagen válida.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Try
                    Await _repo.GuardarImagenAsync(_persona.Id, TipoImagen.Fotografia, contenido)
                    PintarFoto(imagen)
                    Editable()
                Catch ex As MySqlException
                    imagen.Dispose()
                    Mensajes.NoGuardo(ex.Message, "FOTOGRAFIA", Modulo.TituloModulo)
                End Try
            End Using
        End Sub

        Private Async Sub QuitarFoto_Clic(sender As Object, e As EventArgs)
            If MessageBox.Show(Me, "¿Está seguro de que desea eliminar la fotografía?", Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Try
                Await _repo.BorrarImagenAsync(_persona.Id, TipoImagen.Fotografia)
                PintarFoto(Nothing)
                Editable()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "FOTOGRAFIA", Modulo.TituloModulo)
            End Try
        End Sub

        Private Async Sub Imagenes_Clic(sender As Object, e As EventArgs)
            If _persona Is Nothing Then Return
            Using ventana As New FrmImagenes(_repo, _persona.Id, $"{_persona.Nombre} {_persona.Apellido1}".Trim(), _puedeImagenes)
                Tema.Aplicar(ventana, Modulo.TituloModulo)
                ventana.ShowDialog(Me)
            End Using
            Try
                ' La fotografía también se cambia desde esa ventana.
                PintarFoto(FrmImagenes.CrearImagen(Await _repo.ObtenerImagenAsync(_persona.Id, TipoImagen.Fotografia)))
                Editable()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ' ---- Representantes ----

        Private Sub LimpiarRepresentante()
            txtRepId.Clear()
            txtRepPersoneria.Clear()
            lblRepNombre.Text = ""
            dtpRepFecha.Value = Date.Today
        End Sub

        Private Async Function MostrarNombreRepresentanteAsync() As Task(Of String)
            Dim id = txtRepId.Text.Trim()
            Dim nombre = If(id.Length = 0, "", If(Await _repo.NombreAsync(id), ""))
            lblRepNombre.ForeColor = If(nombre.Length = 0, Tema.Peligro, Tema.Acento)
            lblRepNombre.Text = If(nombre.Length > 0, nombre, If(id.Length = 0, "", "Identificación no encontrada"))
            Return nombre
        End Function

        Private Async Sub RepId_Salir(sender As Object, e As EventArgs)
            Try
                Await MostrarNombreRepresentanteAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub BuscarRep_Clic(sender As Object, e As EventArgs)
            Try
                Using datos = Await _repo.BuscarRepresentantesAsync()
                    Using buscador As New FrmBuscador("Seleccione a un representante legal", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(1).Length > 0 Then
                            txtRepId.Text = buscador.Resultado(1)
                            Await MostrarNombreRepresentanteAsync()
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>Si el representante ya está en la lista, actualiza su personería.</summary>
        Private Async Sub AgregarRep_Clic(sender As Object, e As EventArgs)
            Try
                Dim nombre = Await MostrarNombreRepresentanteAsync()
                If nombre.Length = 0 OrElse txtRepPersoneria.Text.Trim().Length = 0 Then
                    MessageBox.Show(Me, "Indique una identificación válida y un detalle de la personería para agregar al representante.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    Return
                End If
                Dim id = txtRepId.Text.Trim()
                Dim representante = _representantes.FirstOrDefault(Function(r) r.IdRep = id)
                If representante Is Nothing Then
                    representante = New RepresentanteLegal With {.IdRep = id}
                    _representantes.Add(representante)
                End If
                representante.Nombre = nombre
                representante.Personeria = txtRepPersoneria.Text.Trim()
                representante.Fecha = dtpRepFecha.Value.Date
                representante.Borrar = False
                PintarRepresentantes()
                LimpiarRepresentante()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Representante_Seleccionado(sender As Object, e As EventArgs)
            Dim fila = gridRepresentantes.CurrentRow
            If fila Is Nothing OrElse fila.Index >= _representantes.Count OrElse _modo = ModoPantalla.Normal Then Return
            Dim r = _representantes(fila.Index)
            txtRepId.Text = r.IdRep
            lblRepNombre.Text = r.Nombre
            txtRepPersoneria.Text = r.Personeria
            If r.Fecha >= dtpRepFecha.MinDate Then dtpRepFecha.Value = r.Fecha
        End Sub

        Private Sub MoverRepresentante(desplazamiento As Integer)
            Dim fila = gridRepresentantes.CurrentRow
            If fila Is Nothing Then Return
            Dim origen = fila.Index
            Dim destino = origen + desplazamiento
            If destino < 0 OrElse destino >= _representantes.Count Then Return
            Dim r = _representantes(origen)
            _representantes.RemoveAt(origen)
            _representantes.Insert(destino, r)
            PintarRepresentantes()
            gridRepresentantes.CurrentCell = gridRepresentantes.Rows(destino).Cells(0)
        End Sub

        ''' <summary>Marca o desmarca: se borra al guardar, igual que en VB6.</summary>
        Private Sub BorrarRep_Clic(sender As Object, e As EventArgs)
            Dim fila = gridRepresentantes.CurrentRow
            If fila Is Nothing Then Return
            Dim indice = fila.Index
            _representantes(indice).Borrar = Not _representantes(indice).Borrar
            PintarRepresentantes()
            gridRepresentantes.CurrentCell = gridRepresentantes.Rows(indice).Cells(0)
        End Sub

        ' ---- Códigos ----

        Private Async Sub AgregarCodigo_Clic(sender As Object, e As EventArgs)
            Dim actual = CodigoActual()
            Using ventana As New FrmCodigoPersona(_repo, If(actual IsNot Nothing AndAlso actual.Eliminable, actual.Codigo, ""), _puedeCalcular)
                Tema.Aplicar(ventana, Modulo.TituloModulo)
                If ventana.ShowDialog(Me) <> DialogResult.OK Then Return
                Try
                    Await _repo.AgregarCodigoAsync(_persona.Id, ventana.Codigo, _uid, _local)
                    _codigos = Await _repo.CodigosAsync(_persona.Id, _etiquetaAsociado)
                    PintarCodigos()
                Catch ex As MySqlException
                    Mensajes.NoGuardo(ex.Message, "CODIGOS", Modulo.TituloModulo)
                End Try
            End Using
        End Sub

        Private Async Sub EliminarCodigo_Clic(sender As Object, e As EventArgs)
            Dim actual = CodigoActual()
            If actual Is Nothing OrElse Not actual.Eliminable Then Return
            If MessageBox.Show(Me, $"¿Está seguro de que desea eliminar el código '{actual.Codigo}' de {_persona.NombreCompleto}?", Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Try
                Await _repo.EliminarCodigoAsync(_persona.Id, actual.Codigo, _local)
                _codigos = Await _repo.CodigosAsync(_persona.Id, _etiquetaAsociado)
                PintarCodigos()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CODIGOS", Modulo.TituloModulo)
            End Try
        End Sub

    End Class

End Namespace
