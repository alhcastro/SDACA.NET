Imports System.Data.Common
Imports MySqlConnector
Imports CoopeNet.Core.Catalogos
Imports CoopeNet.Ui

Namespace SD0001

    ''' <summary>
    ''' Cambio de Identificación. Migración de Fuentes\SD0001\FrmCambioID.frm: asistente de cuatro pasos.
    ''' No migra el modo de eventos, que solo usa ClsDP. A diferencia de VB6 no deja volver después de
    ''' ejecutar, rechaza una identificación igual a la original y avisa si la nueva ya existe.
    ''' </summary>
    Public Class FrmCambioId
        Inherits Form

        Private Const LargoIdentificacion As Integer = 16
        Private Const SinOriginal As String = "Seleccione una identificación existente en datos personales para poder continuar..."
        Private Const IdNueva As String = "Identificación nueva..."
        Private Const AvisoExistente As String = "ATENCIÓN: la identificación nueva ya existe. Las filas que choquen con una llave, como la de datos personales, se quedarán con la identificación original."

        Private ReadOnly _personas As DatosPersonalesRepositorio
        Private ReadOnly _repo As CambioIdentificacionRepositorio
        Private ReadOnly _local As Long
        Private _paso As Integer = 1
        Private _original As Persona
        Private _nuevaExiste As Boolean
        Private _ejecutando As Boolean

        Private ReadOnly gbPaso1 As New GroupBox With {.Name = "gbPaso1", .Text = "Paso #1", .Location = New Point(12, 10), .Size = New Size(560, 280)}
        Private ReadOnly txtOriginal As New TextBox With {.Name = "txtOriginal", .AccessibleName = "Identificación original", .MaxLength = LargoIdentificacion, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(16, 60), .Width = 240}
        Private ReadOnly btnBuscarOriginal As New Button With {.Name = "btnBuscarOriginal", .Text = "Buscar", .Location = New Point(264, 58), .Size = New Size(90, 28)}
        Private ReadOnly lblOriginal As New Label With {.Name = "lblOriginal", .Text = SinOriginal, .Location = New Point(16, 100), .Size = New Size(528, 70)}

        Private ReadOnly gbPaso2 As New GroupBox With {.Name = "gbPaso2", .Text = "Paso #2", .Location = New Point(12, 10), .Size = New Size(560, 280), .Visible = False}
        Private ReadOnly rbExistente As New RadioButton With {.Name = "rbExistente", .Text = "Ya existe en datos personales", .AutoSize = True, .Location = New Point(16, 56)}
        Private ReadOnly rbDigitada As New RadioButton With {.Name = "rbDigitada", .Text = "Es de persona jurídica, extranjero o es un número de consecutivo interno", .AutoSize = True, .Checked = True, .Location = New Point(16, 84)}
        Private ReadOnly btnBuscarNueva As New Button With {.Name = "btnBuscarNueva", .Text = "Buscar", .Enabled = False, .Location = New Point(264, 134), .Size = New Size(90, 28)}
        Private ReadOnly txtNueva As New TextBox With {.Name = "txtNueva", .AccessibleName = "Nuevo número de identificación", .MaxLength = LargoIdentificacion, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(16, 136), .Width = 240}

        Private ReadOnly gbPaso3 As New GroupBox With {.Name = "gbPaso3", .Text = "Paso #3", .Location = New Point(12, 10), .Size = New Size(560, 280), .Visible = False}
        Private ReadOnly lblOrigen As New Label With {.Name = "lblOrigen", .Location = New Point(16, 56), .Size = New Size(260, 110)}
        Private ReadOnly lblDestino As New Label With {.Name = "lblDestino", .Location = New Point(284, 56), .Size = New Size(260, 110)}
        Private ReadOnly lblAviso As New Label With {.Name = "lblAviso", .ForeColor = Color.Firebrick, .Location = New Point(16, 172), .Size = New Size(528, 56)}
        Private ReadOnly chkProceder As New CheckBox With {.Name = "chkProceder", .Text = "La información aquí mostrada es correcta, proceder el cambio", .AutoSize = True, .Location = New Point(16, 240)}

        Private ReadOnly gbPaso4 As New GroupBox With {.Name = "gbPaso4", .Text = "Paso #4", .Location = New Point(12, 10), .Size = New Size(560, 280), .Visible = False}
        Private ReadOnly lstResultado As New ListBox With {.Name = "lstResultado", .AccessibleName = "Resultado por tabla", .IntegralHeight = False, .Location = New Point(16, 52), .Size = New Size(528, 176)}
        Private ReadOnly barraProgreso As New ProgressBar With {.Name = "barraProgreso", .Style = ProgressBarStyle.Marquee, .Visible = False, .Location = New Point(16, 234), .Size = New Size(528, 10)}
        Private ReadOnly lblResultado As New Label With {.Name = "lblResultado", .AutoSize = True, .Location = New Point(16, 250)}

        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "<< &Anterior", .Location = New Point(12, 300), .Size = New Size(110, 30)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "&Siguiente >>", .Location = New Point(128, 300), .Size = New Size(110, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .Location = New Point(366, 300), .Size = New Size(100, 30)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(472, 300), .Size = New Size(100, 30)}

        Public Sub New(personas As DatosPersonalesRepositorio, repositorio As CambioIdentificacionRepositorio, local As Long)
            _personas = personas
            _repo = repositorio
            _local = local
            Text = "Cambio de Identificación"
            ClientSize = New Size(584, 342)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            gbPaso1.Controls.AddRange(New Control() {
                New Label With {.Text = "Digite o seleccione la identificación que desea corregir", .AutoSize = True, .Location = New Point(16, 30)},
                txtOriginal, btnBuscarOriginal, lblOriginal,
                New Label With {.Text = "El número de identificación debe estar registrado en datos personales. Si no existe, utilice el mantenimiento de datos personales para agregarlo.", .Location = New Point(16, 190), .Size = New Size(528, 60)}
            })
            gbPaso2.Controls.AddRange(New Control() {
                New Label With {.Text = "Seleccione el origen de la nueva identificación", .AutoSize = True, .Location = New Point(16, 30)},
                rbExistente, rbDigitada, txtNueva, btnBuscarNueva,
                New Label With {.Text = "Nuevo número de identificación", .AutoSize = True, .Location = New Point(16, 108)}
            })
            gbPaso3.Controls.AddRange(New Control() {
                New Label With {.Text = "¿Confirmar cambio de identificación?", .AutoSize = True, .Location = New Point(16, 30)},
                lblOrigen, lblDestino, lblAviso, chkProceder
            })
            gbPaso4.Controls.AddRange(New Control() {
                New Label With {.Text = "Finalización del proceso de cambio de identificación", .AutoSize = True, .Location = New Point(16, 28)},
                lstResultado, barraProgreso, lblResultado
            })
            Controls.AddRange(New Control() {gbPaso1, gbPaso2, gbPaso3, gbPaso4, btnAnterior, btnSiguiente, btnCancelar, btnSalir})
            AddHandler txtOriginal.KeyDown, AddressOf Original_Tecla
            AddHandler txtOriginal.TextChanged, AddressOf Original_Cambio
            AddHandler btnBuscarOriginal.Click, AddressOf BuscarOriginal_Clic
            AddHandler rbExistente.CheckedChanged, Sub() Origen_Cambio()
            AddHandler txtNueva.TextChanged, Sub() Validar()
            AddHandler btnBuscarNueva.Click, AddressOf BuscarNueva_Clic
            AddHandler chkProceder.CheckedChanged, Sub() Validar()
            AddHandler btnAnterior.Click, Sub() IrA(_paso - 1)
            AddHandler btnSiguiente.Click, AddressOf Siguiente_Clic
            AddHandler btnCancelar.Click, Sub() Reiniciar()
            AddHandler btnSalir.Click, Sub() Close()
            AddHandler FormClosing, Sub(s, e) e.Cancel = _ejecutando
            IrA(1)
        End Sub

        Private ReadOnly Property Nueva As String
            Get
                Return txtNueva.Text.Trim()
            End Get
        End Property

        Private Sub IrA(paso As Integer)
            _paso = paso
            gbPaso1.Visible = paso = 1
            gbPaso2.Visible = paso = 2
            gbPaso3.Visible = paso = 3
            gbPaso4.Visible = paso = 4
            If paso = 3 Then chkProceder.Checked = False
            btnSiguiente.Text = If(paso = 4, "<&Finalizado>", If(paso = 3, "<&Finalizar>", "&Siguiente >>"))
            Validar()
        End Sub

        Private Sub Validar()
            Select Case _paso
                Case 1
                    btnSiguiente.Enabled = _original IsNot Nothing
                Case 2
                    btnSiguiente.Enabled = Nueva.Length > 0
                Case 3
                    btnSiguiente.Enabled = chkProceder.Checked
                Case Else
                    btnSiguiente.Enabled = Not _ejecutando
            End Select
            btnAnterior.Enabled = _paso = 2 OrElse _paso = 3
            btnCancelar.Enabled = _original IsNot Nothing AndAlso _paso < 4
            btnSalir.Enabled = Not _ejecutando
        End Sub

        Private Sub Reiniciar()
            txtOriginal.Clear()
            txtNueva.Clear()
            rbDigitada.Checked = True
            lstResultado.Items.Clear()
            lblResultado.Text = ""
            IrA(1)
            txtOriginal.Focus()
        End Sub

        Private Sub MostrarOriginal(persona As Persona)
            _original = persona
            lblOriginal.Text = If(persona Is Nothing, SinOriginal, CambioIdentificacionRepositorio.Descripcion(persona))
            Validar()
        End Sub

        Private Sub Original_Cambio(sender As Object, e As EventArgs)
            If _original IsNot Nothing AndAlso Not String.Equals(txtOriginal.Text, _original.Id, StringComparison.OrdinalIgnoreCase) Then MostrarOriginal(Nothing)
        End Sub

        Private Async Function CargarOriginalAsync(id As String) As Task
            Dim persona = Await _personas.ObtenerAsync(id)
            If persona IsNot Nothing Then txtOriginal.Text = persona.Id
            MostrarOriginal(persona)
        End Function

        Private Async Sub Original_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter Then Return
            e.SuppressKeyPress = True
            If txtOriginal.Text.Trim().Length = 0 Then Return
            Try
                Await CargarOriginalAsync(txtOriginal.Text.Trim())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Function BuscarAsync() As Task(Of String)
            Using datos = Await _personas.BuscarAsync()
                Using buscador As New FrmBuscador("Seleccione la identificación", datos)
                    Return If(buscador.ShowDialog(Me) = DialogResult.OK, buscador.Resultado(1), "")
                End Using
            End Using
        End Function

        Private Async Sub BuscarOriginal_Clic(sender As Object, e As EventArgs)
            Try
                Dim id = Await BuscarAsync()
                If id.Length > 0 Then Await CargarOriginalAsync(id)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Origen_Cambio()
            txtNueva.ReadOnly = rbExistente.Checked
            btnBuscarNueva.Enabled = rbExistente.Checked
        End Sub

        Private Async Sub BuscarNueva_Clic(sender As Object, e As EventArgs)
            Try
                Dim id = Await BuscarAsync()
                If id.Length > 0 Then txtNueva.Text = id
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>False si la identificación nueva no sirve o no se pudo consultar.</summary>
        Private Async Function PrepararConfirmacionAsync() As Task(Of Boolean)
            If String.Equals(Nueva, _original.Id, StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show(Me, "La identificación nueva debe ser distinta de la original.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return False
            End If
            Dim destino = Await _personas.ObtenerAsync(Nueva)
            _nuevaExiste = destino IsNot Nothing
            lblOrigen.Text = $"Identificación: {_original.Id}{Environment.NewLine}{CambioIdentificacionRepositorio.Descripcion(_original)}"
            lblDestino.Text = $"Identificación: {Nueva}{Environment.NewLine}{If(_nuevaExiste, CambioIdentificacionRepositorio.Descripcion(destino), IdNueva)}"
            lblAviso.Text = If(_nuevaExiste, AvisoExistente, "")
            Return True
        End Function

        Private Async Function EjecutarAsync() As Task
            _ejecutando = True
            barraProgreso.Visible = True
            lstResultado.Items.Clear()
            lblResultado.Text = ""
            IrA(4)
            Try
                Dim progreso As New Progress(Of ResultadoCambioId)(Sub(r) lstResultado.Items.Add(r.Mensaje))
                Dim resultados = Await _repo.CambiarAsync(_original.Id, Nueva, _local, progreso)
                If resultados.Count = 0 Then
                    lblResultado.Text = "No se cambió nada."
                    MessageBox.Show(Me, "No se puede obtener la información necesaria para continuar con este proceso.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Else
                    lblResultado.Text = CambioIdentificacionRepositorio.Resumen(resultados)
                End If
            Catch ex As Exception When TypeOf ex Is DbException OrElse TypeOf ex Is InvalidOperationException
                ' Las tablas son MyISAM: lo ya cambiado no se deshace.
                lblResultado.Text = "El proceso se interrumpió. Revise la lista: lo ya cambiado no se deshace."
                Mensajes.NoGuardo(ex.Message, "CAMBIO DE IDENTIFICACION", Modulo.TituloModulo)
            Finally
                _ejecutando = False
                barraProgreso.Visible = False
                Validar()
            End Try
        End Function

        Private Async Sub Siguiente_Clic(sender As Object, e As EventArgs)
            Try
                Select Case _paso
                    Case 1
                        IrA(2)
                    Case 2
                        If Await PrepararConfirmacionAsync() Then IrA(3)
                    Case 3
                        Await EjecutarAsync()
                    Case Else
                        Reiniciar()
                End Select
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

    End Class

End Namespace
