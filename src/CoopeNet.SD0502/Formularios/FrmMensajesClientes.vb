Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core
Imports CoopeNet.Core.Datos
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Mensajes para clientes: los textos que la pantalla del cliente muestra por turnos, su imagen y la velocidad
    ''' con que pasan. Migración de SD0502\frmMantMensajes.frm (Mantenimiento, opción 019, privilegio 5024).
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: al guardar un mensaje nuevo se muestra el código que le tocó (VB6 seguía mostrando
    ''' "&lt;&lt; Nuevo &gt;&gt;" y un segundo Guardar creaba otro), Borrar pregunta antes y solo se activa con un mensaje
    ''' cargado, y Cancelar vuelve al mensaje que se veía.
    ''' </remarks>
    Public Class FrmMensajesClientes
        Inherits Form

        Private Const Nuevo As String = "<< Nuevo >>"

        Private ReadOnly _repo As MensajesClientesRepositorio
        Private ReadOnly _parametros As Parametros
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _genero As String
        Private ReadOnly _puedeImagen As Boolean
        Private _actual As MensajeCliente
        Private _antes As MensajeCliente
        Private _editando As Boolean

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(12, 8), .Size = New Size(600, 218)}
        Private ReadOnly txtTitulo1 As New TextBox With {.Name = "txtTitulo1", .AccessibleName = "Título 1", .Location = New Point(90, 24), .Width = 496, .MaxLength = MensajesClientesRepositorio.LargoTexto}
        Private ReadOnly txtLocal As New TextBox With {.Name = "txtLocal", .AccessibleName = "Local", .Location = New Point(90, 60), .Width = 496, .MaxLength = MensajesClientesRepositorio.LargoTexto}
        Private ReadOnly txtTitulo2 As New TextBox With {.Name = "txtTitulo2", .AccessibleName = "Título 2", .Location = New Point(90, 96), .Width = 496, .MaxLength = MensajesClientesRepositorio.LargoTexto}
        Private ReadOnly txtTitulo3 As New TextBox With {.Name = "txtTitulo3", .AccessibleName = "Título 3", .Location = New Point(90, 132), .Width = 496, .MaxLength = MensajesClientesRepositorio.LargoTexto}
        Private ReadOnly lblCodigo As New Label With {.Name = "lblCodigo", .AutoSize = True, .Location = New Point(90, 176)}
        Private ReadOnly btnImagen As New Button With {.Name = "btnImagen", .Text = "&Imágenes", .Location = New Point(486, 170), .Size = New Size(100, 28)}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Mensaje anterior", .Location = New Point(12, 236), .Size = New Size(56, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Mensaje siguiente", .Location = New Point(72, 236), .Size = New Size(56, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(136, 236), .Size = New Size(476, 32)}
        Private ReadOnly gbVelocidad As New GroupBox With {.Name = "gbVelocidad", .Location = New Point(12, 280), .Size = New Size(600, 70)}
        Private ReadOnly chkVelocidad As New CheckBox With {.Name = "chkVelocidad", .Text = "Modificar velocidad de mensajes", .AutoSize = True, .Location = New Point(10, 0)}
        Private ReadOnly trkVelocidad As New TrackBar With {
            .Name = "trkVelocidad", .AccessibleName = "Velocidad de los mensajes", .Location = New Point(10, 24), .Width = 440, .AutoSize = False, .Height = 32,
            .Minimum = MensajesClientesRepositorio.VelocidadMinima, .Maximum = MensajesClientesRepositorio.VelocidadMaxima, .LargeChange = 1, .Value = MensajesClientesRepositorio.VelocidadMinima}
        Private ReadOnly lblVelocidad As New Label With {.Name = "lblVelocidad", .AutoSize = True, .Location = New Point(460, 30)}
        Private ReadOnly btnVelocidad As New Button With {.Name = "btnVelocidad", .Text = "&Aceptar", .Location = New Point(496, 24), .Size = New Size(90, 30)}

        Public Sub New(repo As MensajesClientesRepositorio, parametros As Parametros, local As Long, uid As Long, genero As String, puedeImagen As Boolean)
            _repo = repo
            _parametros = parametros
            _local = local
            _uid = uid
            _genero = genero
            _puedeImagen = puedeImagen
            Text = "Mantenimiento de mensajes"
            ClientSize = New Size(624, 362)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            gbDatos.Controls.AddRange(New Control() {
                Rotulo("Título 1", 12, 27), txtTitulo1, Rotulo("Local", 12, 63), txtLocal,
                Rotulo("Título 2", 12, 99), txtTitulo2, Rotulo("Título 3", 12, 135), txtTitulo3,
                Rotulo("Código", 12, 176), lblCodigo, btnImagen
            })
            ' La casilla va en el borde del marco, como el CheckVisible de SDFrame.
            gbVelocidad.Controls.AddRange(New Control() {chkVelocidad, trkVelocidad, lblVelocidad, btnVelocidad})
            Controls.AddRange(New Control() {gbDatos, btnAnterior, btnSiguiente, barra, gbVelocidad})

            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler btnAnterior.Click, Async Sub() Await MoverAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await MoverAsync(True)
            AddHandler btnImagen.Click, AddressOf Imagen_Clic
            AddHandler chkVelocidad.CheckedChanged, Sub() HabilitarVelocidad(chkVelocidad.Checked)
            AddHandler trkVelocidad.ValueChanged, Sub() lblVelocidad.Text = trkVelocidad.Value.ToString(CultureInfo.CurrentCulture)
            AddHandler btnVelocidad.Click, Async Sub() Await GuardarVelocidadAsync()
            Limpiar()
            Habilitar(False)
            HabilitarVelocidad(False)
        End Sub

        Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        ''' <summary>Form_Load: la velocidad guardada del local (o la global).</summary>
        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                Dim valor = Await _parametros.ObtenerNumeroAsync(_local, MensajesClientesRepositorio.ParametroVelocidad)
                trkVelocidad.Value = MensajesClientesRepositorio.AjustarVelocidad(valor)
                lblVelocidad.Text = trkVelocidad.Value.ToString(CultureInfo.CurrentCulture)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ' ---- Datos en pantalla ----

        ''' <summary>SD_Limpiar.</summary>
        Private Sub Limpiar()
            _actual = Nothing
            txtTitulo1.Text = ""
            txtTitulo2.Text = ""
            txtTitulo3.Text = ""
            txtLocal.Text = ""
            lblCodigo.Text = Nuevo
        End Sub

        Private Sub Mostrar(m As MensajeCliente)
            Limpiar()
            If m Is Nothing Then Return
            _actual = m
            txtTitulo1.Text = m.Titulo1
            txtTitulo2.Text = m.Titulo2
            txtTitulo3.Text = m.Titulo3
            txtLocal.Text = m.NombreLocal
            lblCodigo.Text = m.Codigo.ToString(CultureInfo.InvariantCulture)
        End Sub

        ''' <summary>Habilitar sobre frmDatos: el botón de imágenes también va en el marco, así que solo sirve al editar.</summary>
        Private Sub Habilitar(editando As Boolean)
            _editando = editando
            For Each control In New Control() {txtTitulo1, txtLocal, txtTitulo2, txtTitulo3}
                control.Enabled = editando
            Next
            btnImagen.Enabled = editando AndAlso _actual IsNot Nothing
            btnAnterior.Enabled = Not editando
            btnSiguiente.Enabled = Not editando
            barra.Estado(BotonBarra.Borrar, _actual IsNot Nothing)
        End Sub

        Private Sub HabilitarVelocidad(activa As Boolean)
            trkVelocidad.Enabled = activa
            lblVelocidad.Enabled = activa
            btnVelocidad.Enabled = activa
        End Sub

        ' ---- Anterior y siguiente ----

        ''' <summary>cmdAnterior y cmdSiguiente: al llegar al extremo se apaga el botón de ese lado.</summary>
        Private Async Function MoverAsync(siguiente As Boolean) As Task
            Dim desde = If(_actual?.Codigo, 0L)
            Dim boton = If(siguiente, btnSiguiente, btnAnterior)
            Dim otro = If(siguiente, btnAnterior, btnSiguiente)
            otro.Enabled = True
            Try
                Dim codigo = Await _repo.VecinoAsync(desde, siguiente, _local)
                If codigo = 0 Then
                    boton.Enabled = False
                    Return
                End If
                Mostrar(Await _repo.CargarAsync(codigo))
                barra.Estado(BotonBarra.Borrar, _actual IsNot Nothing)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ' ---- Imagen ----

        Private Sub Imagen_Clic(sender As Object, e As EventArgs)
            If _actual Is Nothing Then Return
            Using f As New FrmImagenMensaje(_repo, _actual.Codigo, txtTitulo1.Text.Trim(), _local, _genero, _puedeImagen)
                Tema.Aplicar(f, Modulo.TituloModulo)
                f.ShowDialog(Me)
            End Using
        End Sub

        ' ---- Velocidad ----

        ''' <summary>cmdGuarVeloc: guarda PosVelocMensaj del local y apaga la casilla.</summary>
        Private Async Function GuardarVelocidadAsync() As Task
            Try
                Await _parametros.GuardarAsync(_local, MensajesClientesRepositorio.ParametroVelocidad, trkVelocidad.Value.ToString(CultureInfo.InvariantCulture), _uid)
                chkVelocidad.Checked = False
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "VELOCIDAD", Modulo.TituloModulo)
            End Try
        End Function

        ' ---- Barra ----

        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        _antes = _actual
                        Limpiar()
                        Habilitar(True)
                        txtTitulo1.Focus()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        _antes = _actual
                        Habilitar(True)
                        txtTitulo1.Focus()
                    Case BotonBarra.Guardar
                        Await GuardarAsync()
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
                Mensajes.NoGuardo(ex.Message, "MENSAJES", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SD_Guardar. Como en VB6, no hay campos obligatorios.</summary>
        Private Async Function GuardarAsync() As Task
            Dim m As New MensajeCliente With {
                .Codigo = If(_actual?.Codigo, 0L), .Titulo1 = txtTitulo1.Text, .Titulo2 = txtTitulo2.Text,
                .Titulo3 = txtTitulo3.Text, .NombreLocal = txtLocal.Text
            }
            Dim codigo = Await _repo.GuardarAsync(m, _local)
            Mostrar(If(Await _repo.CargarAsync(codigo), m))
        End Function

        Private Async Function BorrarAsync() As Task
            If _actual Is Nothing Then Return
            Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} que desea eliminar el mensaje {_actual.Codigo}?"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Await _repo.BorrarAsync(_actual.Codigo, _local)
            Limpiar()
            Habilitar(False)
        End Function

        Private Async Function BuscarAsync() As Task
            Using datos = Await _repo.BuscarAsync(_local)
                Using buscador As New FrmBuscador("Búsqueda de mensajes", datos)
                    If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                        Mostrar(Await _repo.CargarAsync(CLng(Vb6.Val(buscador.Resultado(0)))))
                    End If
                End Using
            End Using
            Habilitar(False)
        End Function

    End Class

End Namespace
