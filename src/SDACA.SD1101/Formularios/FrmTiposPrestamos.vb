Imports System.Drawing
Imports MySqlConnector
Imports SDACA.Core
Imports SDACA.Core.Bodegas
Imports SDACA.Ui

Namespace SD1101

    ''' <summary>
    ''' Opción 001.002 (Mantenimiento - Tipos de Préstamos), privilegio 11002.
    ''' Migración de Fuentes\SD1101\frmTiposPrestamos.frm.
    ''' </summary>
    Partial Public Class FrmTiposPrestamos
        Inherits Form

        Private Const SinCodigo As String = "<< Nuevo >>"

        Private ReadOnly _repositorio As TiposPrestamoRepositorio
        Private ReadOnly _genero As String

        Public Sub New(repositorio As TiposPrestamoRepositorio, genero As String)
            _repositorio = repositorio
            _genero = genero
            InitializeComponent()
            barra.AlHacerClic = AddressOf Barra_Clic
            Limpiar()
            txtDescripcion.Enabled = False
        End Sub

        Private ReadOnly Property HayCodigo As Boolean
            Get
                Return lblCodigo.Text <> SinCodigo
            End Get
        End Property

        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Select Case boton
                Case BotonBarra.Borrar
                    Await Borrar()
                Case BotonBarra.Buscar
                    Limpiar()
                    Await Buscar()
                Case BotonBarra.Cancelar
                    txtDescripcion.Enabled = False
                Case BotonBarra.Editar
                    If Not HayCodigo Then Return True
                    txtDescripcion.Enabled = True
                Case BotonBarra.Guardar
                    Return Await Guardar()
                Case BotonBarra.Nuevo
                    Limpiar()
                    Return Not Await PrepararNuevo()
                Case BotonBarra.Salir
                    Close()
            End Select
            Return False
        End Function

        Private Sub Limpiar()
            txtDescripcion.Text = ""
            chkAprobacion.Checked = False
            lblCodigo.Text = SinCodigo
        End Sub

        Private Async Function Borrar() As Task
            If Not HayCodigo Then Return
            Dim terminacion = If(_genero = "0", "o", "a")
            Dim respuesta = MessageBox.Show("Esta segur" & terminacion & " de eliminar los datos actuales?", Modulo.TituloModulo,
                                            MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If respuesta <> DialogResult.Yes Then Return
            Try
                Await _repositorio.BorrarAsync(CodigoActual())
                Limpiar()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "BORRANDO TIPO DE PRESTAMO", Modulo.TituloModulo)
            End Try
        End Function

        Private Async Function Buscar() As Task
            Dim tipos As List(Of TipoPrestamo)
            Try
                tipos = Await _repositorio.ListarAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return
            End Try
            Using datos As New DataTable()
                datos.Columns.Add("CODIGO", GetType(Integer))
                datos.Columns.Add("DESCRIPCION", GetType(String))
                For Each tipo In tipos
                    datos.Rows.Add(tipo.Codigo, tipo.Descripcion)
                Next
                Using buscador As New FrmBuscador("Búsqueda tipos de préstamos", datos)
                    If buscador.ShowDialog(Me) = DialogResult.OK Then
                        Await Cargar(CInt(Vb6.Val(buscador.Resultado(0))), DireccionConsulta.Exacta)
                    End If
                End Using
            End Using
        End Function

        ''' <summary>Devuelve True para cancelar el cambio de estado de la barra.</summary>
        Private Async Function Guardar() As Task(Of Boolean)
            If txtDescripcion.Text.Length = 0 Then
                MessageBox.Show("La descripción es un dato requerido", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtDescripcion.Focus()
                Return True
            End If
            Try
                Await _repositorio.GuardarAsync(CodigoActual(), txtDescripcion.Text.Trim(), chkAprobacion.Checked)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "GUARDANDO TIPO DE PRESTAMO", Modulo.TituloModulo)
            End Try
            txtDescripcion.Enabled = False
            Return False
        End Function

        Private Async Function PrepararNuevo() As Task(Of Boolean)
            Try
                lblCodigo.Text = (Await _repositorio.SiguienteCodigoAsync()).ToString()
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return False
            End Try
            txtDescripcion.Enabled = True
            txtDescripcion.Focus()
            Return True
        End Function

        Private Function CodigoActual() As Integer
            Return CInt(Vb6.Val(lblCodigo.Text))
        End Function

        Private Async Function Cargar(codigo As Integer, direccion As DireccionConsulta) As Task(Of Boolean)
            Try
                Dim tipo = Await _repositorio.ObtenerAsync(codigo, direccion)
                If tipo Is Nothing Then Return False
                lblCodigo.Text = tipo.Codigo.ToString()
                txtDescripcion.Text = tipo.Descripcion
                chkAprobacion.Checked = tipo.RequiereAprobacion
                Return True
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return False
            End Try
        End Function

        Private Sub TxtDescripcion_Enter(sender As Object, e As EventArgs) Handles txtDescripcion.Enter
            txtDescripcion.BackColor = SystemColors.Info
        End Sub

        Private Sub TxtDescripcion_Leave(sender As Object, e As EventArgs) Handles txtDescripcion.Leave
            txtDescripcion.BackColor = SystemColors.Window
            txtDescripcion.Text = txtDescripcion.Text.ToUpper()
        End Sub

        Private Async Sub CmdAnterior_Click(sender As Object, e As EventArgs) Handles cmdAnterior.Click
            cmdProximo.Enabled = True
            cmdAnterior.Enabled = Await Cargar(CodigoActual(), DireccionConsulta.Anterior)
        End Sub

        Private Async Sub CmdProximo_Click(sender As Object, e As EventArgs) Handles cmdProximo.Click
            cmdAnterior.Enabled = True
            cmdProximo.Enabled = Await Cargar(CodigoActual(), DireccionConsulta.Siguiente)
        End Sub

    End Class

End Namespace
