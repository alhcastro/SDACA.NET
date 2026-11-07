Imports System.Diagnostics
Imports MySqlConnector
Imports CoopeNet.Core.Datos
Imports CoopeNet.Ui

Namespace SD0001

    ''' <summary>
    ''' Reparar BD. Migración de Fuentes\SD0001\frmRepararT.frm. Con Reparar, corre REPAIR TABLE en cada tabla
    ''' marcada, igual que VB6. Con MyIsamCHK, arma las líneas de myisamchk, que se pueden corregir a mano, y
    ''' las corre en este equipo: solo sirve en el servidor y con MySQL detenido. A diferencia de VB6, pide
    ''' confirmación antes de empezar.
    ''' </summary>
    Public Class FrmRepararTablas
        Inherits Form

        Private ReadOnly _repo As ReparacionTablas
        Private _ocupado As Boolean
        Private _marcando As Boolean

        Private ReadOnly txtFiltro As New TextBox With {.Name = "txtFiltro", .AccessibleName = "Filtro de tablas", .Text = ReparacionTablas.Todas, .Location = New Point(12, 30), .Width = 300}
        Private ReadOnly chkMarcar As New CheckBox With {.Name = "chkMarcar", .Text = "Marcar todas", .AutoSize = True, .Location = New Point(12, 60)}
        Private ReadOnly lstTablas As New CheckedListBox With {.Name = "lstTablas", .AccessibleName = "Tablas", .CheckOnClick = True, .IntegralHeight = False, .Location = New Point(12, 86), .Size = New Size(300, 466),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left}
        Private ReadOnly optReparar As New RadioButton With {.Name = "optReparar", .Text = "Reparar", .AutoSize = True, .Checked = True, .Location = New Point(330, 12)}
        Private ReadOnly optMyIsamChk As New RadioButton With {.Name = "optMyIsamChk", .Text = "MyIsamCHK", .AutoSize = True, .Location = New Point(430, 12)}
        Private ReadOnly lblDirectorio As New Label With {.Text = "Directorio", .AutoSize = True, .Location = New Point(330, 45)}
        Private ReadOnly txtDirectorio As New TextBox With {.Name = "txtDirectorio", .AccessibleName = "Directorio de la base", .Location = New Point(410, 42), .Width = 440, .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right}
        Private ReadOnly btnDirectorio As New Button With {.Name = "btnDirectorio", .Text = "...", .AccessibleName = "Elegir directorio", .Location = New Point(856, 40), .Size = New Size(32, 26), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
        Private ReadOnly chkRecuperar As New CheckBox With {.Name = "chkRecuperar", .Location = New Point(330, 72), .Size = New Size(558, 36), .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .Text = "-r: realiza una reparación que puede resolver casi cualquier problema, excepto las claves únicas que no son únicas. Si quiere recuperar una tabla, esta es la primera opción a intentar."}
        Private ReadOnly chkRapido As New CheckBox With {.Name = "chkRapido", .Location = New Point(330, 110), .Size = New Size(558, 22), .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .Text = "-q: consigue una reparación más rápida al no modificar el archivo de datos."}
        Private ReadOnly txtResultado As New TextBox With {.Name = "txtResultado", .AccessibleName = "Resultado", .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Both, .WordWrap = False,
            .Location = New Point(330, 140), .Size = New Size(558, 412), .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}
        Private ReadOnly btnReparar As New Button With {.Name = "btnReparar", .Text = "&Reparar", .Location = New Point(692, 562), .Size = New Size(96, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .DialogResult = DialogResult.Cancel, .Location = New Point(792, 562), .Size = New Size(96, 30), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}
        Private ReadOnly lblConteo As New Label With {.Name = "lblConteo", .AutoSize = True, .Location = New Point(12, 568), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left}
        Private ReadOnly tmrFiltro As New Timer With {.Interval = 400}

        Public Sub New(repositorio As ReparacionTablas)
            _repo = repositorio
            Text = "Utilidad para el mantenimiento de tablas"
            ClientSize = New Size(900, 604)
            MinimumSize = New Size(760, 480)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir
            Controls.AddRange(New Control() {
                New Label With {.Text = "Tablas", .AutoSize = True, .Location = New Point(12, 12)}, txtFiltro, chkMarcar, lstTablas, lblConteo,
                optReparar, optMyIsamChk, lblDirectorio, txtDirectorio, btnDirectorio, chkRecuperar, chkRapido, txtResultado, btnReparar, btnSalir
            })
            AddHandler Load, AddressOf Cargar
            AddHandler FormClosing, Sub(s, e) e.Cancel = _ocupado
            AddHandler txtFiltro.TextChanged, Sub()
                                                  tmrFiltro.Stop()
                                                  tmrFiltro.Start()
                                              End Sub
            AddHandler tmrFiltro.Tick, AddressOf Filtro_Tiempo
            AddHandler chkMarcar.CheckedChanged, AddressOf Marcar_Cambio
            AddHandler lstTablas.ItemCheck, Sub() If Not _marcando Then BeginInvoke(New Action(AddressOf Comandos))
            AddHandler optReparar.CheckedChanged, Sub() Modo()
            AddHandler txtDirectorio.TextChanged, Sub() Comandos()
            AddHandler chkRecuperar.CheckedChanged, Sub() Comandos()
            AddHandler chkRapido.CheckedChanged, Sub() Comandos()
            AddHandler btnDirectorio.Click, AddressOf Directorio_Clic
            AddHandler btnReparar.Click, AddressOf Reparar_Clic
            Modo()
        End Sub

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            txtResultado.Font = New Font(FontFamily.GenericMonospace, 9.0F)
            Try
                txtDirectorio.Text = Await _repo.CarpetaDatosAsync()
                Await ListarAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Function ListarAsync() As Task
            Dim tablas = Await _repo.TablasAsync(txtFiltro.Text)
            _marcando = True
            lstTablas.Items.Clear()
            lstTablas.Items.AddRange(tablas.ToArray())
            chkMarcar.Checked = False
            _marcando = False
            lblConteo.Text = $"{tablas.Count} tablas"
            Comandos()
        End Function

        Private Async Sub Filtro_Tiempo(sender As Object, e As EventArgs)
            tmrFiltro.Stop()
            Try
                Await ListarAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Marcar_Cambio(sender As Object, e As EventArgs)
            If _marcando Then Return
            _marcando = True
            lstTablas.BeginUpdate()
            For i = 0 To lstTablas.Items.Count - 1
                lstTablas.SetItemChecked(i, chkMarcar.Checked)
            Next
            lstTablas.EndUpdate()
            _marcando = False
            Comandos()
        End Sub

        Private Function Marcadas() As List(Of TablaBase)
            Return lstTablas.CheckedItems.Cast(Of TablaBase)().ToList()
        End Function

        Private Sub Modo()
            Dim myisamchk = optMyIsamChk.Checked
            For Each c As Control In New Control() {lblDirectorio, txtDirectorio, btnDirectorio, chkRecuperar, chkRapido}
                c.Visible = myisamchk
            Next
            txtResultado.ReadOnly = Not myisamchk
            txtResultado.Clear()
            Comandos()
        End Sub

        ''' <summary>RepMyIs: en modo MyIsamCHK el cuadro muestra las líneas que se van a correr.</summary>
        Private Sub Comandos()
            If Not optMyIsamChk.Checked OrElse _ocupado Then Return
            txtResultado.Lines = ReparacionTablas.ComandosMyIsamChk(txtDirectorio.Text, Marcadas().Select(Function(t) t.Nombre), chkRecuperar.Checked, chkRapido.Checked).ToArray()
        End Sub

        Private Sub Directorio_Clic(sender As Object, e As EventArgs)
            Using dialogo As New FolderBrowserDialog With {.Description = "Seleccione el directorio de la BD.", .UseDescriptionForTitle = True}
                If IO.Directory.Exists(txtDirectorio.Text) Then dialogo.InitialDirectory = txtDirectorio.Text
                If dialogo.ShowDialog(Me) = DialogResult.OK Then txtDirectorio.Text = dialogo.SelectedPath
            End Using
        End Sub

        Private Sub Ocupado(valor As Boolean)
            _ocupado = valor
            UseWaitCursor = valor
            For Each c As Control In New Control() {txtFiltro, chkMarcar, lstTablas, optReparar, optMyIsamChk, txtDirectorio, btnDirectorio, chkRecuperar, chkRapido, btnReparar, btnSalir}
                c.Enabled = Not valor
            Next
            txtResultado.ReadOnly = valor OrElse optReparar.Checked
        End Sub

        Private Sub Escribir(linea As String)
            txtResultado.AppendText(linea & Environment.NewLine)
        End Sub

        Private Async Sub Reparar_Clic(sender As Object, e As EventArgs)
            If optMyIsamChk.Checked Then
                Await CorrerMyIsamChkAsync()
            Else
                Await RepararAsync()
            End If
        End Sub

        Private Async Function RepararAsync() As Task
            Dim tablas = Marcadas()
            If tablas.Count = 0 Then Return
            Dim pregunta = $"Se repararán {tablas.Count} tablas. MySQL bloquea cada tabla mientras la repara, y en las tablas grandes puede tardar.{Environment.NewLine}¿Desea continuar?"
            If MessageBox.Show(Me, pregunta, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Ocupado(True)
            txtResultado.Clear()
            Try
                For Each t In tablas
                    Escribir($"Reparando la tabla: {t.Nombre}...")
                    Try
                        For Each fila In Await _repo.RepararAsync(t.Nombre)
                            Escribir(fila.ToString())
                        Next
                    Catch ex As MySqlException
                        Escribir($"ERROR: {ex.Message}")
                    End Try
                    Escribir("")
                Next
                Escribir("Terminado.")
            Finally
                Ocupado(False)
            End Try
        End Function

        Private Async Function CorrerMyIsamChkAsync() As Task
            Dim lineas = txtResultado.Lines.Select(Function(l) l.Trim()).Where(Function(l) l.Length > 0).ToList()
            If lineas.Count = 0 Then Return
            Dim pregunta = $"Se correrán {lineas.Count} comandos de myisamchk en este equipo. Solo funcionan en el servidor de la base de datos, " &
                           $"y MySQL debe estar detenido: si está en uso, myisamchk puede dañar las tablas.{Environment.NewLine}¿Desea continuar?"
            If MessageBox.Show(Me, pregunta, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
            Ocupado(True)
            txtResultado.Clear()
            Try
                For Each linea In lineas
                    Escribir(linea)
                    Escribir(Await CorrerAsync(linea))
                Next
                Escribir("Terminado.")
            Finally
                Ocupado(False)
            End Try
        End Function

        ''' <summary>Como ShellAndWait, pero devuelve lo que el comando escribió.</summary>
        Private Shared Async Function CorrerAsync(linea As String) As Task(Of String)
            Dim inicio As New ProcessStartInfo("cmd.exe", $"/c {linea}") With {
                .UseShellExecute = False, .CreateNoWindow = True, .RedirectStandardOutput = True, .RedirectStandardError = True
            }
            Try
                Using proceso = Process.Start(inicio)
                    Dim salida = proceso.StandardOutput.ReadToEndAsync()
                    Dim errores = proceso.StandardError.ReadToEndAsync()
                    Await proceso.WaitForExitAsync()
                    Return $"{Await salida}{Await errores}".TrimEnd() & Environment.NewLine
                End Using
            Catch ex As ComponentModel.Win32Exception
                Return $"ERROR: {ex.Message}{Environment.NewLine}"
            End Try
        End Function

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then tmrFiltro.Dispose()
            MyBase.Dispose(disposing)
        End Sub

    End Class

End Namespace
