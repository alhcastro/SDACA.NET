Imports System.Drawing.Printing
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Vista previa de un texto para imprimir. Migración de SD0502\FACPOS\frmVistaPrevia.frm: se elige la impresora
    ''' (se recuerda en CONFIG.INI, ULTIMA_IMPR), se guarda el texto en un archivo o se imprime. En una impresora de
    ''' tiquete (papel de menos de 75 mm) va crudo, con sus códigos y el corte; en las demás, en Courier New 8 negrita.
    ''' </summary>
    Public Class FrmVistaPreviaTexto
        Inherits Form

        Private Const AnchoTiquete As Integer = 295 ' 75 mm en centésimas de pulgada.

        Private ReadOnly _texto As String
        Private ReadOnly _configIni As String
        Private _restante As String = ""

        Private ReadOnly txtPrevia As New TextBox With {
            .Name = "txtPrevia", .AccessibleName = "Texto a imprimir", .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Both, .WordWrap = False,
            .Font = New Font("Consolas", 9.75F), .Location = New Point(12, 12), .Size = New Size(560, 420), .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}
        Private ReadOnly cmbImpresora As New ComboBox With {.Name = "cmbImpresora", .AccessibleName = "Imprime en", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 446), .Width = 250, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left}
        Private ReadOnly btnImprimir As New Button With {.Name = "btnGuardar", .Text = "&Imprimir", .Location = New Point(352, 442), .Size = New Size(70, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}
        Private ReadOnly btnArchivo As New Button With {.Name = "btnArchivo", .Text = "&Guardar", .Location = New Point(428, 442), .Size = New Size(70, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(504, 442), .Size = New Size(68, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}

        Public Sub New(texto As String, configIni As String, Optional titulo As String = "Vista previa")
            _texto = If(texto, "")
            _configIni = configIni
            Text = titulo
            ClientSize = New Size(584, 484)
            ShowInTaskbar = False
            MinimizeBox = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir
            txtPrevia.Text = ImpresionFormato.ParaPantalla(_texto)
            Controls.AddRange(New Control() {txtPrevia, New Label With {.Text = "Imprime en", .AutoSize = True, .Location = New Point(12, 449), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left}, cmbImpresora, btnImprimir, btnArchivo, btnSalir})
            Ventanas.Recordar(Me)

            For Each nombre As String In PrinterSettings.InstalledPrinters
                cmbImpresora.Items.Add(nombre)
            Next
            Dim ultima = ConexionesIni.LeerValor(configIni, "POS", "ULTIMA_IMPR")
            If ultima = "" Then ultima = FrmSeleccionImpresora.PredeterminadaDelSistema()
            Dim i = cmbImpresora.Items.Cast(Of String)().ToList().FindIndex(Function(p) String.Equals(p, ultima, StringComparison.OrdinalIgnoreCase))
            If i >= 0 Then cmbImpresora.SelectedIndex = i Else If cmbImpresora.Items.Count > 0 Then cmbImpresora.SelectedIndex = 0
            btnImprimir.Enabled = cmbImpresora.Items.Count > 0

            AddHandler btnImprimir.Click, AddressOf Imprimir_Clic
            AddHandler btnArchivo.Click, AddressOf Archivo_Clic
            AddHandler btnSalir.Click, Sub() Close()
            AddHandler FormClosing, AddressOf RecordarImpresora
            ' Un TextBox de solo lectura con el foco selecciona todo al abrir.
            AddHandler Shown, Sub()
                                  txtPrevia.SelectionStart = 0
                                  txtPrevia.SelectionLength = 0
                                  btnImprimir.Focus()
                              End Sub
        End Sub

        Private Sub RecordarImpresora(sender As Object, e As FormClosingEventArgs)
            Dim nombre = TryCast(cmbImpresora.SelectedItem, String)
            If String.IsNullOrEmpty(nombre) Then Return
            Try
                ConexionesIni.FijarValor(_configIni, "POS", "ULTIMA_IMPR", nombre)
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                ' Sin permiso sobre CONFIG.INI solo se pierde el recuerdo de la impresora.
            End Try
        End Sub

        ''' <summary>cmdGuardar: el texto como se ve, en un archivo.</summary>
        Private Sub Archivo_Clic(sender As Object, e As EventArgs)
            Using dialogo As New SaveFileDialog With {.Title = "Guardar texto", .Filter = "Texto|*.txt|Todos los archivos|*.*", .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                IO.File.WriteAllText(dialogo.FileName, txtPrevia.Text & vbCrLf, PropiedadesFacturacionRepositorio.CodificacionArchivo)
            End Using
        End Sub

        ''' <summary>cmdImprime.</summary>
        Private Sub Imprimir_Clic(sender As Object, e As EventArgs)
            Dim nombre = TryCast(cmbImpresora.SelectedItem, String)
            If String.IsNullOrEmpty(nombre) Then Return
            Try
                Using documento As New PrintDocument()
                    documento.PrinterSettings.PrinterName = nombre
                    If documento.DefaultPageSettings.PaperSize.Width < AnchoTiquete Then
                        If Not ImpresionDirecta.ImprimirTiquete(_texto, nombre) Then
                            MessageBox.Show(Me, $"La impresora {nombre} no es reconocida.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End If
                        Return
                    End If
                    _restante = ImpresionFormato.ParaPantalla(_texto).Replace(vbCr, "")
                    documento.DocumentName = Text
                    AddHandler documento.PrintPage, AddressOf Pagina
                    documento.Print()
                End Using
            Catch ex As Exception When TypeOf ex Is InvalidPrinterException OrElse TypeOf ex Is ComponentModel.Win32Exception
                MessageBox.Show(Me, $"No se pudo imprimir.{Environment.NewLine}{ex.Message}", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
        End Sub

        ''' <summary>Printer.Print en Courier New 8 negrita, después de una línea en blanco.</summary>
        Private Sub Pagina(sender As Object, e As PrintPageEventArgs)
            Using letra As New Font("Courier New", 8.0F, FontStyle.Bold)
                Dim alto = letra.GetHeight(e.Graphics)
                Dim y As Single = e.MarginBounds.Top + alto
                Dim lineas = _restante.Split(ChrW(10))
                Dim i = 0
                While i < lineas.Length AndAlso y + alto <= e.MarginBounds.Bottom
                    e.Graphics.DrawString(lineas(i), letra, Brushes.Black, e.MarginBounds.Left, y)
                    y += alto
                    i += 1
                End While
                _restante = String.Join(ChrW(10), lineas.Skip(i))
                e.HasMorePages = i < lineas.Length
            End Using
        End Sub

    End Class

End Namespace
