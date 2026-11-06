Imports System.Data
Imports MySqlConnector
Imports CoopeNet.Core.Catalogos
Imports CoopeNet.Ui

Namespace SD0001

    ''' <summary>
    ''' Administración de Códigos. Migración de Fuentes\SD0001\FrmAdminCodigos.frm. No incluye a la persona
    ''' que no existe: VB6 la agrega desde el padrón con un INSERT por posición, y aquí se remite a Datos
    ''' Personales. A una persona fallecida o inactiva no se le agregan códigos, pero sí se le eliminan.
    ''' </summary>
    Public Class FrmAdminCodigos
        Inherits Form

        Private Const LargoMinimoClave As Integer = 2

        Private ReadOnly _personas As DatosPersonalesRepositorio
        Private ReadOnly _repo As CodigosRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _puedeCalcular As Boolean
        Private ReadOnly _abrirTipos As Func(Of Form)
        Private _etiquetaAsociado As String = "Cliente"
        Private _tiposContacto As New List(Of TipoContacto)
        Private _id As String = ""
        Private _bloqueado As Boolean
        Private _codigos As New List(Of CodigoPersona)

        Private ReadOnly txtId As New TextBox With {.Name = "txtId", .AccessibleName = "Identificación o código", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(100, 14), .Width = 220}
        Private ReadOnly btnBuscar As New Button With {.Name = "btnBuscar", .Text = "Buscar", .Location = New Point(328, 12), .Size = New Size(90, 28)}
        Private ReadOnly txtNombre As New TextBox With {.Name = "txtNombre", .AccessibleName = "Nombre", .ReadOnly = True, .TabStop = False, .Location = New Point(100, 46), .Width = 560}
        Private ReadOnly gridCodigos As New DataGridView With {.Name = "gridCodigos", .AccessibleName = "Códigos asignados", .Location = New Point(12, 80), .Size = New Size(320, 280),
            .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .ReadOnly = True, .MultiSelect = False,
            .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect}
        Private ReadOnly txtDetalle As New TextBox With {.Name = "txtDetalle", .AccessibleName = "Detalle", .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Both, .WordWrap = False, .Location = New Point(342, 80), .Size = New Size(318, 280)}
        Private ReadOnly btnAgregar As New Button With {.Name = "btnAgregar", .Text = "Agregar código", .Enabled = False, .Location = New Point(12, 370), .Size = New Size(138, 30)}
        Private ReadOnly btnEliminar As New Button With {.Name = "btnEliminar", .Text = "Eliminar código", .Enabled = False, .Location = New Point(156, 370), .Size = New Size(138, 30)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "Salir", .DialogResult = DialogResult.Cancel, .Location = New Point(570, 370), .Size = New Size(90, 30)}

        Public Sub New(personas As DatosPersonalesRepositorio, repositorio As CodigosRepositorio, uid As Long, local As Long, puedeCalcular As Boolean, abrirTipos As Func(Of Form))
            _personas = personas
            _repo = repositorio
            _uid = uid
            _local = local
            _puedeCalcular = puedeCalcular
            _abrirTipos = abrirTipos
            Text = "Administración de códigos"
            ClientSize = New Size(672, 412)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir
            gridCodigos.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colCodigo", .HeaderText = "Código", .Width = 150})
            gridCodigos.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colTipo", .HeaderText = "Tipo", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            Controls.AddRange(New Control() {
                New Label With {.Text = "Identificación", .AutoSize = True, .Location = New Point(12, 17)}, txtId, btnBuscar,
                New Label With {.Text = "Nombre", .AutoSize = True, .Location = New Point(12, 49)}, txtNombre,
                gridCodigos, txtDetalle, btnAgregar, btnEliminar, btnSalir
            })
            AddHandler Load, AddressOf Cargar
            AddHandler Shown, Sub() txtDetalle.Font = New Font(FontFamily.GenericMonospace, 8.25F)
            AddHandler txtId.KeyDown, AddressOf Id_Tecla
            AddHandler txtId.TextChanged, Sub() If Not String.Equals(txtId.Text, _id, StringComparison.OrdinalIgnoreCase) Then Limpiar()
            AddHandler btnBuscar.Click, AddressOf Buscar_Clic
            AddHandler gridCodigos.SelectionChanged, Sub() Validar()
            AddHandler btnAgregar.Click, AddressOf Agregar_Clic
            AddHandler btnEliminar.Click, AddressOf Eliminar_Clic
        End Sub

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                _etiquetaAsociado = Await _personas.EtiquetaAsociadoAsync(_local)
                _tiposContacto = Await _personas.TiposContactoAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Limpiar()
            _id = ""
            _bloqueado = False
            _codigos = New List(Of CodigoPersona)
            txtNombre.Clear()
            txtDetalle.Clear()
            gridCodigos.Rows.Clear()
            Validar()
        End Sub

        Private Function CodigoActual() As CodigoPersona
            Dim fila = gridCodigos.CurrentRow
            Return If(fila Is Nothing OrElse fila.Index >= _codigos.Count, Nothing, _codigos(fila.Index))
        End Function

        Private Sub Validar()
            Dim actual = CodigoActual()
            btnAgregar.Enabled = _id.Length > 0 AndAlso Not _bloqueado
            btnEliminar.Enabled = actual IsNot Nothing AndAlso actual.Eliminable
        End Sub

        Private Async Function MostrarPersonaAsync(id As String) As Task
            Dim persona = Await _personas.ObtenerAsync(id)
            Dim detalle As New List(Of String)
            Dim asociado = Await _repo.CodigoAsociadoAsync(id)
            If asociado.Length > 0 Then detalle.Add(CodigosRepositorio.LineaDetalle(_etiquetaAsociado, asociado))
            Dim inactivo = persona Is Nothing OrElse persona.Activo <> "1"
            If inactivo Then detalle.Add(CodigosRepositorio.LineaDetalle("Estado", "INACTIVO"))
            Dim fallecido = persona IsNot Nothing AndAlso persona.Fallecido
            If fallecido Then detalle.Add(CodigosRepositorio.LineaDetalle("Fallecido", CodigosRepositorio.FechaDetalle(persona.FDef)))
            For Each contacto In Await _personas.ContactosAsync(id)
                Dim tipo = _tiposContacto.Find(Function(t) t.Tipo = contacto.Tipo)
                detalle.Add(CodigosRepositorio.LineaDetalle(If(tipo Is Nothing, "", tipo.Descripcion), contacto.Dato))
            Next
            Dim codigos = Await _personas.CodigosAsync(id, _etiquetaAsociado)

            _id = id
            txtId.Text = id
            _bloqueado = inactivo OrElse fallecido
            txtNombre.Text = If(persona Is Nothing, "IDENTIFICACION NO ENCONTRADA", persona.NombreCompleto)
            txtDetalle.Lines = detalle.ToArray()
            _codigos = codigos
            gridCodigos.Rows.Clear()
            For Each c In _codigos
                gridCodigos.Rows.Add(c.Codigo, c.Tipo)
            Next
            Validar()
        End Function

        Private Async Sub Id_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter Then Return
            e.SuppressKeyPress = True
            Dim clave = txtId.Text.Trim()
            If clave.Length < LargoMinimoClave Then Return
            Try
                Using datos = Await _repo.CoincidenciasAsync(clave)
                    Dim id = Elegir(Me, datos)
                    If id.Length > 0 Then
                        Await MostrarPersonaAsync(id)
                    ElseIf datos.Rows.Count = 0 Then
                        MessageBox.Show(Me, $"No se encuentra ningún registro para '{clave}'. Para agregar la persona use Datos Personales.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>Con varias coincidencias pregunta cuál. Vacío si no hay o si el usuario cancela.</summary>
        Friend Shared Function Elegir(dueno As IWin32Window, datos As DataTable) As String
            If datos.Rows.Count = 0 Then Return ""
            If datos.Rows.Count = 1 Then Return datos.Rows(0)(0).ToString()
            Using buscador As New FrmBuscador("Seleccione la identificación", datos)
                Return If(buscador.ShowDialog(dueno) = DialogResult.OK, buscador.Resultado(0), "")
            End Using
        End Function

        Private Async Sub Buscar_Clic(sender As Object, e As EventArgs)
            Try
                Using datos = Await _personas.BuscarAsync()
                    Using buscador As New FrmBuscador("Seleccione la identificación", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(1).Length > 0 Then
                            Await MostrarPersonaAsync(buscador.Resultado(1))
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Agregar_Clic(sender As Object, e As EventArgs)
            Dim actual = CodigoActual()
            If actual IsNot Nothing AndAlso Not actual.Eliminable Then
                Dim pregunta = $"El código de {actual.Tipo} no se puede cambiar desde el administrador de códigos.{Environment.NewLine}¿Desea agregar un código nuevo a {txtNombre.Text}?"
                If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            End If
            Using ventana As New FrmCodigoPersona(_personas, If(actual IsNot Nothing AndAlso actual.Eliminable, actual.Codigo, ""), _puedeCalcular, _abrirTipos)
                Tema.Aplicar(ventana, Modulo.TituloModulo)
                If ventana.ShowDialog(Me) <> DialogResult.OK Then Return
                Try
                    Await _repo.AgregarCodigoAsync(_id, ventana.Codigo, _uid, _local)
                    Await MostrarPersonaAsync(_id)
                Catch ex As MySqlException
                    Mensajes.NoGuardo(ex.Message, "CODIGOS", Modulo.TituloModulo)
                End Try
            End Using
        End Sub

        Private Async Sub Eliminar_Clic(sender As Object, e As EventArgs)
            Dim actual = CodigoActual()
            If actual Is Nothing OrElse Not actual.Eliminable Then Return
            If MessageBox.Show(Me, $"¿Está seguro de que desea eliminar el código '{actual.Codigo}' de {txtNombre.Text}?", Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Try
                Await _repo.EliminarCodigoAsync(_id, actual.Codigo, _local)
                Await MostrarPersonaAsync(_id)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CODIGOS", Modulo.TituloModulo)
            End Try
        End Sub

    End Class

End Namespace
