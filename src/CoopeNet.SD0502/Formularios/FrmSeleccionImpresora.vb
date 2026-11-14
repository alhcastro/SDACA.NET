Imports System.Drawing.Printing
Imports Microsoft.Win32
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Elige la impresora de un documento del POS. Migración de SD0502\FACPOS\frmSelPrinters.frm: guarda la impresora
    ''' en el CONFIG.INI de CoopeNet y, para contado y crédito, su tipo (EPSON, SAMSUNG u OTRA).
    ''' </summary>
    Public Class FrmSeleccionImpresora
        Inherits Form

        Private ReadOnly _configIni As String
        Private ReadOnly _documento As DocumentoImpresion

        Private ReadOnly cmbImpresoras As New ComboBox With {.Name = "cmbImpresoras", .AccessibleName = "Impresora", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 18), .Width = 300}
        Private ReadOnly btnAceptar As New Button With {.Name = "btnGuardar", .Text = "&Aceptar", .Location = New Point(204, 58), .Size = New Size(90, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .Location = New Point(300, 58), .Size = New Size(90, 30)}

        ''' <summary>La impresora elegida, después de Aceptar.</summary>
        Public ReadOnly Property Impresora As String = ""

        Public Sub New(configIni As String, documento As DocumentoImpresion)
            _configIni = configIni
            _documento = documento
            Text = "Seleccionar impresora"
            ClientSize = New Size(402, 100)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            AcceptButton = btnAceptar
            CancelButton = btnCancelar
            Controls.AddRange(New Control() {New Label With {.Text = "Impresora", .AutoSize = True, .Location = New Point(12, 21)}, cmbImpresoras, btnAceptar, btnCancelar})

            For Each nombre As String In PrinterSettings.InstalledPrinters
                cmbImpresoras.Items.Add(nombre)
            Next
            Dim actual = ImpresorasPos.Impresora(configIni, documento, PredeterminadaDelSistema())
            Dim i = cmbImpresoras.Items.Cast(Of String)().ToList().FindIndex(Function(p) String.Equals(p, actual, StringComparison.OrdinalIgnoreCase))
            If i >= 0 Then cmbImpresoras.SelectedIndex = i Else If cmbImpresoras.Items.Count > 0 Then cmbImpresoras.SelectedIndex = 0
            btnAceptar.Enabled = cmbImpresoras.Items.Count > 0

            AddHandler btnAceptar.Click, AddressOf Aceptar
            AddHandler btnCancelar.Click, Sub() Close()
        End Sub

        ''' <summary>Printer.DeviceName de VB6: la impresora predeterminada de Windows.</summary>
        Public Shared Function PredeterminadaDelSistema() As String
            Return If(New PrinterSettings().PrinterName, "")
        End Function

        ''' <summary>
        ''' SD_TipoPrint: VB6 consultaba el controlador por WMI; aquí se lee del registro de Windows, donde está el mismo
        ''' nombre. Una impresora de red que no aparece ahí queda como OTRA, igual que una sin controlador conocido.
        ''' </summary>
        Public Shared Function TipoImpresora(nombre As String) As String
            Try
                Using llave = Registry.LocalMachine.OpenSubKey($"SYSTEM\CurrentControlSet\Control\Print\Printers\{nombre}")
                    Return ImpresorasPos.TipoPorControlador(TryCast(llave?.GetValue("Printer Driver"), String))
                End Using
            Catch ex As Exception When TypeOf ex Is Security.SecurityException OrElse TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is ArgumentException
                Return "OTRA"
            End Try
        End Function

        Private Sub Aceptar(sender As Object, e As EventArgs)
            Dim nombre = TryCast(cmbImpresoras.SelectedItem, String)
            If String.IsNullOrEmpty(nombre) Then Return
            Try
                ImpresorasPos.GuardarImpresora(_configIni, _documento, nombre, TipoImpresora(nombre))
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                MessageBox.Show(Me, $"No se pudo guardar en {_configIni}.{Environment.NewLine}{ex.Message}", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End Try
            _Impresora = nombre
            DialogResult = DialogResult.OK
            Close()
        End Sub

    End Class

End Namespace
