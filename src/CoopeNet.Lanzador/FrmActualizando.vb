Imports System.Threading
Imports CoopeNet.Actualizacion

''' <summary>La ventanita de avance mientras se baja una versión. Cancelar abre la que ya había.</summary>
Public NotInheritable Class FrmActualizando
    Inherits Form

    Private ReadOnly _cancelar As New CancellationTokenSource()
    Private ReadOnly lblEstado As New Label With {.AutoSize = False, .Location = New Point(20, 20), .Size = New Size(380, 40), .Text = "Buscando actualizaciones..."}
    Private ReadOnly barra As New ProgressBar With {.Location = New Point(20, 66), .Size = New Size(380, 18), .Style = ProgressBarStyle.Marquee}
    Private ReadOnly btnCancelar As New Button With {.Text = "Cancelar", .Location = New Point(310, 98), .Size = New Size(90, 28)}

    Public Sub New()
        Text = "CoopeNet ERP"
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        StartPosition = FormStartPosition.CenterScreen
        AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        AutoScaleMode = AutoScaleMode.Dpi
        Font = New Font("Segoe UI", 9.75F)
        ClientSize = New Size(420, 140)
        Controls.AddRange(New Control() {lblEstado, barra, btnCancelar})
        CancelButton = btnCancelar
        AddHandler btnCancelar.Click, Sub()
                                          btnCancelar.Enabled = False
                                          lblEstado.Text = "Cancelando..."
                                          _cancelar.Cancel()
                                      End Sub
    End Sub

    Public ReadOnly Property Cancelacion As CancellationToken
        Get
            Return _cancelar.Token
        End Get
    End Property

    Public Sub Mostrar(avance As Avance)
        If IsDisposed OrElse _cancelar.IsCancellationRequested Then Return
        lblEstado.Text = $"{avance.Texto}" & vbCrLf & $"{avance.Hechos} de {avance.Total} archivos, {avance.Bytes / 1048576.0:N1} MB descargados"
        If avance.Total > 0 Then
            barra.Style = ProgressBarStyle.Continuous
            barra.Maximum = avance.Total
            barra.Value = Math.Min(avance.Hechos, avance.Total)
        End If
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then _cancelar.Dispose()
        MyBase.Dispose(disposing)
    End Sub

End Class
