Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Bancos del POS (posbancos). Migración de SD0502\frmBancos.frm (Mantenimiento, opción 028, privilegio 5045).
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: el nombre es obligatorio, al guardar uno nuevo se muestra su código, Cancelar vuelve al
    ''' que se veía, y Borrar solo se activa con un banco cargado.
    ''' </remarks>
    Public Class FrmBancosPos
        Inherits Form

        Private Const Nuevo As String = "<< Nuevo >>"

        Private ReadOnly _repo As BancosPosRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _genero As String
        Private _actual As BancoPos
        Private _antes As BancoPos

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(12, 8), .Size = New Size(560, 132)}
        Private ReadOnly lblCodigo As New Label With {.Name = "lblCodigo", .AutoSize = True, .Location = New Point(110, 26)}
        Private ReadOnly lblFecha As New Label With {.Name = "lblFecha", .AutoSize = True, .Location = New Point(250, 26)}
        Private ReadOnly txtNombre As New TextBox With {
            .Name = "txtNombre", .AccessibleName = "Descripción", .CharacterCasing = CharacterCasing.Upper,
            .MaxLength = BancosPosRepositorio.LargoNombre, .Location = New Point(110, 56), .Width = 434}
        Private ReadOnly txtTelefono As New TextBox With {.Name = "txtTelefono", .AccessibleName = "Teléfono", .MaxLength = BancosPosRepositorio.LargoTelefono, .Location = New Point(110, 92), .Width = 160}
        Private ReadOnly txtFax As New TextBox With {.Name = "txtFax", .AccessibleName = "Fax", .MaxLength = BancosPosRepositorio.LargoTelefono, .Location = New Point(384, 92), .Width = 160}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Banco anterior", .Location = New Point(12, 150), .Size = New Size(56, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Banco siguiente", .Location = New Point(72, 150), .Size = New Size(56, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(136, 150), .Size = New Size(436, 32)}

        Public Sub New(repo As BancosPosRepositorio, uid As Long, local As Long, genero As String)
            _repo = repo
            _uid = uid
            _local = local
            _genero = genero
            Text = "Creación y mantenimiento de bancos"
            ClientSize = New Size(584, 194)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            gbDatos.Controls.AddRange(New Control() {
                Rotulo("Código", 12, 26), lblCodigo, lblFecha,
                Rotulo("Descripción", 12, 59), txtNombre,
                Rotulo("Teléfono", 12, 95), txtTelefono, Rotulo("Fax", 340, 95), txtFax
            })
            Controls.AddRange(New Control() {gbDatos, btnAnterior, btnSiguiente, barra})

            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler btnAnterior.Click, Async Sub() Await MoverAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await MoverAsync(True)
            Limpiar()
            Habilitar(False)
        End Sub

        Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        ''' <summary>SD_Limpiar.</summary>
        Private Sub Limpiar()
            _actual = Nothing
            txtNombre.Text = ""
            txtTelefono.Text = ""
            txtFax.Text = ""
            lblFecha.Text = ""
            lblCodigo.Text = Nuevo
        End Sub

        Private Sub Mostrar(b As BancoPos)
            Limpiar()
            If b Is Nothing Then Return
            _actual = b
            lblCodigo.Text = b.Codigo.ToString(CultureInfo.InvariantCulture)
            txtNombre.Text = b.Nombre
            txtTelefono.Text = b.Telefono
            txtFax.Text = b.Fax
            lblFecha.Text = If(b.FC.HasValue, "Creado el " & b.FC.Value.ToString("D", CultureInfo.CurrentCulture), "")
        End Sub

        ''' <summary>SD_Activar.</summary>
        Private Sub Habilitar(editando As Boolean)
            txtNombre.Enabled = editando
            txtTelefono.Enabled = editando
            txtFax.Enabled = editando
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
                Dim b = Await _repo.VecinoAsync(If(_actual?.Codigo, 0), siguiente)
                If b Is Nothing Then
                    boton.Enabled = False
                    Return
                End If
                Mostrar(b)
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
                        txtNombre.Focus()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        _antes = _actual
                        Habilitar(True)
                        txtNombre.Focus()
                    Case BotonBarra.Guardar
                        If Not Await GuardarAsync() Then Return True
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
                Mensajes.NoGuardo(ex.Message, "Datos del banco", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SD_Guardar.</summary>
        Private Async Function GuardarAsync() As Task(Of Boolean)
            If txtNombre.Text.Trim().Length = 0 Then
                txtNombre.Focus()
                Return False
            End If
            Dim b As New BancoPos With {.Codigo = If(_actual?.Codigo, 0), .Nombre = txtNombre.Text, .Telefono = txtTelefono.Text, .Fax = txtFax.Text, .FC = _actual?.FC}
            Dim codigo = Await _repo.GuardarAsync(b, _uid, _local)
            If codigo = 0 Then
                MessageBox.Show(Me, $"Ya hay {BancosPosRepositorio.CodigoMaximo} bancos: el código no admite más.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return False
            End If
            Mostrar(If(Await _repo.CargarAsync(codigo), b))
            Return True
        End Function

        Private Async Function BorrarAsync() As Task
            If _actual Is Nothing Then Return
            Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} de eliminar los datos actuales?"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Await _repo.BorrarAsync(_actual.Codigo)
            Limpiar()
            Habilitar(False)
        End Function

        Private Async Function BuscarAsync() As Task
            Limpiar()
            Using datos = Await _repo.BuscarAsync()
                Using buscador As New FrmBuscador("Búsqueda de bancos", datos)
                    If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                        Mostrar(Await _repo.CargarAsync(CInt(Core.Vb6.Val(buscador.Resultado(0)))))
                    End If
                End Using
            End Using
            Habilitar(False)
        End Function

    End Class

End Namespace
