Namespace Seguridad

    ''' <summary>Reglas de <c>SD_Verif_Horario</c> y <c>SD_Horario</c>, sin acceso a datos.</summary>
    Public NotInheritable Class PoliticaHorario

        Private Sub New()
        End Sub

        ''' <summary>Columna de gatusuarios con el horario del día: D1 es domingo, como Weekday de VB6.</summary>
        Public Shared Function ColumnaDia(fecha As Date) As String
            Return "D" & (CInt(fecha.DayOfWeek) + 1)
        End Function

        ''' <summary>Cada bit de la máscara es una hora del día. Igual que SD_Horario, resta de mayor a menor.</summary>
        Public Shared Function HoraPermitida(mascara As Long, hora As Integer) As Boolean
            Dim permitida = False
            For bit = 23 To 0 Step -1
                Dim valor = 1L << bit
                If mascara >= valor Then
                    mascara -= valor
                    If bit = hora Then permitida = True
                End If
            Next
            Return permitida
        End Function

        ''' <summary>Si alguna fecha pasa de 1900-01-01, hoy tiene que estar entre las dos.</summary>
        Public Shared Function VigenciaPermite(desde As Date, hasta As Date, hoy As Date) As Boolean
            Dim sinFecha As New Date(1900, 1, 1)
            If desde > sinFecha OrElse hasta > sinFecha Then
                If desde > hoy.Date OrElse hasta < hoy.Date Then Return False
            End If
            Return True
        End Function

    End Class

End Namespace
