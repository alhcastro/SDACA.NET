Imports System.IO
Imports MySqlConnector
Imports SDACA.Core
Imports SDACA.Core.Bancos
Imports SDACA.Core.Datos
Imports SDACA.Ui

Namespace SD1401

    ''' <summary>
    ''' Generar archivo de transferencia bancaria, privilegio 14012. Migración de
    ''' Fuentes\SD1401\FrmTransferenciasBancos.frm. Escribe el .ENV y registra el pago en el libro. No genera
    ''' sin formato o sin líneas (allá salen líneas vacías) y avisa antes de reemplazar un movimiento ya generado.
    ''' </summary>
    Public Class FrmGenerarTransferencia
        Inherits Form

        Private Const Extension As String = ".ENV"

        Private ReadOnly _transferencias As TransferenciasBancarias
        Private ReadOnly _parametros As Parametros
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _parametroCarpeta As String
        Private _consulta As Integer

        Private ReadOnly cmbDepartamento As New ComboBox With {.Name = "cmbDepartamento", .AccessibleName = "Departamento", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 14), .Width = 460}
        Private ReadOnly cmbModulo As New ComboBox With {.Name = "cmbModulo", .AccessibleName = "Módulo", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 46), .Width = 460}
        Private ReadOnly txtNumero As New TextBox With {.Name = "txtNumero", .AccessibleName = "Transferencia número", .Location = New Point(130, 78), .Width = 120}
        Private ReadOnly btnBuscar As New Button With {.Name = "btnBuscar", .Text = "&Buscar", .Location = New Point(258, 76), .Size = New Size(90, 28), .UseVisualStyleBackColor = True}
        Private ReadOnly lblResumen As New Label With {.Name = "lblResumen", .AutoSize = False, .Location = New Point(130, 110), .Size = New Size(460, 56)}
        Private ReadOnly txtCarpeta As New TextBox With {.Name = "txtCarpeta", .AccessibleName = "Carpeta de salida", .ReadOnly = True, .Location = New Point(130, 172), .Width = 370}
        Private ReadOnly btnCarpeta As New Button With {.Name = "btnCarpeta", .Text = "...", .AccessibleName = "Elegir carpeta", .Location = New Point(506, 171), .Size = New Size(36, 25), .UseVisualStyleBackColor = True}
        Private ReadOnly btnExplorar As New Button With {.Name = "btnExplorar", .Text = "Abrir", .Location = New Point(546, 171), .Size = New Size(44, 25), .UseVisualStyleBackColor = True}
        Private ReadOnly txtArchivo As New TextBox With {.Name = "txtArchivo", .AccessibleName = "Nombre del archivo", .Location = New Point(130, 204), .Width = 300}
        Private ReadOnly btnGenerar As New Button With {.Name = "btnGuardar", .Text = "&Generar archivo", .Location = New Point(350, 242), .Size = New Size(130, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(490, 242), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}

        Public Sub New(transferencias As TransferenciasBancarias, parametros As Parametros, uid As Long, local As Long, estacion As String)
            _transferencias = transferencias
            _parametros = parametros
            _uid = uid
            _local = local
            _parametroCarpeta = "CCB-PATH-" & estacion
            Text = "Transferencias Bancarias"
            ClientSize = New Size(604, 284)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir
            Controls.AddRange(New Control() {
                Etiqueta("Departamento", 17), cmbDepartamento, Etiqueta("Módulo", 49), cmbModulo, Etiqueta("Transferencia Nº", 81), txtNumero, btnBuscar,
                lblResumen, Etiqueta("Carpeta de salida", 175), txtCarpeta, btnCarpeta, btnExplorar, Etiqueta("Nombre del archivo", 207), txtArchivo,
                New Label With {.Text = Extension, .AutoSize = True, .Location = New Point(434, 207)}, btnGenerar, btnSalir
            })
            AddHandler Load, AddressOf Cargar
            AddHandler cmbDepartamento.SelectedIndexChanged, AddressOf Departamento_Cambio
            AddHandler cmbModulo.SelectedIndexChanged, Sub() Numero_Cambio()
            AddHandler txtNumero.TextChanged, Sub() Numero_Cambio()
            AddHandler btnBuscar.Click, AddressOf Buscar
            AddHandler btnCarpeta.Click, AddressOf ElegirCarpeta
            AddHandler btnExplorar.Click, Sub() If Directory.Exists(txtCarpeta.Text) Then Process.Start(New ProcessStartInfo("explorer.exe", $"""{txtCarpeta.Text}"""))
            AddHandler btnGenerar.Click, AddressOf Generar
            AddHandler btnSalir.Click, Sub() Close()
            AddHandler FormClosed, AddressOf GuardarCarpeta
        End Sub

        Private Shared Function Etiqueta(texto As String, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(12, y)}
        End Function

        Private ReadOnly Property Departamento As OpcionTransferencia
            Get
                Return TryCast(cmbDepartamento.SelectedItem, OpcionTransferencia)
            End Get
        End Property

        Private ReadOnly Property ModuloActual As OpcionTransferencia
            Get
                Return TryCast(cmbModulo.SelectedItem, OpcionTransferencia)
            End Get
        End Property

        Private ReadOnly Property Numero As Long
            Get
                Return Vb6.Val(txtNumero.Text)
            End Get
        End Property

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                Dim carpeta = (Await _parametros.ObtenerAsync(_local, _parametroCarpeta)).Replace("/", "\")
                txtCarpeta.Text = If(Directory.Exists(carpeta), carpeta, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                Dim departamentos = Await _transferencias.ListarLocalesPendientesAsync()
                cmbDepartamento.Items.AddRange(departamentos.ToArray())
                If cmbDepartamento.Items.Count > 0 Then
                    cmbDepartamento.SelectedIndex = 0
                Else
                    lblResumen.Text = "No hay transferencias pendientes de aplicar en ningún departamento."
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Departamento_Cambio(sender As Object, e As EventArgs)
            cmbModulo.Items.Clear()
            If Departamento Is Nothing Then Return
            Try
                Dim modulos = Await _transferencias.ListarModulosPendientesAsync(Departamento.Codigo)
                cmbModulo.Items.AddRange(modulos.ToArray())
                If cmbModulo.Items.Count > 0 Then cmbModulo.SelectedIndex = 0
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Numero_Cambio()
            _consulta += 1
            Dim consulta = _consulta
            lblResumen.Text = ""
            btnGenerar.Enabled = False
            If Departamento Is Nothing OrElse ModuloActual Is Nothing Then Return
            txtArchivo.Text = ArchivoTransferencia.NombreArchivo(Date.Today, Departamento.Codigo, ModuloActual.Codigo, txtNumero.Text)
            If Numero = 0 Then Return
            Try
                Dim resumen = Await _transferencias.ResumenAsync(Departamento.Codigo, ModuloActual.Codigo, Numero)
                If consulta <> _consulta Then Return
                If resumen Is Nothing Then
                    lblResumen.Text = "La transferencia no existe."
                    Return
                End If
                lblResumen.Text = $"{resumen.Concepto}{Environment.NewLine}Cuenta {resumen.Cuenta}, {resumen.Fecha:dd/MM/yyyy}{Environment.NewLine}{resumen.Lineas} líneas por un total de {resumen.Total:#,##0.00}"
                btnGenerar.Enabled = resumen.Lineas > 0
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub Buscar(sender As Object, e As EventArgs)
            If Departamento Is Nothing OrElse ModuloActual Is Nothing Then Return
            Try
                Using datos = Await _transferencias.BuscarPorModuloAsync(Departamento.Codigo, ModuloActual.Codigo)
                    Using buscador As New FrmBuscador("Seleccione la transferencia", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(3).Length > 0 Then txtNumero.Text = buscador.Resultado(3)
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub ElegirCarpeta(sender As Object, e As EventArgs)
            Using dialogo As New FolderBrowserDialog With {.Description = "Seleccione la carpeta de destino", .SelectedPath = txtCarpeta.Text}
                If dialogo.ShowDialog(Me) = DialogResult.OK Then txtCarpeta.Text = dialogo.SelectedPath
            End Using
        End Sub

        Private Function Pregunta(texto As String) As Boolean
            Return MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
        End Function

        Private Sub Aviso(texto As String, icono As MessageBoxIcon)
            MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.OK, icono)
        End Sub

        Private Async Sub Generar(sender As Object, e As EventArgs)
            If Departamento Is Nothing OrElse ModuloActual Is Nothing OrElse Numero = 0 OrElse txtArchivo.Text.Trim().Length = 0 Then Return
            If Not Pregunta("¿Desea generar el archivo de transferencia?") Then Return
            Dim codigoLocal = Departamento.Codigo
            Dim codigoModulo = ModuloActual.Codigo
            Dim numTra = Numero
            Dim ruta = Path.Combine(txtCarpeta.Text, txtArchivo.Text.Trim() & Extension)
            btnGenerar.Enabled = False
            Try
                If File.Exists(ruta) AndAlso Not Pregunta($"El archivo {ruta} ya existe. ¿Desea sobrescribirlo?") Then Return
                Dim contenido = Await _transferencias.ArmarAsync(codigoLocal, codigoModulo, numTra)
                If contenido Is Nothing OrElse contenido.Monto <= 0 Then
                    Aviso("No se generó el archivo: la cuenta de origen no tiene banco o formato configurado, o la transferencia no tiene líneas vigentes.", MessageBoxIcon.Warning)
                    Return
                End If
                Dim anterior = Await _transferencias.MovimientoExistenteAsync(codigoLocal, contenido, codigoModulo, numTra)
                If anterior IsNot Nothing Then
                    Dim estado = If(anterior.Conciliado, "CONCILIADO, y se perderá la conciliación", "sin conciliar")
                    If Not Pregunta($"Esta transferencia ya se generó el {anterior.Fecha:dd/MM/yyyy} por {anterior.Monto:#,##0.00}. Ese movimiento está {estado}.{Environment.NewLine}{Environment.NewLine}¿Desea reemplazarlo por uno de hoy?") Then Return
                End If
                File.WriteAllBytes(ruta, ArchivoTransferencia.Bytes(contenido.Lineas))
                Await _transferencias.RegistrarEnLibroAsync(codigoLocal, codigoModulo, numTra, contenido, _local, _uid, Date.Today)
                Aviso($"Archivo generado:{Environment.NewLine}{ruta}", MessageBoxIcon.Information)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "GENERANDO TRANSFERENCIA", Modulo.TituloModulo)
            Catch ex As IOException
                Aviso($"No se pudo escribir el archivo: {ex.Message}", MessageBoxIcon.Error)
            Catch ex As UnauthorizedAccessException
                Aviso($"No se pudo escribir el archivo: {ex.Message}", MessageBoxIcon.Error)
            Finally
                If Not IsDisposed Then btnGenerar.Enabled = True
            End Try
        End Sub

        Private Async Sub GuardarCarpeta(sender As Object, e As FormClosedEventArgs)
            If txtCarpeta.TextLength = 0 Then Return
            Try
                Await _parametros.GuardarAsync(_local, _parametroCarpeta, txtCarpeta.Text.Replace("\", "/"), _uid)
            Catch ex As MySqlException
                ' La carpeta es una comodidad: no vale la pena avisar al cerrar.
            End Try
        End Sub

    End Class

End Namespace
