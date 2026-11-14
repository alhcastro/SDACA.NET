Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Propiedades de facturación: parámetros de impresión y formatos de factura del POS. Migración de
    ''' SD0502\FACPOS\frmOpImpres.frm (Mantenimiento, opción 043 "Configurar impresión", privilegio 5072).
    ''' </summary>
    ''' <remarks>
    ''' La impresora y el formato de cada documento van al CONFIG.INI de CoopeNet (C:\ProgramData\CoopeNet), no al de
    ''' SDACA: los usará la facturación de CoopeNet, y el POS de VB6 sigue con los suyos. Los parámetros y los formatos
    ''' sí van a la base y el POS de VB6 los usa. Aún falta la vista de una factura con el formato (Ver, Imprimir y
    ''' Vista previa), que necesita el motor de impresión de facturas de FACPOS.
    ''' </remarks>
    Public Class FrmPropiedadesFacturacion
        Inherits Form

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture

        Private ReadOnly _repo As PropiedadesFacturacionRepositorio
        Private ReadOnly _variables As VariablesImpresionRepositorio
        Private ReadOnly _configIni As String
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _genero As String
        Private ReadOnly _empresa As String
        Private _formatoCargado As String = ""

        Private ReadOnly tabs As New TabControl With {.Name = "tabs", .Location = New Point(8, 8), .Size = New Size(824, 584), .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}
        ' El tamaño va antes que los hijos: sus anclajes se miden contra el de la pestaña.
        Private ReadOnly tabParametros As New TabPage("Parámetros de impresión") With {.Name = "tabParametros", .Size = New Size(816, 555)}
        Private ReadOnly tabFormatos As New TabPage("Formatos de impresión") With {.Name = "tabFormatos", .Size = New Size(816, 555)}

        ' ---- Parámetros ----
        Private ReadOnly gbImpresora As New GroupBox With {.Name = "gbImpresora", .Text = "Impresora y tipo de formato", .Location = New Point(10, 10), .Size = New Size(640, 124)}
        Private ReadOnly cmbDocumento As New ComboBox With {.Name = "cmbDocumento", .AccessibleName = "Documento", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(12, 24), .Width = 300}
        Private ReadOnly lblImpresora As New Label With {.Name = "lblImpresora", .AutoSize = False, .AutoEllipsis = True, .BorderStyle = BorderStyle.FixedSingle, .TextAlign = ContentAlignment.MiddleCenter, .Location = New Point(12, 88), .Size = New Size(612, 26)}
        Private ReadOnly btnSeleccionar As New Button With {.Name = "btnSeleccionar", .Text = "&Seleccionar impresora", .Location = New Point(12, 54), .Size = New Size(170, 28)}
        Private ReadOnly cmbFormatoDocumento As New ComboBox With {.Name = "cmbFormatoDocumento", .AccessibleName = "Formato del documento", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(260, 56), .Width = 220}
        Private ReadOnly btnGuardarImpresora As New Button With {.Name = "btnGuardarImpresora", .Text = "Guardar formato", .Location = New Point(492, 54), .Size = New Size(132, 28)}
        Private ReadOnly btnModificar As New Button With {.Name = "btnModificar", .Text = "&Modificar", .Location = New Point(664, 18), .Size = New Size(140, 34)}
        Private ReadOnly btnGuardar As New Button With {.Name = "btnGuardar", .Text = "&Guardar", .Location = New Point(664, 60), .Size = New Size(140, 34)}
        Private ReadOnly btnRestaurar As New Button With {.Name = "btnRestaurar", .Text = "&Valores por defecto", .Location = New Point(664, 102), .Size = New Size(140, 34)}
        Private ReadOnly txtObservaciones As TextBox() = {
            CampoNumero("txtObservacion1", "Largo de la observación 1", 260, 146), CampoNumero("txtObservacion2", "Largo de la observación 2", 330, 146), CampoNumero("txtObservacion3", "Largo de la observación 3", 400, 146)}
        Private ReadOnly gbLeyendas As New GroupBox With {.Name = "gbLeyendas", .Text = "Leyenda al pie de factura (40 caracteres por línea)", .Location = New Point(10, 180), .Size = New Size(794, 196)}
        Private ReadOnly txtLeyendas As TextBox() = {CampoLeyenda("txtLinea1", "Línea 1", 26), CampoLeyenda("txtLinea2", "Línea 2", 58), CampoLeyenda("txtLinea3", "Línea 3", 90), CampoLeyenda("txtLinea4", "Última línea", 122)}
        Private ReadOnly txtProforma As New TextBox With {.Name = "txtProforma", .AccessibleName = "Texto de la proforma", .Location = New Point(130, 154), .Width = 650}
        Private ReadOnly gbRedondeo As New GroupBox With {.Name = "gbRedondeo", .Text = "Redondeos", .Location = New Point(10, 384), .Size = New Size(794, 92)}
        Private ReadOnly txtRedondeoArticulo As TextBox = CampoNumero("txtRedondeoArticulo", "Redondeo de cada artículo", 72, 24)
        Private ReadOnly txtRedondeoTotal As TextBox = CampoNumero("txtRedondeoTotal", "Redondeo del total", 72, 56)
        Private ReadOnly cmbDireccionArticulo As New ComboBox With {.Name = "cmbDireccionArticulo", .AccessibleName = "Dirección del redondeo de cada artículo", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(470, 22), .Width = 180}
        Private ReadOnly cmbDireccionTotal As New ComboBox With {.Name = "cmbDireccionTotal", .AccessibleName = "Dirección del redondeo del total", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(470, 54), .Width = 180}
        Private ReadOnly gbOtros As New GroupBox With {.Name = "gbOtros", .Text = "Otros", .Location = New Point(10, 484), .Size = New Size(794, 60)}
        Private ReadOnly cmbCopias As New ComboBox With {.Name = "cmbCopias", .AccessibleName = "Cantidad de copias crédito", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(200, 22), .Width = 420}

        ' ---- Formatos ----
        Private ReadOnly cmbFormatos As New ComboBox With {.Name = "cmbFormatos", .AccessibleName = "Formato de factura", .DropDownStyle = ComboBoxStyle.DropDown, .MaxLength = PropiedadesFacturacionRepositorio.LargoNombreFormato, .Location = New Point(150, 14), .Width = 220}
        Private ReadOnly btnVariables As New Button With {.Name = "btnVariables", .Text = "&Variables", .Location = New Point(436, 12), .Size = New Size(100, 28)}
        Private ReadOnly btnBuscarVariable As New Button With {.Name = "btnBuscarVariable", .Text = "&Buscar variable", .Location = New Point(542, 12), .Size = New Size(130, 28), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
        Private ReadOnly btnGuardarFormato As New Button With {.Name = "btnGuardarFormato", .Text = "&Guardar formato", .Location = New Point(678, 12), .Size = New Size(126, 28), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
        Private ReadOnly grdFormato As New DataGridView With {
            .Name = "grdFormato", .AccessibleName = "Líneas del formato", .Location = New Point(10, 50), .Size = New Size(794, 454),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right,
            .AllowUserToAddRows = True, .AllowUserToDeleteRows = True, .AllowUserToResizeRows = False, .RowHeadersWidth = 28,
            .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells, .SelectionMode = DataGridViewSelectionMode.CellSelect}
        Private ReadOnly btnExportar As New Button With {.Name = "btnExportar", .Text = "&Exportar", .Location = New Point(578, 512), .Size = New Size(110, 28), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}
        Private ReadOnly btnImportar As New Button With {.Name = "btnImportar", .Text = "&Importar", .Location = New Point(694, 512), .Size = New Size(110, 28), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}

        Public Sub New(repo As PropiedadesFacturacionRepositorio, variables As VariablesImpresionRepositorio, configIni As String, local As Long, uid As Long, genero As String, empresa As String)
            _repo = repo
            _variables = variables
            _configIni = configIni
            _local = local
            _uid = uid
            _genero = genero
            _empresa = empresa
            Text = "Propiedades de facturación"
            ClientSize = New Size(840, 600)
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            cmbDocumento.Items.AddRange(ImpresorasPos.Nombres.ToArray())
            For Each c In {cmbDireccionArticulo, cmbDireccionTotal}
                c.Items.AddRange({"Ascendente", "Descendente", "Ambas"})
            Next
            cmbCopias.Items.AddRange({"Una - (Factura preimpresa)", "Dos - Boucher (Original - Cliente)", "Tres - Boucher (Original - Cliente - Contabilidad)"})

            gbImpresora.Controls.AddRange(New Control() {cmbDocumento, btnSeleccionar, New Label With {.Text = "Formato", .AutoSize = True, .Location = New Point(196, 59)}, cmbFormatoDocumento, btnGuardarImpresora, lblImpresora})
            For i = 0 To 3
                gbLeyendas.Controls.Add(New Label With {.Text = If(i = 3, "Última línea", $"Línea {i + 1}"), .AutoSize = True, .Location = New Point(12, 29 + 32 * i)})
                gbLeyendas.Controls.Add(txtLeyendas(i))
            Next
            gbLeyendas.Controls.AddRange(New Control() {New Label With {.Text = "Texto proforma", .AutoSize = True, .Location = New Point(12, 157)}, txtProforma})
            gbRedondeo.Controls.AddRange(New Control() {
                New Label With {.Text = "Aplicar un", .AutoSize = True, .Location = New Point(12, 27)}, txtRedondeoArticulo, New Label With {.Text = "por redondeo a cada artículo", .AutoSize = True, .Location = New Point(140, 27)}, cmbDireccionArticulo,
                New Label With {.Text = "Aplicar un", .AutoSize = True, .Location = New Point(12, 59)}, txtRedondeoTotal, New Label With {.Text = "por redondeo al total de la factura", .AutoSize = True, .Location = New Point(140, 59)}, cmbDireccionTotal})
            gbOtros.Controls.AddRange(New Control() {New Label With {.Text = "Cantidad de copias crédito", .AutoSize = True, .Location = New Point(12, 25)}, cmbCopias})
            tabParametros.Controls.AddRange(New Control() {gbImpresora, btnModificar, btnGuardar, btnRestaurar, New Label With {.Text = "Largo de observaciones", .AutoSize = True, .Location = New Point(12, 149)}})
            tabParametros.Controls.AddRange(txtObservaciones)
            tabParametros.Controls.AddRange(New Control() {gbLeyendas, gbRedondeo, gbOtros})

            grdFormato.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colPos", .HeaderText = "Pos.", .Width = 60, .ValueType = GetType(Integer)})
            grdFormato.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colLinea", .HeaderText = "Línea", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .MaxInputLength = PropiedadesFacturacionRepositorio.LargoLinea,
                                                                   .DefaultCellStyle = New DataGridViewCellStyle With {.WrapMode = DataGridViewTriState.True, .Font = New Font("Consolas", 9.0F)}})
            grdFormato.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colMultiple", .HeaderText = "Múltiple", .Width = 80, .ValueType = GetType(Integer)})
            tabFormatos.Controls.AddRange(New Control() {New Label With {.Text = "Formatos de factura", .AutoSize = True, .Location = New Point(12, 17)}, cmbFormatos, btnVariables, btnBuscarVariable, btnGuardarFormato, grdFormato, btnExportar, btnImportar})

            tabs.TabPages.AddRange({tabParametros, tabFormatos})
            Controls.Add(tabs)
            Ventanas.Recordar(Me)

            AddHandler cmbDocumento.SelectedIndexChanged, Sub() MostrarDocumento()
            AddHandler btnSeleccionar.Click, AddressOf Seleccionar_Clic
            AddHandler btnGuardarImpresora.Click, AddressOf GuardarImpresora_Clic
            AddHandler btnModificar.Click, Sub() Habilitar(True)
            AddHandler btnGuardar.Click, Async Sub() Await GuardarAsync()
            AddHandler btnRestaurar.Click, Sub() Restaurar()
            For Each t In {txtRedondeoArticulo, txtRedondeoTotal}
                Dim campo = t
                AddHandler campo.Leave, Sub() campo.Text = Conversion.Val(campo.Text.Replace(","c, "."c)).ToString("0.00", Invariante)
            Next
            AddHandler cmbFormatos.SelectionChangeCommitted, Async Sub() Await CargarFormatoAsync(TryCast(cmbFormatos.SelectedItem, String))
            AddHandler btnGuardarFormato.Click, Async Sub() Await GuardarFormatoAsync()
            AddHandler btnVariables.Click, AddressOf Variables_Clic
            AddHandler btnBuscarVariable.Click, Async Sub() Await BuscarVariableAsync()
            AddHandler btnExportar.Click, AddressOf Exportar_Clic
            AddHandler btnImportar.Click, AddressOf Importar_Clic
            AddHandler grdFormato.DataError, Sub(s, e)
                                                 MessageBox.Show(Me, "Escriba un número entero.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                 e.Cancel = True
                                             End Sub
            Habilitar(False)
        End Sub

        Private Shared Function CampoNumero(nombre As String, descripcion As String, x As Integer, y As Integer) As TextBox
            Dim t As New TextBox With {.Name = nombre, .AccessibleName = descripcion, .TextAlign = HorizontalAlignment.Right, .Location = New Point(x, y), .Width = 60}
            AddHandler t.KeyPress, Sub(s, e)
                                       If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> "."c AndAlso e.KeyChar <> ","c Then e.Handled = True
                                   End Sub
            Return t
        End Function

        Private Shared Function CampoLeyenda(nombre As String, descripcion As String, y As Integer) As TextBox
            Return New TextBox With {.Name = nombre, .AccessibleName = descripcion, .MaxLength = PropiedadesFacturacionRepositorio.LargoLeyenda, .Location = New Point(130, y), .Width = 650, .Font = New Font("Consolas", 9.75F)}
        End Function

        ''' <summary>SD_Activado: todo lo de la pestaña de parámetros se edita después de Modificar.</summary>
        Private Sub Habilitar(editando As Boolean)
            For Each c As Control In New Control() {gbImpresora, gbLeyendas, gbRedondeo, gbOtros, btnGuardar, btnRestaurar}
                c.Enabled = editando
            Next
            For Each t In txtObservaciones
                t.Enabled = editando
            Next
            btnModificar.Enabled = Not editando
        End Sub

        ''' <summary>Form_Load: los parámetros, la lista de formatos y la impresora del primer documento.</summary>
        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                Mostrar(Await _repo.CargarAsync(_local))
                Dim formatos = Await _repo.FormatosAsync(_local)
                cmbFormatos.Items.AddRange(formatos.ToArray())
                cmbFormatoDocumento.Items.AddRange(formatos.ToArray())
                cmbFormatoDocumento.Items.Add(ImpresorasPos.FormatoCarta)
                cmbDocumento.SelectedIndex = 0
                If formatos.Count = 0 Then
                    MessageBox.Show(Me, "No se encontraron formatos de factura. Favor crear al menos uno.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    cmbFormatos.SelectedIndex = 0
                    Await CargarFormatoAsync(formatos(0))
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ' ---- Parámetros ----

        Private Sub Mostrar(p As ParametrosFacturacion)
            For i = 0 To 3
                txtLeyendas(i).Text = p.Leyendas(i)
            Next
            txtProforma.Text = p.TextoProforma
            txtRedondeoArticulo.Text = p.RedondeoArticulo.ToString("0.00", Invariante)
            txtRedondeoTotal.Text = p.RedondeoTotal.ToString("0.00", Invariante)
            cmbDireccionArticulo.SelectedIndex = CInt(p.DireccionArticulo)
            cmbDireccionTotal.SelectedIndex = CInt(p.DireccionTotal)
            For i = 0 To 2
                txtObservaciones(i).Text = p.LargoObservaciones(i).ToString(Invariante)
            Next
            cmbCopias.SelectedIndex = p.CopiasCredito - 1
        End Sub

        Private Function Leer() As ParametrosFacturacion
            Return New ParametrosFacturacion With {
                .Leyendas = txtLeyendas.Select(Function(t) t.Text).ToArray(),
                .TextoProforma = txtProforma.Text,
                .RedondeoArticulo = Conversion.Val(txtRedondeoArticulo.Text.Replace(","c, "."c)),
                .RedondeoTotal = Conversion.Val(txtRedondeoTotal.Text.Replace(","c, "."c)),
                .DireccionArticulo = CType(Math.Max(0, cmbDireccionArticulo.SelectedIndex), DireccionRedondeo),
                .DireccionTotal = CType(Math.Max(0, cmbDireccionTotal.SelectedIndex), DireccionRedondeo),
                .LargoObservaciones = txtObservaciones.Select(Function(t) CInt(Core.Vb6.Val(t.Text))).ToArray(),
                .CopiasCredito = Math.Max(0, cmbCopias.SelectedIndex) + 1}
        End Function

        ''' <summary>cmdRestaurar.</summary>
        Private Sub Restaurar()
            Dim p = Leer()
            PropiedadesFacturacionRepositorio.ValoresPorDefecto(p)
            Mostrar(p)
        End Sub

        ''' <summary>cmdGuardar.</summary>
        Private Async Function GuardarAsync() As Task
            Try
                btnGuardar.Enabled = False
                Await _repo.GuardarAsync(Leer(), _local, _uid)
                Habilitar(False)
            Catch ex As MySqlException
                btnGuardar.Enabled = True
                Mensajes.NoGuardo(ex.Message, "Guardando parámetros", Modulo.TituloModulo)
            End Try
        End Function

        ''' <summary>cmbPrinters_Click: la impresora y el formato del documento.</summary>
        Private Sub MostrarDocumento()
            Dim d = DocumentoElegido()
            lblImpresora.Text = ImpresorasPos.Impresora(_configIni, d, FrmSeleccionImpresora.PredeterminadaDelSistema())
            Dim formato = ImpresorasPos.Formato(_configIni, d)
            Dim i = cmbFormatoDocumento.Items.Cast(Of String)().ToList().FindIndex(Function(f) String.Equals(f, formato, StringComparison.OrdinalIgnoreCase))
            cmbFormatoDocumento.SelectedIndex = i
        End Sub

        Private Function DocumentoElegido() As DocumentoImpresion
            Return CType(Math.Max(0, cmbDocumento.SelectedIndex), DocumentoImpresion)
        End Function

        ''' <summary>cmdSelec: frmSelPrinters.</summary>
        Private Sub Seleccionar_Clic(sender As Object, e As EventArgs)
            Using f As New FrmSeleccionImpresora(_configIni, DocumentoElegido())
                Tema.Aplicar(f, Modulo.TituloModulo)
                If f.ShowDialog(Me) = DialogResult.OK Then lblImpresora.Text = f.Impresora
            End Using
        End Sub

        ''' <summary>cmdGuardaPrint.</summary>
        Private Sub GuardarImpresora_Clic(sender As Object, e As EventArgs)
            Dim formato = TryCast(cmbFormatoDocumento.SelectedItem, String)
            If String.IsNullOrEmpty(formato) Then Return
            Try
                ImpresorasPos.GuardarFormato(_configIni, DocumentoElegido(), formato)
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                MessageBox.Show(Me, $"No se pudo guardar en {_configIni}.{Environment.NewLine}{ex.Message}", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        ' ---- Formatos ----

        ''' <summary>cmbFormatos_Click.</summary>
        Private Async Function CargarFormatoAsync(informe As String) As Task
            If String.IsNullOrEmpty(informe) Then Return
            Try
                Dim lineas = Await _repo.LineasAsync(_local, informe)
                MostrarLineas(lineas)
                _formatoCargado = informe
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Sub MostrarLineas(lineas As IEnumerable(Of LineaFormato))
            grdFormato.Rows.Clear()
            For Each l In lineas
                grdFormato.Rows.Add(l.Pos, l.Linea, l.Multiple)
            Next
        End Sub

        Private Function LineasEnPantalla() As List(Of LineaFormato)
            Dim lista As New List(Of LineaFormato)
            For Each fila As DataGridViewRow In grdFormato.Rows
                If fila.IsNewRow Then Continue For
                Dim linea = If(TryCast(fila.Cells(1).Value, String), "")
                If linea.Length = 0 Then Continue For
                lista.Add(New LineaFormato With {
                    .Pos = If(fila.Cells(0).Value Is Nothing, -1, CInt(fila.Cells(0).Value)), .Linea = linea,
                    .Multiple = If(fila.Cells(2).Value Is Nothing, -1, CInt(fila.Cells(2).Value))})
            Next
            Return lista
        End Function

        ''' <summary>
        ''' cmdGuardaFormato: reemplaza las líneas del formato que dice el combo. Con un nombre nuevo se crea otro
        ''' formato con las líneas en pantalla, como en VB6.
        ''' </summary>
        Private Async Function GuardarFormatoAsync() As Task
            grdFormato.EndEdit()
            Dim nombre = cmbFormatos.Text.Trim()
            Dim lineas = LineasEnPantalla()
            Dim problema = PropiedadesFacturacionRepositorio.Validar(nombre, lineas)
            If problema.Length > 0 Then
                MessageBox.Show(Me, problema, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim existe = cmbFormatos.Items.Cast(Of String)().Any(Function(f) String.Equals(f, nombre, StringComparison.OrdinalIgnoreCase))
            If Not existe AndAlso MessageBox.Show(Me, $"Se creará el formato {nombre} con estas líneas. ¿Desea continuar?", Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Try
                Await _repo.GuardarFormatoAsync(_local, nombre, lineas)
                If Not existe Then
                    cmbFormatos.Items.Add(nombre)
                    cmbFormatoDocumento.Items.Insert(cmbFormatoDocumento.Items.Count - 1, nombre)
                End If
                _formatoCargado = nombre
                MessageBox.Show(Me, $"Formato {nombre} guardado.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "FORMATOS", Modulo.TituloModulo)
            End Try
        End Function

        Private Sub Variables_Clic(sender As Object, e As EventArgs)
            Using f As New FrmVariablesImpresion(_variables, _uid, _genero, _empresa)
                Tema.Aplicar(f, Modulo.TituloModulo)
                f.ShowDialog(Me)
            End Using
        End Sub

        ''' <summary>cmdBuscar: el buscador de variables, solo para consultarlas.</summary>
        Private Async Function BuscarVariableAsync() As Task
            Try
                Using datos = Await _variables.BuscarAsync()
                    Using buscador As New FrmBuscador("Búsqueda de variables", datos)
                        buscador.ShowDialog(Me)
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Sub Exportar_Clic(sender As Object, e As EventArgs)
            grdFormato.EndEdit()
            Dim lineas = LineasEnPantalla()
            If lineas.Count = 0 Then Return
            Using dialogo As New SaveFileDialog With {.Title = "Exportar formato", .Filter = "Formato de impresión|*.sdformat", .FileName = cmbFormatos.Text.Trim() & ".sdformat"}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                IO.File.WriteAllText(dialogo.FileName, PropiedadesFacturacionRepositorio.Exportar(lineas), PropiedadesFacturacionRepositorio.CodificacionArchivo)
                MessageBox.Show(Me, $"Los datos fueron exportados exitosamente a la siguiente dirección:{Environment.NewLine}{Environment.NewLine}{dialogo.FileName}", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Using
        End Sub

        ''' <summary>cmdImportar: reemplaza las líneas en pantalla; se graban con Guardar formato.</summary>
        Private Sub Importar_Clic(sender As Object, e As EventArgs)
            Using dialogo As New OpenFileDialog With {.Title = "Importar formato", .Filter = "Formato de impresión (*.sdformat)|*.sdformat"}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                Dim lineas = PropiedadesFacturacionRepositorio.Importar(IO.File.ReadAllText(dialogo.FileName, PropiedadesFacturacionRepositorio.CodificacionArchivo))
                If lineas.Count = 0 Then
                    MessageBox.Show(Me, "El archivo no tiene líneas de formato.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                MostrarLineas(lineas)
            End Using
        End Sub

    End Class

End Namespace
