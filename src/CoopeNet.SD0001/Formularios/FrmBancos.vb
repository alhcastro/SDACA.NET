Imports MySqlConnector
Imports CoopeNet.Core
Imports CoopeNet.Core.Catalogos
Imports CoopeNet.Ui

Namespace SD0001

    ''' <summary>
    ''' Bancos. Migración de Fuentes\SD0001\FrmMantBancos.frm, con las columnas reales de gatbancos: no se
    ''' migran siglas ni web services, que no existen, ni los números de localización, que están vacíos.
    ''' Como en VB6 no se borran bancos.
    ''' </summary>
    Public Class FrmBancos
        Inherits Form

        Private ReadOnly _catalogos As CatalogosMonetarios
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private _actual As BancoCatalogo

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(10, 8), .Size = New Size(466, 96)}
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código", .ReadOnly = True, .TabStop = False, .Location = New Point(110, 24), .Width = 120}
        Private ReadOnly txtDescripcion As New TextBox With {.Name = "txtDescripcion", .AccessibleName = "Descripción", .MaxLength = 64, .Location = New Point(110, 56), .Width = 340}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(106, 112), .Size = New Size(370, 32)}

        Public Sub New(catalogos As CatalogosMonetarios, uid As Long, local As Long)
            _catalogos = catalogos
            _uid = uid
            _local = local
            Text = "Mantenimiento de Bancos"
            ClientSize = New Size(486, 154)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            gbDatos.Controls.AddRange(New Control() {
                New Label With {.Text = "Código", .AutoSize = True, .Location = New Point(12, 27)}, txtCodigo,
                New Label With {.Text = "Descripción", .AutoSize = True, .Location = New Point(12, 59)}, txtDescripcion
            })
            Controls.AddRange(New Control() {gbDatos, barra})
            barra.AlHacerClic = AddressOf Barra_Clic
            barra.Estado(BotonBarra.Borrar, False)
            txtDescripcion.ReadOnly = True
        End Sub

        Private Sub Mostrar(banco As BancoCatalogo)
            _actual = banco
            txtCodigo.Text = If(banco Is Nothing, "", banco.Codigo.ToString())
            txtDescripcion.Text = If(banco Is Nothing, "", banco.Descripcion)
        End Sub

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        txtCodigo.Text = (Await _catalogos.SiguienteBancoAsync()).ToString()
                        txtDescripcion.Clear()
                        txtDescripcion.ReadOnly = False
                        txtDescripcion.Focus()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        txtDescripcion.ReadOnly = False
                        txtDescripcion.Focus()
                    Case BotonBarra.Guardar
                        If txtDescripcion.Text.Trim().Length = 0 Then
                            MessageBox.Show(Me, "La descripción es un dato requerido.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                            txtDescripcion.Focus()
                            Return True
                        End If
                        Dim banco As New BancoCatalogo With {.Codigo = CInt(Vb6.Val(txtCodigo.Text)), .Descripcion = txtDescripcion.Text.Trim()}
                        Await _catalogos.GuardarBancoAsync(banco, _uid, _local)
                        Mostrar(banco)
                        txtDescripcion.ReadOnly = True
                    Case BotonBarra.Cancelar
                        Mostrar(_actual)
                        txtDescripcion.ReadOnly = True
                    Case BotonBarra.Buscar
                        Using datos = Await _catalogos.BuscarBancosAsync()
                            Using buscador As New FrmBuscador("Búsqueda de bancos", datos)
                                If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                                    Mostrar(Await _catalogos.ObtenerBancoAsync(CInt(Vb6.Val(buscador.Resultado(0)))))
                                End If
                            End Using
                        End Using
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "BANCOS", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

    End Class

End Namespace
