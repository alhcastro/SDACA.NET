Imports MySqlConnector
Imports SDACA.Core.Catalogos
Imports SDACA.Ui

Namespace SD0001

    ''' <summary>
    ''' Tipos de códigos. Migración de Fuentes\SD0001\FrmMantCodigos.frm. Lo abre Agregar código, igual
    ''' que en VB6: no tiene opción propia en el menú. El campo Sistema no se puede cambiar.
    ''' </summary>
    Public Class FrmTiposCodigo
        Inherits Form

        Private Const TextoSistema As String = "SISTEMA"
        Private Const TextoUsuario As String = "DEFINIDO POR EL USUARIO"
        Private Const TextoActivo As String = "ACTIVO"
        Private Const TextoInactivo As String = "INACTIVO"

        Private ReadOnly _repo As CodigosRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private _actual As TipoCodigo

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(10, 8), .Size = New Size(550, 160)}
        Private ReadOnly txtTipo As New TextBox With {.Name = "txtTipo", .AccessibleName = "Tipo", .MaxLength = 3, .CharacterCasing = CharacterCasing.Upper, .ReadOnly = True, .Location = New Point(110, 24), .Width = 80}
        Private ReadOnly txtDescripcion As New TextBox With {.Name = "txtDescripcion", .AccessibleName = "Descripción", .MaxLength = 32, .ReadOnly = True, .Location = New Point(110, 56), .Width = 340}
        Private ReadOnly txtSistema As New TextBox With {.Name = "txtSistema", .AccessibleName = "Sistema", .ReadOnly = True, .TabStop = False, .Location = New Point(110, 88), .Width = 220}
        Private ReadOnly cmbEstado As New ComboBox With {.Name = "cmbEstado", .AccessibleName = "Estado", .DropDownStyle = ComboBoxStyle.DropDownList, .Enabled = False, .Location = New Point(110, 120), .Width = 160}
        Private ReadOnly btnPrimero As New Button With {.Name = "btnPrimero", .Text = "<<", .AccessibleName = "Primero", .Location = New Point(10, 176), .Size = New Size(40, 32)}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "<", .AccessibleName = "Anterior", .Location = New Point(54, 176), .Size = New Size(40, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = ">", .AccessibleName = "Siguiente", .Location = New Point(98, 176), .Size = New Size(40, 32)}
        Private ReadOnly btnUltimo As New Button With {.Name = "btnUltimo", .Text = ">>", .AccessibleName = "Último", .Location = New Point(142, 176), .Size = New Size(40, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(190, 176), .Size = New Size(370, 32)}

        Public Sub New(repositorio As CodigosRepositorio, uid As Long, local As Long)
            _repo = repositorio
            _uid = uid
            _local = local
            Text = "Mantenimiento de tipos de códigos"
            ClientSize = New Size(570, 218)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            cmbEstado.Items.AddRange(New Object() {TextoActivo, TextoInactivo})
            gbDatos.Controls.AddRange(New Control() {
                New Label With {.Text = "Tipo", .AutoSize = True, .Location = New Point(12, 27)}, txtTipo,
                New Label With {.Text = "Descripción", .AutoSize = True, .Location = New Point(12, 59)}, txtDescripcion,
                New Label With {.Text = "Sistema", .AutoSize = True, .Location = New Point(12, 91)}, txtSistema,
                New Label With {.Text = "Estado", .AutoSize = True, .Location = New Point(12, 123)}, cmbEstado
            })
            Controls.AddRange(New Control() {gbDatos, btnPrimero, btnAnterior, btnSiguiente, btnUltimo, barra})
            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler btnPrimero.Click, Sub() Ir(IrATipo.Primero)
            AddHandler btnAnterior.Click, Sub() Ir(IrATipo.Anterior)
            AddHandler btnSiguiente.Click, Sub() Ir(IrATipo.Siguiente)
            AddHandler btnUltimo.Click, Sub() Ir(IrATipo.Ultimo)
            Mostrar(Nothing)
        End Sub

        Private Sub Mostrar(tipo As TipoCodigo)
            _actual = tipo
            txtTipo.Text = If(tipo Is Nothing, "", tipo.Tipo.Trim())
            txtDescripcion.Text = If(tipo Is Nothing, "", tipo.Descripcion)
            txtSistema.Text = If(tipo Is Nothing, "", If(tipo.Sistema = TipoCodigo.Si, TextoSistema, TextoUsuario))
            cmbEstado.SelectedIndex = If(tipo Is Nothing, -1, If(tipo.Activo = TipoCodigo.Si, 0, 1))
            ' Un tipo en blanco es el código interno: no se edita ni se borra, igual que en VB6.
            barra.Estado(BotonBarra.Borrar, txtTipo.Text.Length > 0)
            barra.Estado(BotonBarra.Editar, txtTipo.Text.Length > 0)
        End Sub

        Private Sub Editable(activo As Boolean, Optional tipoNuevo As Boolean = False)
            txtTipo.ReadOnly = Not tipoNuevo
            txtDescripcion.ReadOnly = Not activo
            cmbEstado.Enabled = activo
            For Each boton In {btnPrimero, btnAnterior, btnSiguiente, btnUltimo}
                boton.Enabled = Not activo
            Next
        End Sub

        Private Async Sub Ir(destino As IrATipo)
            Try
                Dim tipo = Await _repo.IrAAsync(destino, If(_actual Is Nothing, "", _actual.Tipo))
                If tipo IsNot Nothing Then Mostrar(tipo)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Function Validar() As Boolean
            Dim falta = If(txtTipo.Text.Trim().Length = 0, "El tipo es un dato requerido.", If(txtDescripcion.Text.Trim().Length = 0, "La descripción es un dato requerido.", ""))
            If falta.Length = 0 Then Return True
            MessageBox.Show(Me, falta, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End Function

        Private Async Function Borrar() As Task
            Dim tipo = txtTipo.Text
            If MessageBox.Show(Me, $"¿Está seguro de que desea borrar el tipo de código '{tipo}'?", Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            If Await _repo.BorrarTipoAsync(tipo) Then
                MessageBox.Show(Me, $"El tipo de código '{tipo}' se ha borrado.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Mostrar(Await _repo.IrAAsync(IrATipo.Primero, ""))
            Else
                MessageBox.Show(Me, $"No se puede borrar el tipo de código '{tipo}' porque está siendo utilizado.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
        End Function

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        Mostrar(New TipoCodigo())
                        Editable(True, True)
                        txtTipo.Focus()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        Editable(True)
                        txtDescripcion.Focus()
                    Case BotonBarra.Guardar
                        If Not Validar() Then Return True
                        Dim tipo As New TipoCodigo With {
                            .Tipo = txtTipo.Text.Trim(), .Descripcion = txtDescripcion.Text.Trim(), .Sistema = _actual.Sistema,
                            .Activo = If(cmbEstado.SelectedIndex = 0, TipoCodigo.Si, TipoCodigo.No)
                        }
                        Await _repo.GuardarTipoAsync(tipo, _uid, _local)
                        Mostrar(tipo)
                        Editable(False)
                    Case BotonBarra.Cancelar
                        Mostrar(If(_actual Is Nothing OrElse _actual.Tipo.Length = 0, Nothing, Await _repo.ObtenerTipoAsync(_actual.Tipo)))
                        Editable(False)
                    Case BotonBarra.Borrar
                        Await Borrar()
                    Case BotonBarra.Buscar
                        Using datos = Await _repo.BuscarTiposAsync()
                            Using buscador As New FrmBuscador("Seleccione el tipo", datos)
                                If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                                    Mostrar(Await _repo.ObtenerTipoAsync(buscador.Resultado(0)))
                                End If
                            End Using
                        End Using
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "TIPOS DE CODIGOS", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

    End Class

End Namespace
