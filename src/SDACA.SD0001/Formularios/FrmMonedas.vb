Imports MySqlConnector
Imports SDACA.Core
Imports SDACA.Core.Catalogos
Imports SDACA.Ui

Namespace SD0001

    ''' <summary>
    ''' Tipos de monedas. Migración de Fuentes\SD0001\FrmMantMonedas.frm. Agrega Iniciales, que la tabla
    ''' tiene y la fuente no. Como en VB6 no se borran monedas.
    ''' </summary>
    Public Class FrmMonedas
        Inherits Form

        Private ReadOnly _catalogos As CatalogosMonetarios
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private _actual As MonedaCatalogo

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(10, 8), .Size = New Size(466, 128)}
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código", .ReadOnly = True, .TabStop = False, .Location = New Point(110, 24), .Width = 120}
        Private ReadOnly txtDescripcion As New TextBox With {.Name = "txtDescripcion", .AccessibleName = "Descripción", .MaxLength = 32, .Location = New Point(110, 56), .Width = 340}
        Private ReadOnly txtSimbolo As New TextBox With {.Name = "txtSimbolo", .AccessibleName = "Símbolo", .MaxLength = 1, .Location = New Point(110, 88), .Width = 60}
        Private ReadOnly txtIniciales As New TextBox With {.Name = "txtIniciales", .AccessibleName = "Iniciales", .MaxLength = 3, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(280, 88), .Width = 80}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(106, 144), .Size = New Size(370, 32)}

        Public Sub New(catalogos As CatalogosMonetarios, uid As Long, local As Long)
            _catalogos = catalogos
            _uid = uid
            _local = local
            Text = "Mantenimiento de Tipos de Monedas"
            ClientSize = New Size(486, 186)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            gbDatos.Controls.AddRange(New Control() {
                New Label With {.Text = "Código", .AutoSize = True, .Location = New Point(12, 27)}, txtCodigo,
                New Label With {.Text = "Descripción", .AutoSize = True, .Location = New Point(12, 59)}, txtDescripcion,
                New Label With {.Text = "Símbolo", .AutoSize = True, .Location = New Point(12, 91)}, txtSimbolo,
                New Label With {.Text = "Iniciales", .AutoSize = True, .Location = New Point(210, 91)}, txtIniciales
            })
            Controls.AddRange(New Control() {gbDatos, barra})
            barra.AlHacerClic = AddressOf Barra_Clic
            barra.Estado(BotonBarra.Borrar, False)
            Editable(False)
            AddHandler Load, AddressOf Cargar
        End Sub

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                Mostrar(Await _catalogos.ObtenerMonedaAsync(0))
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Editable(activo As Boolean)
            txtDescripcion.ReadOnly = Not activo
            txtSimbolo.ReadOnly = Not activo
            txtIniciales.ReadOnly = Not activo
        End Sub

        Private Sub Mostrar(moneda As MonedaCatalogo)
            _actual = moneda
            txtCodigo.Text = If(moneda Is Nothing, "", moneda.Codigo.ToString())
            txtDescripcion.Text = If(moneda Is Nothing, "", moneda.Descripcion)
            txtSimbolo.Text = If(moneda Is Nothing, "", moneda.Simbolo)
            txtIniciales.Text = If(moneda Is Nothing, "", moneda.Iniciales)
        End Sub

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        txtCodigo.Text = (Await _catalogos.SiguienteMonedaAsync()).ToString()
                        txtDescripcion.Clear()
                        txtSimbolo.Clear()
                        txtIniciales.Clear()
                        Editable(True)
                        txtDescripcion.Focus()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        Editable(True)
                        txtDescripcion.Focus()
                    Case BotonBarra.Guardar
                        If txtDescripcion.Text.Trim().Length = 0 Then
                            MessageBox.Show(Me, "Faltan datos: la descripción es requerida.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                            txtDescripcion.Focus()
                            Return True
                        End If
                        Dim moneda As New MonedaCatalogo With {
                            .Codigo = CInt(Vb6.Val(txtCodigo.Text)), .Descripcion = txtDescripcion.Text.Trim(), .Simbolo = txtSimbolo.Text.Trim(), .Iniciales = txtIniciales.Text.Trim()
                        }
                        Await _catalogos.GuardarMonedaAsync(moneda, _uid, _local)
                        Mostrar(moneda)
                        Editable(False)
                    Case BotonBarra.Cancelar
                        Mostrar(_actual)
                        Editable(False)
                    Case BotonBarra.Buscar
                        Using datos = Await _catalogos.BuscarMonedasAsync()
                            Using buscador As New FrmBuscador("Búsqueda de monedas", datos)
                                If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                                    Mostrar(Await _catalogos.ObtenerMonedaAsync(CInt(Vb6.Val(buscador.Resultado(0)))))
                                End If
                            End Using
                        End Using
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "MONEDAS", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

    End Class

End Namespace
