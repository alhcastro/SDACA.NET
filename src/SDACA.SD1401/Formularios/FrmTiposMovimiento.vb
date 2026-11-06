Imports MySqlConnector
Imports SDACA.Core
Imports SDACA.Core.Bancos
Imports SDACA.Ui

Namespace SD1401

    ''' <summary>
    ''' Opción 020 (Tipos de Movimiento), privilegio 14015. Migración de Fuentes\SD1401\FrmMantTiposMov.frm.
    ''' El original no borra tipos, deja editar la abreviatura y el tipo fuera de edición y guarda sin
    ''' descripción. Aquí solo se edita en modo edición y la descripción es obligatoria.
    ''' </summary>
    Partial Public Class FrmTiposMovimiento
        Inherits Form

        Private ReadOnly _repositorio As TiposMovimientoRepositorio
        Private ReadOnly _local As Long
        Private _actual As TipoMovimiento

        Public Sub New(repositorio As TiposMovimientoRepositorio, local As Long)
            _repositorio = repositorio
            _local = local
            InitializeComponent()
            cmbTipo.SelectedIndex = TipoMovimiento.Resta
            barra.AlHacerClic = AddressOf Barra_Clic
            barra.Estado(BotonBarra.Borrar, False)
            Editable(False)
        End Sub

        Private Async Sub FrmTiposMovimiento_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Await Navegar(Function() _repositorio.PrimeroAsync())
        End Sub

        Private Sub Editable(activo As Boolean)
            txtDescripcion.Enabled = activo
            txtAbreviatura.Enabled = activo
            cmbTipo.Enabled = activo
            For Each boton In {btnPrimero, btnAnterior, btnSiguiente, btnUltimo}
                boton.Enabled = Not activo
            Next
        End Sub

        Private Sub Mostrar(tipo As TipoMovimiento)
            _actual = tipo
            txtCodigo.Text = If(tipo Is Nothing, "", tipo.Codigo)
            txtDescripcion.Text = If(tipo Is Nothing, "", tipo.Descripcion)
            txtAbreviatura.Text = If(tipo Is Nothing, "", tipo.Abreviatura)
            cmbTipo.SelectedIndex = If(tipo Is Nothing, TipoMovimiento.Resta, tipo.Efecto)
        End Sub

        ''' <summary>Sin resultado se queda donde está, igual que SD_Move.</summary>
        Private Async Function Navegar(consulta As Func(Of Task(Of TipoMovimiento))) As Task
            Try
                Dim tipo = Await consulta()
                If tipo Is Nothing Then
                    Beep()
                Else
                    Mostrar(tipo)
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Shared Sub Beep()
            Media.SystemSounds.Beep.Play()
        End Sub

        Private ReadOnly Property CodigoActual As Long
            Get
                Return Vb6.Val(txtCodigo.Text)
            End Get
        End Property

        Private Async Sub BtnPrimero_Click(sender As Object, e As EventArgs) Handles btnPrimero.Click
            Await Navegar(Function() _repositorio.PrimeroAsync())
        End Sub

        Private Async Sub BtnAnterior_Click(sender As Object, e As EventArgs) Handles btnAnterior.Click
            Await Navegar(Function() _repositorio.AnteriorAsync(CodigoActual))
        End Sub

        Private Async Sub BtnSiguiente_Click(sender As Object, e As EventArgs) Handles btnSiguiente.Click
            Await Navegar(Function() _repositorio.SiguienteAsync(CodigoActual))
        End Sub

        Private Async Sub BtnUltimo_Click(sender As Object, e As EventArgs) Handles btnUltimo.Click
            Await Navegar(Function() _repositorio.UltimoAsync())
        End Sub

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Select Case boton
                Case BotonBarra.Nuevo
                    Dim anterior = _actual
                    Mostrar(Nothing)
                    _actual = anterior
                    Editable(True)
                    txtDescripcion.Focus()
                Case BotonBarra.Editar
                    If txtCodigo.TextLength = 0 Then Return True
                    Editable(True)
                    txtDescripcion.Focus()
                Case BotonBarra.Guardar
                    Return Await Guardar()
                Case BotonBarra.Cancelar
                    Mostrar(_actual)
                    Editable(False)
                Case BotonBarra.Buscar
                    Await Buscar()
                Case BotonBarra.Salir
                    Close()
            End Select
            Return False
        End Function

        Private Async Function Guardar() As Task(Of Boolean)
            If txtDescripcion.Text.Trim().Length = 0 Then
                MessageBox.Show(Me, "La descripción es un dato requerido", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtDescripcion.Focus()
                Return True
            End If
            Try
                Dim codigo = Await _repositorio.GuardarAsync(txtCodigo.Text, txtDescripcion.Text, txtAbreviatura.Text, cmbTipo.SelectedIndex, _local)
                Mostrar(New TipoMovimiento With {
                    .Codigo = codigo, .Descripcion = txtDescripcion.Text, .Abreviatura = txtAbreviatura.Text, .Tipo = cmbTipo.SelectedIndex.ToString()
                })
                Editable(False)
                Return False
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "GUARDANDO TIPO DE MOVIMIENTO", Modulo.TituloModulo)
                Return True
            End Try
        End Function

        Private Async Function Buscar() As Task
            Dim tipos As List(Of TipoMovimiento)
            Try
                tipos = Await _repositorio.ListarAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return
            End Try
            Using datos As New DataTable()
                datos.Columns.Add("Descripcion", GetType(String))
                datos.Columns.Add("Tipo", GetType(String))
                datos.Columns.Add("Abreviatura", GetType(String))
                For Each tipo In tipos
                    datos.Rows.Add(tipo.Descripcion, tipo.Codigo, tipo.Abreviatura)
                Next
                Using buscador As New FrmBuscador("Búsqueda de tipos de movimiento", datos)
                    If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(1).Length > 0 Then
                        Dim codigo = Vb6.Val(buscador.Resultado(1))
                        Await Navegar(Function() _repositorio.ObtenerAsync(codigo))
                    End If
                End Using
            End Using
        End Function

    End Class

End Namespace
