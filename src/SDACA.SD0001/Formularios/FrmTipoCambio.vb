Imports System.Globalization
Imports MySqlConnector
Imports SDACA.Core.Catalogos
Imports SDACA.Ui

Namespace SD0001

    ''' <summary>
    ''' Tipo de cambio. Migración de Fuentes\SD0001\FrmTipoCambio.frm, con la estructura real de
    ''' gattipocambio: uno por moneda y día, sin banco ni hora.
    ''' </summary>
    Public Class FrmTipoCambio
        Inherits Form

        Private ReadOnly _catalogos As CatalogosMonetarios
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long

        Private ReadOnly cmbMonedas As New ComboBox With {.Name = "cmbMonedas", .AccessibleName = "Moneda", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 16), .Width = 290}
        Private ReadOnly dtpFecha As New DateTimePicker With {.Name = "dtpFecha", .AccessibleName = "Fecha", .Format = DateTimePickerFormat.Short, .Location = New Point(90, 48), .Width = 150}
        Private ReadOnly txtCompra As New TextBox With {.Name = "txtCompra", .AccessibleName = "Compra", .TextAlign = HorizontalAlignment.Right, .Location = New Point(90, 80), .Width = 150}
        Private ReadOnly txtVenta As New TextBox With {.Name = "txtVenta", .AccessibleName = "Venta", .TextAlign = HorizontalAlignment.Right, .Location = New Point(90, 112), .Width = 150}
        Private ReadOnly btnBuscar As New Button With {.Name = "btnBuscar", .Text = "&Buscar", .Location = New Point(12, 150), .Size = New Size(86, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnNuevo As New Button With {.Name = "btnNuevo", .Text = "&Nuevo", .Location = New Point(104, 150), .Size = New Size(86, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnAplicar As New Button With {.Name = "btnGuardar", .Text = "&Aplicar", .Location = New Point(204, 150), .Size = New Size(86, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(296, 150), .Size = New Size(86, 30), .UseVisualStyleBackColor = True}

        Public Sub New(catalogos As CatalogosMonetarios, uid As Long, local As Long)
            _catalogos = catalogos
            _uid = uid
            _local = local
            Text = "Tipo de Cambio"
            ClientSize = New Size(394, 192)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir
            dtpFecha.Value = Date.Today
            Controls.AddRange(New Control() {
                Etiqueta("Moneda", 19), cmbMonedas, Etiqueta("Fecha", 51), dtpFecha, Etiqueta("Compra", 83), txtCompra, Etiqueta("Venta", 115), txtVenta, btnBuscar, btnNuevo, btnAplicar, btnSalir
            })
            AddHandler Load, AddressOf Cargar
            AddHandler cmbMonedas.SelectedIndexChanged, Sub() MostrarTipo(Nothing)
            AddHandler btnBuscar.Click, AddressOf Buscar
            AddHandler btnNuevo.Click, AddressOf Nuevo
            AddHandler btnAplicar.Click, AddressOf Aplicar
            AddHandler btnSalir.Click, Sub() Close()
        End Sub

        Private Shared Function Etiqueta(texto As String, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(12, y)}
        End Function

        Private ReadOnly Property Moneda As MonedaCatalogo
            Get
                Return TryCast(cmbMonedas.SelectedItem, MonedaCatalogo)
            End Get
        End Property

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                cmbMonedas.Items.AddRange((Await _catalogos.ListarMonedasExtranjerasAsync()).ToArray())
                If cmbMonedas.Items.Count > 0 Then cmbMonedas.SelectedIndex = 0
                cmbMonedas.Enabled = cmbMonedas.Items.Count > 1
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub MostrarTipo(fecha As Date?)
            If Moneda Is Nothing Then Return
            Try
                Dim tipo = Await _catalogos.ObtenerTipoCambioAsync(Moneda.Codigo, fecha)
                txtCompra.Text = If(tipo Is Nothing, 0.0, tipo.Compra).ToString("#,##0.00")
                txtVenta.Text = If(tipo Is Nothing, 0.0, tipo.Venta).ToString("#,##0.00")
                If tipo IsNot Nothing Then dtpFecha.Value = tipo.Fecha
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Nuevo(sender As Object, e As EventArgs)
            dtpFecha.Value = Date.Today
            txtCompra.Text = "0.00"
            txtVenta.Text = "0.00"
            txtCompra.Focus()
        End Sub

        Private Async Sub Buscar(sender As Object, e As EventArgs)
            If Moneda Is Nothing Then Return
            Try
                Using datos = Await _catalogos.BuscarTiposCambioAsync(Moneda.Codigo)
                    Using buscador As New FrmBuscador("Buscar Tipo de Cambio", datos)
                        Dim fecha As Date
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso
                           Date.TryParseExact(buscador.Resultado(0), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, fecha) Then MostrarTipo(fecha)
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Shared Function Numero(caja As TextBox) As Double
            Dim valor As Double
            Return If(Double.TryParse(caja.Text, NumberStyles.Number, CultureInfo.CurrentCulture, valor), valor, 0)
        End Function

        Private Async Sub Aplicar(sender As Object, e As EventArgs)
            If Moneda Is Nothing Then Return
            If MessageBox.Show(Me, "¿Aplicar el tipo de cambio?", Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Try
                Await _catalogos.GuardarTipoCambioAsync(New TipoCambio With {.Moneda = Moneda.Codigo, .Fecha = dtpFecha.Value.Date, .Compra = Numero(txtCompra), .Venta = Numero(txtVenta)}, _uid, _local)
                MessageBox.Show(Me, "Tipo de cambio aplicado.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "TIPO DE CAMBIO", Modulo.TituloModulo)
            End Try
        End Sub

    End Class

End Namespace
