Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Configuración de informes gráficos: si la impresora es de matriz de puntos o láser. Migración de
    ''' Comun\FrmConfigImp.frm (Mantenimiento, opción 056, privilegio 5072). Guarda RENDERMODE en la sección REPORTES
    ''' del CONFIG.INI de CoopeNet: 1 matriz de puntos, 0 láser o inyección de tinta.
    ''' </summary>
    ''' <remarks>
    ''' En VB6 era el RenderMode de Active Reports; SDACA lo leía al iniciar, pero ningún informe lo aplicaba (los de
    ''' SD0502 fijaban 1 siempre). Aquí también queda guardado para cuando un informe lo use.
    ''' </remarks>
    Public Class FrmConfiguracionInformes
        Inherits Form

        Public Const Seccion As String = "REPORTES"
        Public Const Clave As String = "RENDERMODE"

        Private Shared ReadOnly Tipos As String() = {"Matriz de puntos", "Láser o inyección de tinta"}
        Private Shared ReadOnly Descripciones As String() = {
            "Impresora de matriz de puntos, sus principales características son: utiliza papel continuo o por hoja individual, cinta de nylon, y permite picar esténcil.",
            "Impresora de láser o inyección de tinta, sus principales características son: utiliza hojas individuales, tóner o cartucho de inyección, muchas son multicolor."}

        Private ReadOnly _configIni As String

        Private ReadOnly cmbTipo As New ComboBox With {.Name = "cmbTipo", .AccessibleName = "Compatibilidad de la impresora", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(24, 44), .Width = 400}
        Private ReadOnly lblDescripcion As New Label With {.Name = "lblDescripcion", .Location = New Point(24, 84), .Size = New Size(400, 80)}
        Private ReadOnly btnAplicar As New Button With {.Name = "btnAplicar", .Text = "&Aplicar", .Location = New Point(204, 176), .Size = New Size(108, 30)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(316, 176), .Size = New Size(108, 30)}

        Public Sub New(configIni As String)
            _configIni = configIni
            Text = "Configuración de informes gráficos"
            ClientSize = New Size(448, 220)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir

            cmbTipo.Items.AddRange(Tipos)
            Controls.AddRange(New Control() {New Label With {.Text = "Compatibilidad de la impresora", .AutoSize = True, .Location = New Point(24, 18)}, cmbTipo, lblDescripcion, btnAplicar, btnSalir})
            AddHandler cmbTipo.SelectedIndexChanged, Sub() lblDescripcion.Text = If(cmbTipo.SelectedIndex >= 0, Descripciones(cmbTipo.SelectedIndex), "Seleccione una impresora...")
            AddHandler btnAplicar.Click, AddressOf Aplicar_Clic
            AddHandler btnSalir.Click, Sub() Close()

            ' Form_Load: "1" es matriz de puntos; cualquier otro valor, láser.
            cmbTipo.SelectedIndex = If(ConexionesIni.LeerValor(configIni, Seccion, Clave).Trim() = "1", 0, 1)
        End Sub

        ''' <summary>CmdAplicar: guarda y deja la pantalla abierta, como en VB6.</summary>
        Private Sub Aplicar_Clic(sender As Object, e As EventArgs)
            Try
                ConexionesIni.FijarValor(_configIni, Seccion, Clave, If(cmbTipo.SelectedIndex = 0, "1", "0"))
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                MessageBox.Show(Me, $"No se pudo guardar en {_configIni}.{Environment.NewLine}{ex.Message}", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
        End Sub

    End Class

End Namespace
