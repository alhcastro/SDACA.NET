Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Ui

Namespace SD1401

    ''' <summary>Paso #5, privilegio 14004. Migración de Fuentes\SD1401\REP\FrmConciliacionRep.frm.</summary>
    Public Class FrmInformeConciliacion
        Inherits FrmConCuenta

        Private ReadOnly _conciliacion As ConciliacionBancaria
        Private ReadOnly _empresa As String

        Private ReadOnly dtpInicio As New DateTimePicker With {.Name = "dtpInicio", .AccessibleName = "Fecha inicial", .Format = DateTimePickerFormat.Short, .Location = New Point(110, 142), .Width = 130}
        Private ReadOnly dtpFin As New DateTimePicker With {.Name = "dtpFin", .AccessibleName = "Fecha final", .Format = DateTimePickerFormat.Short, .Location = New Point(320, 142), .Width = 130}
        Private ReadOnly cmbOrden As New ComboBox With {.Name = "cmbOrden", .AccessibleName = "Orden", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(110, 174), .Width = 370}
        Private ReadOnly chkSinConciliar As New CheckBox With {.Name = "chkSinConciliar", .Text = "Solo los no conciliados", .AutoSize = True, .Location = New Point(110, 206)}
        Private ReadOnly chkTransito As New CheckBox With {.Name = "chkTransito", .Text = "Incluir el detalle en tránsito", .AutoSize = True, .Checked = True, .Location = New Point(290, 206)}
        Private ReadOnly btnVer As New Button With {.Name = "btnGuardar", .Text = "&Ver informe", .Location = New Point(270, 238), .Size = New Size(110, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(390, 238), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}

        Public Sub New(conciliacion As ConciliacionBancaria, cierre As CierreMes, uid As Long, todasLasCuentas As Boolean, empresa As String)
            MyBase.New(cierre, uid, todasLasCuentas, "Informe de Conciliación", New Size(500, 280))
            _conciliacion = conciliacion
            _empresa = empresa
            cmbOrden.Items.AddRange(New Object() {
                "(Créditos, Débitos), Documento", "Fecha Libros, (Créditos, Débitos), Documento", "Fecha Banco, (Créditos, Débitos), Documento",
                "(Débitos, Créditos), Documento", "Fecha Libros, (Débitos, Créditos), Documento", "Fecha Banco, (Débitos, Créditos), Documento"
            })
            cmbOrden.SelectedIndex = 0
            Dim hoy = Date.Today
            dtpInicio.Value = New Date(hoy.Year, hoy.Month, 1)
            dtpFin.Value = dtpInicio.Value.AddMonths(1).AddDays(-1)
            CancelButton = btnSalir
            Controls.AddRange(New Control() {
                Etiqueta("Desde", 12, 145), dtpInicio, Etiqueta("Hasta", 262, 145), dtpFin, Etiqueta("Orden", 12, 177), cmbOrden, chkSinConciliar, chkTransito, btnVer, btnSalir
            })
            AddHandler dtpInicio.ValueChanged, Sub() dtpFin.Value = New Date(dtpInicio.Value.Year, dtpInicio.Value.Month, 1).AddMonths(1).AddDays(-1)
            AddHandler btnVer.Click, AddressOf Ver
            AddHandler btnSalir.Click, Sub() Close()
        End Sub

        Private Async Sub Ver(sender As Object, e As EventArgs)
            If Not HayCuenta Then
                Aviso("Debe seleccionar una cuenta.")
                cmbCuentas.Focus()
                Return
            End If
            btnVer.Enabled = False
            Try
                Dim inicio = dtpInicio.Value.Date
                Dim fin = dtpFin.Value.Date
                Dim informe = Await _conciliacion.InformeAsync(Banco.Codigo, Cuenta.Cuenta, inicio, fin, CType(cmbOrden.SelectedIndex, OrdenInforme), chkSinConciliar.Checked)
                Dim subtitulo = $"Conciliación Bancaria cuenta# {Cuenta} del {Banco}. Entre el {inicio:dd/MMM/yyyy} y el {fin:dd/MMM/yyyy}"
                Using documento As New InformeConciliacionImpreso(informe, _empresa, subtitulo, chkTransito.Checked)
                    InformeConciliacionImpreso.Mostrar(Me, documento)
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                If Not IsDisposed Then btnVer.Enabled = True
            End Try
        End Sub

    End Class

End Namespace
