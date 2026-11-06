Imports MySqlConnector
Imports CoopeNet.Core.Catalogos
Imports CoopeNet.Ui

Namespace SD0001

    ''' <summary>
    ''' Fallecidos. Migración de Fuentes\SD0001\FrmFallecido.frm. La casilla admite códigos, igual que el
    ''' SDT con BuscarID. Registrar inactiva a la persona y anular la activa; ambos se replican como DP.
    ''' </summary>
    Public Class FrmFallecido
        Inherits Form

        Private Const LargoMinimoClave As Integer = 2
        Private Const NoRegistrada As String = "NO REGISTRADA"

        Private ReadOnly _personas As DatosPersonalesRepositorio
        Private ReadOnly _codigos As CodigosRepositorio
        Private ReadOnly _local As Long
        Private _persona As Persona
        Private _estadosCiviles As New List(Of ItemCatalogo)

        Private ReadOnly txtId As New TextBox With {.Name = "txtId", .AccessibleName = "Identificación", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(112, 14), .Width = 230}
        Private ReadOnly btnBuscar As New Button With {.Name = "btnBuscar", .Text = "Buscar", .Location = New Point(350, 12), .Size = New Size(90, 28)}
        Private ReadOnly txtFicha As New TextBox With {.Name = "txtFicha", .AccessibleName = "Datos de la persona", .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Vertical, .Location = New Point(12, 50), .Size = New Size(428, 240)}
        Private ReadOnly dtpDefuncion As New DateTimePicker With {.Name = "dtpDefuncion", .AccessibleName = "Fecha de defunción", .Format = DateTimePickerFormat.Short, .Location = New Point(150, 302), .Width = 130}
        Private ReadOnly btnAplicar As New Button With {.Name = "btnAplicar", .Text = "Registrar fallecimiento", .Location = New Point(12, 338), .Size = New Size(190, 30)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "Salir", .DialogResult = DialogResult.Cancel, .Location = New Point(350, 338), .Size = New Size(90, 30)}

        Public Sub New(personas As DatosPersonalesRepositorio, codigos As CodigosRepositorio, local As Long)
            _personas = personas
            _codigos = codigos
            _local = local
            Text = "Cambio de estado en datos personales"
            ClientSize = New Size(452, 380)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir
            Controls.AddRange(New Control() {
                New Label With {.Text = "Identificación", .AutoSize = True, .Location = New Point(12, 17)}, txtId, btnBuscar, txtFicha,
                New Label With {.Text = "Fecha de defunción", .AutoSize = True, .Location = New Point(12, 305)}, dtpDefuncion,
                btnAplicar, btnSalir
            })
            AddHandler Load, AddressOf Cargar
            AddHandler txtId.KeyDown, AddressOf Id_Tecla
            AddHandler txtId.TextChanged, Sub() If _persona IsNot Nothing AndAlso Not String.Equals(txtId.Text, _persona.Id, StringComparison.OrdinalIgnoreCase) Then Mostrar(Nothing, "")
            AddHandler btnBuscar.Click, AddressOf Buscar_Clic
            AddHandler btnAplicar.Click, AddressOf Aplicar_Clic
            Mostrar(Nothing, "")
        End Sub

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                _estadosCiviles = Await _personas.EstadosCivilesAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Mostrar(persona As Persona, estadoCivil As String)
            _persona = persona
            Dim fallecido = persona IsNot Nothing AndAlso persona.Fallecido
            txtFicha.Text = If(persona Is Nothing, NoRegistrada, DatosPersonalesRepositorio.FichaFallecido(persona, estadoCivil))
            ' Una fecha futura ya guardada no debe romper el control.
            dtpDefuncion.MaxDate = If(fallecido AndAlso persona.FDef > Date.Today, persona.FDef, Date.Today)
            dtpDefuncion.Value = If(fallecido, persona.FDef, Date.Today)
            dtpDefuncion.Enabled = persona IsNot Nothing AndAlso Not fallecido
            btnAplicar.Enabled = persona IsNot Nothing
            btnAplicar.Text = If(fallecido, "Anular fallecimiento", "Registrar fallecimiento")
        End Sub

        Private Async Function CargarPersonaAsync(id As String) As Task
            Dim persona = Await _personas.ObtenerAsync(id)
            If persona Is Nothing Then
                Mostrar(Nothing, "")
                Return
            End If
            Dim estado = _estadosCiviles.Find(Function(c) c.Codigo = persona.EstCiv)
            txtId.Text = persona.Id
            Mostrar(persona, If(estado Is Nothing, "", estado.Descripcion.ToUpperInvariant()))
        End Function

        Private Async Sub Id_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter Then Return
            e.SuppressKeyPress = True
            Dim clave = txtId.Text.Trim()
            If clave.Length < LargoMinimoClave Then Return
            Try
                Using datos = Await _codigos.CoincidenciasAsync(clave)
                    Dim id = FrmAdminCodigos.Elegir(Me, datos)
                    If id.Length > 0 Then
                        Await CargarPersonaAsync(id)
                    ElseIf datos.Rows.Count = 0 Then
                        Mostrar(Nothing, "")
                    End If
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Buscar_Clic(sender As Object, e As EventArgs)
            Try
                Using datos = Await _personas.BuscarFisicasAsync()
                    Using buscador As New FrmBuscador("Seleccione a la persona", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(1).Length > 0 Then
                            Await CargarPersonaAsync(buscador.Resultado(1))
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Aplicar_Clic(sender As Object, e As EventArgs)
            If _persona Is Nothing Then Return
            Dim nombre = _persona.NombreCompleto
            Dim anular = _persona.Fallecido
            Dim pregunta = $"¿Desea cambiar el estado a {If(anular, "no fallecido", "fallecido")} para {nombre}?"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Try
                If anular Then
                    Await _personas.AnularFallecimientoAsync(_persona.Id, _local)
                Else
                    Await _personas.RegistrarFallecimientoAsync(_persona.Id, dtpDefuncion.Value.Date, _local)
                End If
                MessageBox.Show(Me, $"{nombre} ha sido marcado como {If(anular, "no fallecido", "fallecido")}.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Await CargarPersonaAsync(_persona.Id)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "FALLECIDOS", Modulo.TituloModulo)
            End Try
        End Sub

    End Class

End Namespace
