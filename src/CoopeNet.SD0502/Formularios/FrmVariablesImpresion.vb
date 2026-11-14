Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Variables de impresión para facturas. Migración de SD0502\FACPOS\frmVariables_Imp.frm, el botón Variables de
    ''' Propiedades de facturación, con su barra completa (incluido Imprimir, el RPT0003 de FACPOS).
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: el parámetro es obligatorio, al guardar una nueva se muestra su código, Cancelar vuelve a
    ''' la que se veía y Borrar solo se activa con una variable cargada.
    ''' </remarks>
    Public Class FrmVariablesImpresion
        Inherits Form

        Private Const Nuevo As String = "<< Nuevo >>"

        Private ReadOnly _repo As VariablesImpresionRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _genero As String
        Private ReadOnly _empresa As String
        Private _actual As VariableImpresion
        Private _antes As VariableImpresion

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(12, 8), .Size = New Size(560, 164)}
        Private ReadOnly lblCodigo As New Label With {.Name = "lblCodigo", .AutoSize = True, .Location = New Point(110, 26)}
        Private ReadOnly lblFecha As New Label With {.Name = "lblFecha", .AutoSize = True, .Location = New Point(250, 26)}
        Private ReadOnly txtParametro As New TextBox With {
            .Name = "txtParametro", .AccessibleName = "Parámetro", .CharacterCasing = CharacterCasing.Upper,
            .MaxLength = VariablesImpresionRepositorio.LargoParametro, .Location = New Point(110, 56), .Width = 160}
        Private ReadOnly txtDescripcion As New TextBox With {
            .Name = "txtDescripcion", .AccessibleName = "Descripción", .CharacterCasing = CharacterCasing.Upper,
            .MaxLength = VariablesImpresionRepositorio.LargoTexto, .Location = New Point(110, 90), .Width = 434}
        Private ReadOnly txtValor As New TextBox With {
            .Name = "txtValor", .AccessibleName = "Valor", .CharacterCasing = CharacterCasing.Upper,
            .MaxLength = VariablesImpresionRepositorio.LargoTexto, .Location = New Point(110, 124), .Width = 434}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Variable anterior", .Location = New Point(12, 182), .Size = New Size(48, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Variable siguiente", .Location = New Point(64, 182), .Size = New Size(48, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Modo = ModoBarra.Todos, .Location = New Point(120, 182), .Size = New Size(452, 32)}

        Public Sub New(repo As VariablesImpresionRepositorio, uid As Long, genero As String, empresa As String)
            _repo = repo
            _uid = uid
            _genero = genero
            _empresa = empresa
            Text = "Variables de impresión para facturas"
            ClientSize = New Size(584, 226)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            gbDatos.Controls.AddRange(New Control() {
                Rotulo("Código", 12, 26), lblCodigo, lblFecha,
                Rotulo("Parámetro", 12, 59), txtParametro, Rotulo("Descripción", 12, 93), txtDescripcion, Rotulo("Valor", 12, 127), txtValor
            })
            Controls.AddRange(New Control() {gbDatos, btnAnterior, btnSiguiente, barra})

            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler btnAnterior.Click, Async Sub() Await MoverAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await MoverAsync(True)
            Limpiar()
            Habilitar(False)
        End Sub

        Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Sub Limpiar()
            _actual = Nothing
            txtParametro.Text = ""
            txtDescripcion.Text = ""
            txtValor.Text = ""
            lblFecha.Text = ""
            lblCodigo.Text = Nuevo
        End Sub

        Private Sub Mostrar(v As VariableImpresion)
            Limpiar()
            If v Is Nothing Then Return
            _actual = v
            lblCodigo.Text = v.Codigo.ToString(CultureInfo.InvariantCulture)
            txtParametro.Text = v.Parametro
            txtDescripcion.Text = v.Descripcion
            txtValor.Text = v.Valor
            lblFecha.Text = If(v.FC.HasValue, "Creada el " & v.FC.Value.ToString("D", CultureInfo.CurrentCulture), "")
        End Sub

        Private Sub Habilitar(editando As Boolean)
            txtParametro.Enabled = editando
            txtDescripcion.Enabled = editando
            txtValor.Enabled = editando
            btnAnterior.Enabled = Not editando
            btnSiguiente.Enabled = Not editando
            barra.Estado(BotonBarra.Borrar, _actual IsNot Nothing)
        End Sub

        Private Async Function MoverAsync(siguiente As Boolean) As Task
            Dim boton = If(siguiente, btnSiguiente, btnAnterior)
            Dim otro = If(siguiente, btnAnterior, btnSiguiente)
            otro.Enabled = True
            Try
                Dim v = Await _repo.VecinaAsync(If(_actual?.Codigo, 0), siguiente)
                If v Is Nothing Then
                    boton.Enabled = False
                    Return
                End If
                Mostrar(v)
                barra.Estado(BotonBarra.Borrar, True)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        _antes = _actual
                        Limpiar()
                        Habilitar(True)
                        txtParametro.Focus()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        _antes = _actual
                        Habilitar(True)
                        txtParametro.Focus()
                    Case BotonBarra.Guardar
                        If txtParametro.Text.Trim().Length = 0 Then
                            txtParametro.Focus()
                            Return True
                        End If
                        Dim v As New VariableImpresion With {
                            .Codigo = If(_actual?.Codigo, 0), .Parametro = txtParametro.Text, .Descripcion = txtDescripcion.Text, .Valor = txtValor.Text, .FC = _actual?.FC}
                        v.Codigo = Await _repo.GuardarAsync(v, _uid)
                        Mostrar(If(Await _repo.CargarAsync(v.Codigo), v))
                        Habilitar(False)
                    Case BotonBarra.Cancelar
                        Mostrar(_antes)
                        Habilitar(False)
                    Case BotonBarra.Borrar
                        If _actual Is Nothing Then Return True
                        Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} de eliminar los datos actuales?"
                        If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                            Await _repo.BorrarAsync(_actual.Codigo)
                            Limpiar()
                            Habilitar(False)
                        End If
                    Case BotonBarra.Buscar
                        Limpiar()
                        Using datos = Await _repo.BuscarAsync()
                            Using buscador As New FrmBuscador("Búsqueda de variables", datos)
                                If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                                    Mostrar(Await _repo.CargarAsync(CInt(Core.Vb6.Val(buscador.Resultado(0)))))
                                End If
                            End Using
                        End Using
                        Habilitar(False)
                    Case BotonBarra.Imprimir
                        Await ImprimirAsync()
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "VARIABLES DE IMPRESIÓN", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SD_Imprimir (RPT0003): la lista ordenada por descripción.</summary>
        Private Async Function ImprimirAsync() As Task
            Dim lista = Await _repo.ListarAsync()
            Dim columnas = {New ColumnaImpresa("Código", 8), New ColumnaImpresa("Parámetro", 14, False), New ColumnaImpresa("Descripción", 44, False), New ColumnaImpresa("Valor", 34, False)}
            Dim filas = lista.Select(Function(v) New String() {v.Codigo.ToString(CultureInfo.InvariantCulture), v.Parametro, v.Descripcion, v.Valor}).ToList()
            Using documento As New TablaImpresa("Variables de impresión", _empresa, "FACPOS-RPT0003", Array.Empty(Of String)(), columnas, filas, Nothing)
                TablaImpresa.Mostrar(Me, documento)
            End Using
        End Function

    End Class

End Namespace
