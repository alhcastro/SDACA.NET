Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Vendedores asignados al local. Migración de SD0502\frmVendedores.frm (Mantenimiento, opción 069, privilegio 5017).
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: se muestra el nombre del vendedor; Editar queda apagado porque el único dato es el
    ''' vendedor (en VB6 editar y guardar asignaba otro y dejaba el anterior); avisa si ya estaba asignado; Cancelar
    ''' vuelve al que se veía.
    ''' </remarks>
    Public Class FrmVendedores
        Inherits Form

        Private ReadOnly _repo As VendedoresRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _genero As String
        Private _actual As Long?
        Private _antes As Long?

        Private ReadOnly lblLocal As New Label With {.Name = "lblLocal", .AutoSize = True, .Location = New Point(12, 12)}
        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(12, 40), .Size = New Size(480, 96)}
        Private ReadOnly cmbVendedor As New ComboBox With {.Name = "cmbVendedor", .AccessibleName = "Vendedor", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(100, 24), .Width = 364}
        Private ReadOnly lblNombre As New Label With {.Name = "lblNombre", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(100, 60), .Size = New Size(364, 20)}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Vendedor anterior", .Location = New Point(12, 146), .Size = New Size(40, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Vendedor siguiente", .Location = New Point(54, 146), .Size = New Size(40, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(100, 146), .Size = New Size(392, 32)}

        Public Sub New(repo As VendedoresRepositorio, uid As Long, local As Long, localDescripcion As String, genero As String)
            _repo = repo
            _uid = uid
            _local = local
            _genero = genero
            Text = "Vendedores"
            ClientSize = New Size(504, 190)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            lblLocal.Text = $"Vendedores asignados en {localDescripcion}"

            gbDatos.Controls.AddRange(New Control() {New Label With {.Text = "Vendedor", .AutoSize = True, .Location = New Point(12, 27)}, cmbVendedor,
                                                     New Label With {.Text = "Nombre", .AutoSize = True, .Location = New Point(12, 60)}, lblNombre})
            Controls.AddRange(New Control() {lblLocal, gbDatos, btnAnterior, btnSiguiente, barra})

            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler btnAnterior.Click, Async Sub() Await MoverAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await MoverAsync(True)
            AddHandler cmbVendedor.SelectedIndexChanged, Sub() lblNombre.Text = If(TryCast(cmbVendedor.SelectedItem, UsuarioVendedor)?.Nombre, "")
            Habilitar(False)
        End Sub

        ''' <summary>Form_Load: SD_ComboVend.</summary>
        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                cmbVendedor.Items.AddRange((Await _repo.UsuariosAsync(_local)).ToArray())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            Mostrar(Nothing)
        End Sub

        ''' <summary>SD_Limpiar o SD_SeleccionaDelCombo.</summary>
        Private Sub Mostrar(id As Long?)
            _actual = id
            cmbVendedor.SelectedIndex = -1
            lblNombre.Text = ""
            If Not id.HasValue Then Return
            For i = 0 To cmbVendedor.Items.Count - 1
                If DirectCast(cmbVendedor.Items(i), UsuarioVendedor).Uid = id.Value Then cmbVendedor.SelectedIndex = i
            Next
        End Sub

        Private Sub Habilitar(editando As Boolean)
            cmbVendedor.Enabled = editando
            btnAnterior.Enabled = Not editando
            btnSiguiente.Enabled = Not editando
            barra.Estado(BotonBarra.Editar, False)
            barra.Estado(BotonBarra.Borrar, _actual.HasValue)
        End Sub

        ''' <summary>cmdAnterior y cmdProximo: en el extremo se apaga el botón de ese lado.</summary>
        Private Async Function MoverAsync(siguiente As Boolean) As Task
            Dim boton = If(siguiente, btnSiguiente, btnAnterior)
            Dim otro = If(siguiente, btnAnterior, btnSiguiente)
            otro.Enabled = True
            Try
                Dim id = Await _repo.VecinoAsync(If(_actual, If(siguiente, 0L, Long.MaxValue)), _local, siguiente)
                If Not id.HasValue Then
                    boton.Enabled = False
                    Return
                End If
                Mostrar(id)
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
                        Mostrar(Nothing)
                        Habilitar(True)
                        cmbVendedor.Focus()
                    Case BotonBarra.Editar
                        Return True
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
                Mensajes.NoGuardo(ex.Message, "Vendedores", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SD_Guardar.</summary>
        Private Async Function GuardarAsync() As Task(Of Boolean)
            Dim elegido = TryCast(cmbVendedor.SelectedItem, UsuarioVendedor)
            If elegido Is Nothing Then
                cmbVendedor.Focus()
                Return False
            End If
            If Await _repo.ExisteAsync(elegido.Uid, _local) Then
                MessageBox.Show(Me, $"{elegido.Usuario} ya es vendedor en este local.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Mostrar(elegido.Uid)
                Return True
            End If
            Await _repo.GuardarAsync(elegido.Uid, _uid, _local)
            Mostrar(elegido.Uid)
            Return True
        End Function

        Private Async Function BorrarAsync() As Task
            If Not _actual.HasValue Then Return
            Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} de eliminar los datos actuales?"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Await _repo.BorrarAsync(_actual.Value, _local)
            Mostrar(Nothing)
            Habilitar(False)
        End Function

        Private Async Function BuscarAsync() As Task
            Using datos = Await _repo.BuscarAsync(_local)
                Using buscador As New FrmBuscador("Búsqueda de vendedores", datos)
                    If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                        Mostrar(Core.Vb6.Val(buscador.Resultado(0)))
                    End If
                End Using
            End Using
            Habilitar(False)
        End Function

    End Class

End Namespace
