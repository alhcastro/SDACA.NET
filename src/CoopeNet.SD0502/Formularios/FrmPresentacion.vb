Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Verificador de Presentación: muestra el precio del artículo y su precio por unidad de medida (por ml, por
    ''' gramo...), y deja corregir las descripciones, la unidad y la presentación. Migración de
    ''' SD0502\frmMantArticulos2.frm (Mantenimiento, opción 078, privilegio 5057).
    ''' </summary>
    ''' <remarks>
    ''' Como en VB6, no hay Nuevo ni Borrar, y la ventana se agranda o maximiza con los controles en proporción. Si el local no tiene la unidad del artículo (el local 7 no tiene
    ''' unidades), VB6 la borraba al guardar; aquí se conserva. No se migra el MSComm de la forma, que no se usa.
    ''' </remarks>
    Public Class FrmPresentacion
        Inherits Form

        Private Const CaracteresTexto As String = "-|;'"

        Private ReadOnly _repo As PresentacionesRepositorio
        Private ReadOnly _articulos As ArticulosRepositorio
        Private ReadOnly _local As Long
        Private _actual As PresentacionArticulo
        Private _factor As Double
        Private _direccion As DireccionRedondeo = DireccionRedondeo.Ascendente
        Private _editando As Boolean

        Private Shared ReadOnly Cultura As CultureInfo = CultureInfo.CurrentCulture

        Private ReadOnly gbFiltro As New GroupBox With {.Name = "gbFiltro", .Text = "Filtrado", .Location = New Point(135, 4), .Size = New Size(287, 52)}
        Private ReadOnly txtBarras As New TextBox With {.Name = "txtBarras", .AccessibleName = "Código o código de barras", .Location = New Point(46, 20), .Width = 230}
        Private ReadOnly logo As New LogoEmpresa With {.Name = "logoEmpresa", .Location = New Point(433, 8), .Size = New Size(169, 66), .Padding = New Padding(8, 6, 8, 6)}
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código del artículo", .ReadOnly = True, .TabStop = False, .Location = New Point(135, 82), .Width = 107}
        Private ReadOnly txtDescCorta As New TextBox With {.Name = "txtDescCorta", .AccessibleName = "Descripción corta", .MaxLength = 30, .Location = New Point(355, 82), .Width = 248}
        Private ReadOnly txtDescripcion As New TextBox With {.Name = "txtDescripcion", .AccessibleName = "Descripción larga", .MaxLength = 255, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(135, 110), .Width = 468}
        Private ReadOnly cmbUnidad As New ComboBox With {.Name = "cmbUnidad", .AccessibleName = "Unidad de medida", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(135, 138), .Width = 304}
        Private ReadOnly txtPresentacion As New TextBox With {.Name = "txtPresentacion", .AccessibleName = "Presentación", .TextAlign = HorizontalAlignment.Right, .Font = New Font("Segoe UI", 20.0F), .Location = New Point(27, 196), .Width = 150}
        Private ReadOnly lblPrecioUnidad As New Label With {.Name = "lblPrecioUnidad", .AutoSize = False, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font("Segoe UI Semibold", 12.0F), .Location = New Point(200, 176), .Size = New Size(402, 28)}
        Private ReadOnly lblPrecio As New Label With {.Name = "lblPrecio", .AutoSize = False, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font("Segoe UI Semibold", 40.0F), .Location = New Point(200, 208), .Size = New Size(402, 86)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(12, 310), .Size = New Size(430, 32)}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Artículo anterior", .Font = New Font("Segoe UI", 14.0F), .Location = New Point(468, 310), .Size = New Size(66, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Artículo siguiente", .Font = New Font("Segoe UI", 14.0F), .Location = New Point(536, 310), .Size = New Size(66, 32)}

        Public Sub New(repo As PresentacionesRepositorio, articulos As ArticulosRepositorio, local As Long)
            _repo = repo
            _articulos = articulos
            _local = local
            Text = "Verificador de Presentación"
            ClientSize = New Size(614, 354)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            KeyPreview = True

            lblPrecio.ForeColor = Tema.AcentoOscuro
            lblPrecioUnidad.ForeColor = Tema.AcentoOscuro
            gbFiltro.Controls.AddRange(New Control() {New Label With {.Text = "Cód.", .AutoSize = True, .Location = New Point(10, 23)}, txtBarras})
            Controls.AddRange(New Control() {
                gbFiltro, logo,
                Rotulo("Código artículo", 135, 82), txtCodigo, Rotulo("Desc. corta", 355, 82), txtDescCorta,
                Rotulo("Descripción larga", 135, 110), txtDescripcion,
                Rotulo("Unidad med.", 135, 140), cmbUnidad,
                New Label With {.Text = "Presentación", .AutoSize = True, .Location = New Point(27, 176)}, txtPresentacion,
                lblPrecioUnidad, lblPrecio,
                barra, btnAnterior, btnSiguiente
            })
            barra.AlHacerClic = AddressOf Barra_Clic
            barra.Estado(BotonBarra.Nuevo, False)
            barra.Estado(BotonBarra.Borrar, False)
            AddHandler txtBarras.KeyDown, AddressOf Barras_Tecla
            AddHandler btnAnterior.Click, Async Sub() Await VecinoAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await VecinoAsync(True)
            AddHandler txtDescripcion.Leave, AddressOf Descripcion_Salir
            AddHandler txtPresentacion.KeyPress, AddressOf Presentacion_Tecla
            Limpiar()
            Habilitar(False)
            ' Como en VB6 (SDSizer y MODOVENTANAS): se agranda o maximiza, los controles crecen con la ventana
            ' y recuerda si quedó maximizada.
            Ventanas.Recordar(Me)
        End Sub

        ''' <summary>Rótulo alineado a la derecha y a la izquierda del campo que empieza en <paramref name="xCampo"/>.</summary>
        Private Shared Function Rotulo(texto As String, xCampo As Integer, yCampo As Integer) As Label
            Dim ancho = TextRenderer.MeasureText(texto, SystemFonts.MessageBoxFont).Width + 4
            Return New Label With {.Text = texto, .AutoSize = False, .TextAlign = ContentAlignment.MiddleRight, .Location = New Point(xCampo - ancho - 6, yCampo), .Size = New Size(ancho, 23)}
        End Function

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            ' Antes de MyBase.OnLoad, que restaura el último tamaño: las medidas base son las de diseño.
            Proporcional.Aplicar(gbFiltro.Parent)
            MyBase.OnLoad(e)
            Try
                cmbUnidad.Items.AddRange((Await _repo.UnidadesAsync(_local)).ToArray())
                Dim redondeo = Await _repo.RedondeoAsync(_local)
                _factor = redondeo.Factor
                _direccion = redondeo.Direccion
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            txtBarras.Focus()
        End Sub

        ' ---- Pantalla ----

        ''' <summary>SD_Borrar.</summary>
        Private Sub Limpiar()
            cmbUnidad.SelectedIndex = -1
            txtCodigo.Clear()
            txtDescCorta.Clear()
            txtDescripcion.Clear()
            txtPresentacion.Clear()
            lblPrecio.Text = "0"
            lblPrecioUnidad.Text = ""
        End Sub

        Private Sub Mostrar(a As PresentacionArticulo)
            _actual = a
            Limpiar()
            If a Is Nothing Then Return
            txtCodigo.Text = a.CodigoArt
            txtDescCorta.Text = a.DescCorta
            txtDescripcion.Text = a.Descripcion
            txtPresentacion.Text = a.Grupo.ToString("G", Cultura)
            For i = 0 To cmbUnidad.Items.Count - 1
                If CType(cmbUnidad.Items(i), OpcionArticulo).Codigo = a.UnidadMedida.Trim() Then cmbUnidad.SelectedIndex = i
            Next
            Dim precio = PresentacionesRepositorio.PrecioVenta(a, _factor, _direccion)
            lblPrecio.Text = precio.ToString("N2", Cultura)
            lblPrecioUnidad.Text = PresentacionesRepositorio.TextoPrecioPorUnidad(a, precio, Cultura)
        End Sub

        ''' <summary>SD_Habilitar y SD_Mandos.</summary>
        Private Sub Habilitar(editando As Boolean)
            _editando = editando
            For Each c In New Control() {txtDescCorta, txtDescripcion, cmbUnidad, txtPresentacion}
                c.Enabled = editando
            Next
            txtBarras.Enabled = Not editando
            btnAnterior.Enabled = Not editando
            btnSiguiente.Enabled = Not editando
        End Sub

        ''' <summary>SD_CargarArt.</summary>
        Private Async Function CargarAsync(codigo As String) As Task
            If codigo.Length = 0 Then Return
            Dim a = Await _repo.ObtenerAsync(codigo, _local)
            If a Is Nothing Then
                MessageBox.Show(Me, "No se encontro ningun artículo con este código", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Mostrar(a)
        End Function

        ''' <summary>SD_Siguiente y SD_Anterior: al llegar al final se apaga la flecha de ese lado.</summary>
        Private Async Function VecinoAsync(siguiente As Boolean) As Task
            Try
                Dim codigo = Await _articulos.VecinoAsync(txtCodigo.Text.Trim(), _local, siguiente, 0, 0, 0)
                If siguiente Then btnAnterior.Enabled = True Else btnSiguiente.Enabled = True
                If codigo.Length = 0 Then
                    If siguiente Then btnSiguiente.Enabled = False Else btnAnterior.Enabled = False
                    Return
                End If
                Await CargarAsync(codigo)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>SDTCodBar_KeyDown: código de barras, código corto o código del artículo. Vacío abre el buscador.</summary>
        Private Async Sub Barras_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter Then Return
            e.SuppressKeyPress = True
            Try
                Dim texto = txtBarras.Text.Trim()
                If texto.Length = 0 Then
                    Await BuscarAsync()
                    Return
                End If
                Dim resultado = Await _articulos.BuscarCodigoBarrasAsync(texto, _local)
                Dim codigo = resultado.Articulo
                If resultado.Opciones IsNot Nothing Then
                    Using buscador As New FrmBuscador("Seleccione el código correcto", resultado.Opciones)
                        codigo = If(buscador.ShowDialog(Me) = DialogResult.OK, buscador.Resultado(0), "")
                    End Using
                    resultado.Opciones.Dispose()
                    If codigo.Length = 0 Then Return
                End If
                If codigo.Length = 0 Then codigo = texto
                Await CargarAsync(codigo)
                txtBarras.Clear()
                txtBarras.Focus()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Function BuscarAsync() As Task
            UseWaitCursor = True
            Try
                Using datos = Await _repo.BuscarAsync(_local)
                    UseWaitCursor = False
                    Using buscador As New FrmBuscador("Seleccione el artículo...", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(1).Length > 0 Then
                            Await CargarAsync(buscador.Resultado(1))
                        End If
                    End Using
                End Using
            Finally
                UseWaitCursor = False
            End Try
        End Function

        ''' <summary>txtDescripcion_LostFocus: sin descripción corta, toma los primeros 20 caracteres de la larga.</summary>
        Private Sub Descripcion_Salir(sender As Object, e As EventArgs)
            If _editando AndAlso txtDescCorta.Text.Length = 0 Then
                Dim larga = txtDescripcion.Text
                txtDescCorta.Text = If(larga.Length > 20, larga.Substring(0, 20), larga)
            End If
        End Sub

        Private Shared Sub Presentacion_Tecla(sender As Object, e As KeyPressEventArgs)
            Dim separador = Cultura.NumberFormat.NumberDecimalSeparator
            If Char.IsControl(e.KeyChar) OrElse Char.IsDigit(e.KeyChar) Then Return
            If e.KeyChar.ToString() = separador AndAlso Not CType(sender, TextBox).Text.Contains(separador) Then Return
            e.Handled = True
        End Sub

        ' ---- Barra ----

        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Buscar
                        Await BuscarAsync()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        Habilitar(True)
                        txtDescripcion.Focus()
                    Case BotonBarra.Guardar
                        If Not Await GuardarAsync() Then Return True
                        Habilitar(False)
                        txtBarras.Focus()
                    Case BotonBarra.Cancelar
                        Mostrar(_actual)
                        Habilitar(False)
                        txtBarras.Focus()
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "ARTICULOS", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SD_GuardarArticulo, con las validaciones de SDCaracteresProhibidos.</summary>
        Private Async Function GuardarAsync() As Task(Of Boolean)
            If txtCodigo.Text.Trim().Length = 0 Then Return False
            For Each caja In {txtDescripcion, txtDescCorta}
                Dim c = caja.Text.FirstOrDefault(Function(ch) CaracteresTexto.Contains(ch))
                If c <> Nothing Then
                    MessageBox.Show(Me, $"El carácter ( {c} ) no está permitido en {caja.AccessibleName.ToLower(Cultura)}.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    caja.Focus()
                    Return False
                End If
            Next
            Dim grupo As Double
            If txtPresentacion.Text.Trim().Length > 0 AndAlso Not Double.TryParse(txtPresentacion.Text, NumberStyles.Number, Cultura, grupo) Then
                MessageBox.Show(Me, "La presentación debe ser un número.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtPresentacion.Focus()
                Return False
            End If
            Dim unidad = If(TryCast(cmbUnidad.SelectedItem, OpcionArticulo)?.Codigo, _actual.UnidadMedida)
            Dim a As New PresentacionArticulo With {
                .CodigoArt = txtCodigo.Text.Trim(), .DescCorta = txtDescCorta.Text.Trim(), .Descripcion = txtDescripcion.Text.Trim(),
                .UnidadMedida = unidad, .Grupo = grupo
            }
            Await _repo.GuardarAsync(a, _local)
            Mostrar(If(Await _repo.ObtenerAsync(a.CodigoArt, _local), _actual))
            Return True
        End Function

    End Class

End Namespace
