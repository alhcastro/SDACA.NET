Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Catálogo de marcas (posmarcas). Migración de SD0502\frmMarcas.frm (Mantenimiento, opción 051, privilegio 5082).
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: la descripción es obligatoria, al guardar una nueva se muestra su código, Cancelar vuelve
    ''' a la que se veía, y Borrar solo se activa con una marca cargada.
    ''' </remarks>
    Public Class FrmMarcas
        Inherits Form

        Private Const Nuevo As String = "<< Nuevo >>"

        Private ReadOnly _repo As MarcasRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _genero As String
        Private _actual As MarcaArticulo
        Private _antes As MarcaArticulo

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(12, 8), .Size = New Size(480, 96)}
        Private ReadOnly lblCodigo As New Label With {.Name = "lblCodigo", .AutoSize = True, .Location = New Point(110, 26)}
        Private ReadOnly txtDescripcion As New TextBox With {
            .Name = "txtDescripcion", .AccessibleName = "Descripción", .CharacterCasing = CharacterCasing.Upper,
            .MaxLength = MarcasRepositorio.LargoDescripcion, .Location = New Point(110, 56), .Width = 354}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Marca anterior", .Location = New Point(12, 114), .Size = New Size(40, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Marca siguiente", .Location = New Point(54, 114), .Size = New Size(40, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(100, 114), .Size = New Size(392, 32)}

        Public Sub New(repo As MarcasRepositorio, uid As Long, local As Long, genero As String)
            _repo = repo
            _uid = uid
            _local = local
            _genero = genero
            Text = "Mantenimiento de marcas"
            ClientSize = New Size(504, 158)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            gbDatos.Controls.AddRange(New Control() {Rotulo("Código", 12, 26), lblCodigo, Rotulo("Descripción", 12, 59), txtDescripcion})
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
            txtDescripcion.Text = ""
            lblCodigo.Text = Nuevo
        End Sub

        Private Sub Mostrar(m As MarcaArticulo)
            Limpiar()
            If m Is Nothing Then Return
            _actual = m
            lblCodigo.Text = m.Codigo.ToString(CultureInfo.InvariantCulture)
            txtDescripcion.Text = m.Descripcion
        End Sub

        Private Sub Habilitar(editando As Boolean)
            txtDescripcion.Enabled = editando
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
                Dim m = Await _repo.VecinaAsync(If(_actual?.Codigo, 0), _local, siguiente)
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
                Mensajes.NoGuardo(ex.Message, "Datos de la marca", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SD_Guardar.</summary>
        Private Async Function GuardarAsync() As Task(Of Boolean)
            If txtDescripcion.Text.Trim().Length = 0 Then
                txtDescripcion.Focus()
                Return False
            End If
            Dim m As New MarcaArticulo With {.Codigo = If(_actual?.Codigo, 0), .Descripcion = txtDescripcion.Text}
            Dim codigo = Await _repo.GuardarAsync(m, _uid, _local)
            Mostrar(If(Await _repo.CargarAsync(codigo, _local), m))
            Return True
        End Function

        Private Async Function BorrarAsync() As Task
            If _actual Is Nothing Then Return
            Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} de eliminar los datos actuales?"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Await _repo.BorrarAsync(_actual.Codigo, _local)
            Limpiar()
            Habilitar(False)
        End Function

        Private Async Function BuscarAsync() As Task
            Limpiar()
            Using datos = Await _repo.BuscarAsync(_local)
                Using buscador As New FrmBuscador("Búsqueda de marcas", datos)
                    If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                        Mostrar(Await _repo.CargarAsync(CInt(Core.Vb6.Val(buscador.Resultado(0))), _local))
                    End If
                End Using
            End Using
            Habilitar(False)
        End Function

    End Class

End Namespace
