Imports SDACA.Ui

Namespace SD1401

    Partial Public Class FrmCuentasBancarias

        Private WithEvents cmbLocal As ComboBox
        Private gbDatos As GroupBox
        Private cmbBancos As ComboBox
        Private cmbMonedas As ComboBox
        Private WithEvents txtCodigo As TextBox
        Private txtDescripcion As TextBox
        Private cmbTipoCuenta As ComboBox
        Private txtCuentaBanco As TextBox
        Private txtNumCliente As TextBox
        Private WithEvents txtMontoInicial As TextBox
        Private WithEvents txtChequesTransito As TextBox
        Private WithEvents txtDepositosTransito As TextBox
        Private txtSaldo As TextBox
        Private tabs As TabControl
        Private WithEvents chkCheques As CheckBox
        Private txtNumCheque As TextBox
        Private chkConta As CheckBox
        Private WithEvents txtContable As TextBox
        Private WithEvents btnBuscarContable As Button
        Private txtDetalleContable As TextBox
        Private WithEvents txtConversion As TextBox
        Private WithEvents btnBuscarConversion As Button
        Private txtDetalleConversion As TextBox
        Private WithEvents btnDepartamentos As Button
        Private WithEvents btnUsuarios As Button
        Private barra As BarraComandos

        Private Sub InitializeComponent()
            cmbLocal = Lista("cmbLocal", "Departamento", 110, 10, 400)

            cmbBancos = Lista("cmbBancos", "Banco", 110, 24, 240)
            cmbMonedas = Lista("cmbMonedas", "Moneda", 440, 24, 180)
            txtCodigo = Caja("txtCodigo", "Número de cuenta", 110, 56, 240, 16)
            cmbTipoCuenta = Lista("cmbTipoCuenta", "Tipo de cuenta", 440, 56, 180)
            cmbTipoCuenta.Items.AddRange(New Object() {"Ahorros", "Corriente", "Caja Chica"})
            txtDescripcion = Caja("txtDescripcion", "Descripción", 110, 88, 510, 64)
            txtCuentaBanco = Caja("txtCuentaBanco", "Cuenta del banco", 110, 120, 240, 32)
            txtNumCliente = Caja("txtNumCliente", "Número de cliente", 440, 120, 180, 32)
            txtMontoInicial = Importe("txtMontoInicial", "Monto inicial", 110, 152)
            txtChequesTransito = Importe("txtChequesTransito", "Cheques en tránsito", 440, 152)
            txtDepositosTransito = Importe("txtDepositosTransito", "Depósitos en tránsito", 110, 184)
            txtSaldo = Importe("txtSaldo", "Saldo inicial", 440, 184)
            txtSaldo.ReadOnly = True
            txtSaldo.TabStop = False

            gbDatos = New GroupBox With {.Name = "gbDatos", .Text = "Datos de la Cuenta", .Location = New Point(10, 40), .Size = New Size(636, 220)}
            gbDatos.Controls.AddRange(New Control() {
                Etiqueta("Banco", 12, 27), cmbBancos, Etiqueta("Monedas", 366, 27), cmbMonedas,
                Etiqueta("# de Cuenta", 12, 59), txtCodigo, Etiqueta("Tipo", 366, 59), cmbTipoCuenta,
                Etiqueta("Descripción", 12, 91), txtDescripcion,
                Etiqueta("Cuenta Banco", 12, 123), txtCuentaBanco, Etiqueta("# de Cliente", 366, 123), txtNumCliente,
                Etiqueta("Monto Inicial", 12, 155), txtMontoInicial, Etiqueta("Ch. Tránsito", 366, 155), txtChequesTransito,
                Etiqueta("Dep. Tránsito", 12, 187), txtDepositosTransito, Etiqueta("Saldo Inicial", 366, 187), txtSaldo
            })

            tabs = New Pestanas With {.Name = "tabs", .Location = New Point(10, 266), .Size = New Size(636, 138)}
            Dim consecutivo As New TabPage("Consecutivo")
            chkCheques = New CheckBox With {.Name = "chkCheques", .Text = "La cuenta puede imprimir cheques", .AutoSize = True, .Location = New Point(12, 14)}
            txtNumCheque = Caja("txtNumCheque", "Consecutivo de cheques", 170, 44, 120, 11)
            consecutivo.Controls.AddRange(New Control() {chkCheques, Etiqueta("Consecutivo de Cheques", 12, 47), txtNumCheque})

            Dim contable As New TabPage("Cuenta Contable")
            chkConta = New CheckBox With {.Name = "chkConta", .Text = "Llevar información contable", .AutoSize = True, .Location = New Point(12, 10)}
            txtContable = Caja("txtContable", "Cuenta contable", 122, 36, 150, 16)
            btnBuscarContable = Boton("...", "btnBuscarContable", 276, 35, 30, 25, "Buscar cuenta contable")
            txtDetalleContable = Caja("txtDetalleContable", "Descripción contable", 122, 66, 480, 128)
            txtDetalleContable.ReadOnly = True
            txtDetalleContable.TabStop = False
            contable.Controls.AddRange(New Control() {
                chkConta, Etiqueta("Cuenta Contable", 12, 39), txtContable, btnBuscarContable, Etiqueta("Descripción", 12, 69), txtDetalleContable
            })

            Dim conversion As New TabPage("Cuenta Conversion")
            txtConversion = Caja("txtConversion", "Cuenta de conversión", 122, 16, 150, 16)
            btnBuscarConversion = Boton("...", "btnBuscarConversion", 276, 15, 30, 25, "Buscar cuenta de conversión")
            txtDetalleConversion = Caja("txtDetalleConversion", "Descripción de conversión", 122, 46, 480, 128)
            txtDetalleConversion.ReadOnly = True
            txtDetalleConversion.TabStop = False
            conversion.Controls.AddRange(New Control() {
                Etiqueta("Cuenta Contable", 12, 19), txtConversion, btnBuscarConversion, Etiqueta("Descripción", 12, 49), txtDetalleConversion
            })
            tabs.TabPages.AddRange({consecutivo, contable, conversion})

            btnDepartamentos = Boton("Cuenta x Departamento", "btnDepartamentos", 10, 412, 160, 32, "")
            btnUsuarios = Boton("Usuarios", "btnUsuarios", 176, 412, 90, 32, "")
            barra = New BarraComandos With {.Name = "barra", .Location = New Point(276, 412), .Size = New Size(370, 32)}

            SuspendLayout()
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(656, 454)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            Text = "Cuentas Bancarias"
            Controls.AddRange(New Control() {Etiqueta("Departamento", 12, 13), cmbLocal, gbDatos, tabs, btnDepartamentos, btnUsuarios, barra})
            ResumeLayout(False)
        End Sub

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Shared Function Caja(nombre As String, accesible As String, x As Integer, y As Integer, ancho As Integer, maximo As Integer) As TextBox
            Return New TextBox With {.Name = nombre, .AccessibleName = accesible, .Location = New Point(x, y), .Width = ancho, .MaxLength = maximo}
        End Function

        Private Shared Function Importe(nombre As String, accesible As String, x As Integer, y As Integer) As TextBox
            Dim caja = FrmCuentasBancarias.Caja(nombre, accesible, x, y, 180, 20)
            caja.TextAlign = HorizontalAlignment.Right
            Return caja
        End Function

        Private Shared Function Lista(nombre As String, accesible As String, x As Integer, y As Integer, ancho As Integer) As ComboBox
            Return New ComboBox With {.Name = nombre, .AccessibleName = accesible, .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(x, y), .Width = ancho}
        End Function

        Private Shared Function Boton(texto As String, nombre As String, x As Integer, y As Integer, ancho As Integer, alto As Integer, accesible As String) As Button
            Return New Button With {
                .Text = texto, .Name = nombre, .AccessibleName = If(accesible.Length > 0, accesible, texto), .Location = New Point(x, y), .Size = New Size(ancho, alto), .UseVisualStyleBackColor = True
            }
        End Function

    End Class

End Namespace
