Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Tipos de moneda del POS (postipomoneda). Migración de SD0502\frmtipomonedas.frm (Mantenimiento, opción 025,
    ''' privilegio 5035), con su botón "Tipo de cambio".
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: la descripción es obligatoria, al guardar una nueva se muestra su tipo, Cancelar vuelve
    ''' a la que se veía, y Borrar solo se activa con una moneda cargada.
    ''' </remarks>
    Public Class FrmTiposMoneda
        Inherits Form

        Private Const Nuevo As String = "<< Nuevo >>"

        Private ReadOnly _repo As MonedasPosRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _genero As String
        Private _actual As TipoMonedaPos
        Private _antes As TipoMonedaPos

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(12, 8), .Size = New Size(420, 110)}
        Private ReadOnly lblTipo As New Label With {.Name = "lblTipo", .AutoSize = True, .Location = New Point(104, 30)}
        Private ReadOnly btnCambio As New Button With {.Name = "btnCambio", .Text = "&Tipo de cambio", .Location = New Point(276, 22), .Size = New Size(128, 30)}
        Private ReadOnly txtDescripcion As New TextBox With {
            .Name = "txtDescripcion", .AccessibleName = "Descripción", .CharacterCasing = CharacterCasing.Upper,
            .MaxLength = MonedasPosRepositorio.LargoDescripcion, .Location = New Point(104, 68), .Width = 300}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Moneda anterior", .Location = New Point(12, 128), .Size = New Size(48, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Moneda siguiente", .Location = New Point(64, 128), .Size = New Size(48, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(120, 128), .Size = New Size(312, 32)}

        Public Sub New(repo As MonedasPosRepositorio, uid As Long, local As Long, genero As String)
            _repo = repo
            _uid = uid
            _local = local
            _genero = genero
            Text = "Mantenimiento de monedas"
            ClientSize = New Size(444, 172)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            gbDatos.Controls.AddRange(New Control() {Rotulo("Código", 12, 30), lblTipo, btnCambio, Rotulo("Descripción", 12, 71), txtDescripcion})
            Controls.AddRange(New Control() {gbDatos, btnAnterior, btnSiguiente, barra})

            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler btnAnterior.Click, Async Sub() Await MoverAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await MoverAsync(True)
            AddHandler btnCambio.Click, AddressOf Cambio_Clic
            Limpiar()
            Habilitar(False)
        End Sub

        Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Sub Limpiar()
            _actual = Nothing
            txtDescripcion.Text = ""
            lblTipo.Text = Nuevo
        End Sub

        Private Sub Mostrar(m As TipoMonedaPos)
            Limpiar()
            If m Is Nothing Then Return
            _actual = m
            lblTipo.Text = m.Tipo.ToString(CultureInfo.InvariantCulture)
            txtDescripcion.Text = m.Descripcion
        End Sub

        Private Sub Habilitar(editando As Boolean)
            txtDescripcion.Enabled = editando
            btnAnterior.Enabled = Not editando
            btnSiguiente.Enabled = Not editando
            btnCambio.Enabled = Not editando
            barra.Estado(BotonBarra.Borrar, _actual IsNot Nothing)
        End Sub

        ''' <summary>cmdAnterior y cmdProximo: en el extremo se apaga el botón de ese lado.</summary>
        Private Async Function MoverAsync(siguiente As Boolean) As Task
            Dim boton = If(siguiente, btnSiguiente, btnAnterior)
            Dim otro = If(siguiente, btnAnterior, btnSiguiente)
            otro.Enabled = True
            Try
                Dim m = Await _repo.VecinaAsync(If(_actual?.Tipo, 0), siguiente)
                If m Is Nothing Then
                    boton.Enabled = False
                    Return
                End If
                Mostrar(m)
                barra.Estado(BotonBarra.Borrar, True)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>cmdCambio: el tipo de cambio del POS.</summary>
        Private Sub Cambio_Clic(sender As Object, e As EventArgs)
            Using f As New FrmTipoCambioPos(_repo, _uid, _local)
                Tema.Aplicar(f, Modulo.TituloModulo)
                f.ShowDialog(Me)
            End Using
        End Sub

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
                        Dim m As New TipoMonedaPos With {.Tipo = If(_actual?.Tipo, 0), .Descripcion = txtDescripcion.Text}
                        m.Tipo = Await _repo.GuardarAsync(m)
                        Mostrar(If(Await _repo.CargarAsync(m.Tipo), m))
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
                Mensajes.NoGuardo(ex.Message, "MONEDAS", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        Private Async Function BorrarAsync() As Task
            If _actual Is Nothing Then Return
            Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} de eliminar los datos actuales?"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Await _repo.BorrarAsync(_actual.Tipo)
            Limpiar()
            Habilitar(False)
        End Function

        Private Async Function BuscarAsync() As Task
            Limpiar()
            Using datos = Await _repo.BuscarAsync()
                Using buscador As New FrmBuscador("Búsqueda de tipos de moneda", datos)
                    If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                        Mostrar(Await _repo.CargarAsync(CInt(Core.Vb6.Val(buscador.Resultado(0)))))
                    End If
                End Using
            End Using
            Habilitar(False)
        End Function

    End Class

End Namespace
