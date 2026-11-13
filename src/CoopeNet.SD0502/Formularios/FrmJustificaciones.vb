Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Justificaciones: los motivos que el cajero elige al modificar cantidades, eliminar líneas, cerrar sin guardar,
    ''' retornar, anular o cambiar precios. Migración de SD0502\frmMantJustif.frm (Mantenimiento, opción 010,
    ''' privilegio 5006).
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: el movimiento no se cambia mientras se edita (VB6 lo dejaba y el cambio borraba lo
    ''' escrito), al guardar una nueva se muestra su código, Cancelar vuelve a la que se veía y reactiva anterior y
    ''' siguiente, y Borrar solo se activa con una justificación cargada.
    ''' </remarks>
    Public Class FrmJustificaciones
        Inherits Form

        Private Const Nuevo As String = "<< Nuevo >>"

        Private ReadOnly _repo As JustificacionesRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _genero As String
        Private _actual As Justificacion
        Private _antes As Justificacion

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(12, 8), .Size = New Size(520, 150)}
        Private ReadOnly cmbMovimiento As New ComboBox With {.Name = "cmbMovimiento", .AccessibleName = "Movimiento", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(104, 24), .Width = 400}
        Private ReadOnly lblCodigo As New Label With {.Name = "lblCodigo", .AutoSize = True, .Location = New Point(104, 68)}
        Private ReadOnly txtDescripcion As New TextBox With {
            .Name = "txtDescripcion", .AccessibleName = "Descripción", .CharacterCasing = CharacterCasing.Upper,
            .MaxLength = JustificacionesRepositorio.LargoDescripcion, .Location = New Point(104, 106), .Width = 400}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Justificación anterior", .Location = New Point(12, 168), .Size = New Size(56, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Justificación siguiente", .Location = New Point(72, 168), .Size = New Size(56, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(136, 168), .Size = New Size(396, 32)}

        Public Sub New(repo As JustificacionesRepositorio, uid As Long, genero As String)
            _repo = repo
            _uid = uid
            _genero = genero
            Text = "Mantenimiento de justificaciones (anulaciones y devoluciones)"
            ClientSize = New Size(544, 212)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            cmbMovimiento.Items.AddRange(JustificacionesRepositorio.Movimientos.ToArray())
            cmbMovimiento.SelectedIndex = 0
            gbDatos.Controls.AddRange(New Control() {
                Rotulo("Movimiento", 12, 27), cmbMovimiento, Rotulo("Código", 12, 68), lblCodigo,
                Rotulo("Descripción", 12, 109), txtDescripcion
            })
            Controls.AddRange(New Control() {gbDatos, btnAnterior, btnSiguiente, barra})

            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler cmbMovimiento.SelectionChangeCommitted, Sub()
                                                                   Limpiar()
                                                                   Habilitar(False)
                                                               End Sub
            AddHandler btnAnterior.Click, Async Sub() Await MoverAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await MoverAsync(True)
            Limpiar()
            Habilitar(False)
        End Sub

        Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private ReadOnly Property Movimiento As MovimientoJustificado
            Get
                Return CType(Math.Max(0, cmbMovimiento.SelectedIndex), MovimientoJustificado)
            End Get
        End Property

        ''' <summary>SD_Limpiar: el movimiento se queda.</summary>
        Private Sub Limpiar()
            _actual = Nothing
            txtDescripcion.Text = ""
            lblCodigo.Text = Nuevo
        End Sub

        Private Sub Mostrar(j As Justificacion)
            Limpiar()
            If j Is Nothing Then Return
            _actual = j
            cmbMovimiento.SelectedIndex = CInt(j.Accion)
            lblCodigo.Text = j.Codigo.ToString(CultureInfo.InvariantCulture)
            txtDescripcion.Text = j.Descripcion
        End Sub

        Private Sub Habilitar(editando As Boolean)
            txtDescripcion.Enabled = editando
            cmbMovimiento.Enabled = Not editando
            btnAnterior.Enabled = Not editando
            btnSiguiente.Enabled = Not editando
            barra.Estado(BotonBarra.Borrar, _actual IsNot Nothing)
        End Sub

        ''' <summary>cmdAnterior y cmdProximo: en el extremo se apaga el botón de ese lado.</summary>
        Private Async Function MoverAsync(siguiente As Boolean) As Task
            Dim boton = If(siguiente, btnSiguiente, btnAnterior)
            Dim otro = If(siguiente, btnAnterior, btnSiguiente)
            otro.Enabled = True
            Try
                Dim j = Await _repo.VecinaAsync(Movimiento, If(_actual?.Codigo, 0), siguiente)
                If j Is Nothing Then
                    boton.Enabled = False
                    Return
                End If
                Mostrar(j)
                barra.Estado(BotonBarra.Borrar, True)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        _antes = _actual
                        Limpiar()
                        Habilitar(True)
                        txtDescripcion.Focus()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        _antes = _actual
                        Habilitar(True)
                        txtDescripcion.Focus()
                    Case BotonBarra.Guardar
                        If txtDescripcion.Text.Trim().Length = 0 Then
                            txtDescripcion.Focus()
                            Return True
                        End If
                        Dim j As New Justificacion With {.Accion = Movimiento, .Codigo = If(_actual?.Codigo, 0), .Descripcion = txtDescripcion.Text}
                        j.Codigo = Await _repo.GuardarAsync(j, _uid)
                        Mostrar(If(Await _repo.CargarAsync(j.Accion, j.Codigo), j))
                        Habilitar(False)
                    Case BotonBarra.Cancelar
                        Mostrar(_antes)
                        Habilitar(False)
                    Case BotonBarra.Borrar
                        Await BorrarAsync()
                    Case BotonBarra.Buscar
                        Await BuscarAsync()
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "JUSTIFICACIONES", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        Private Async Function BorrarAsync() As Task
            If _actual Is Nothing Then Return
            Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} de eliminar los datos actuales?"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Await _repo.BorrarAsync(_actual.Accion, _actual.Codigo)
            Limpiar()
            Habilitar(False)
        End Function

        Private Async Function BuscarAsync() As Task
            Limpiar()
            Using datos = Await _repo.BuscarAsync(Movimiento)
                Using buscador As New FrmBuscador("Búsqueda de justificaciones", datos)
                    If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                        Mostrar(Await _repo.CargarAsync(Movimiento, CInt(Core.Vb6.Val(buscador.Resultado(0)))))
                    End If
                End Using
            End Using
            Habilitar(False)
        End Function

    End Class

End Namespace
