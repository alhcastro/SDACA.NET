Imports System.Globalization

Namespace Cheques

    ''' <summary>
    ''' Migración literal de <c>Numeros2CaracteresC</c> (Comun\SD_MOD_UTILES02.bas). El texto va impreso
    ''' en los cheques, así que conserva sus particularidades: "Veintiuno mil" y que los céntimos se
    ''' queden con dos dígitos, de modo que 1.995 da "Uno Con 00/100".
    ''' </summary>
    Public NotInheritable Class MontoEnLetras

        Private Sub New()
        End Sub

        Public Shared Function Convertir(numero As Double, masculino As Boolean, Optional centimos As Boolean = False) As String
            If numero < 0 Then numero = -numero
            Dim fraccion = numero - Math.Truncate(numero)
            If numero < 1 Then
                If centimos AndAlso fraccion > 0 Then Return "Cero Con " & Centavos(fraccion) & "/100"
                Return "Cero"
            End If

            Dim x = Math.Truncate(numero).ToString("0", CultureInfo.InvariantCulture).PadLeft(9, "0"c)
            Dim a = ""
            ' Grupos de 3 cifras, de atrás hacia adelante.
            For n = 7 To 1 Step -3
                If CInt(Mid(x, n, 3)) <> 0 Then
                    a = Decenas(x, n, masculino, a)
                    If a.StartsWith("y", StringComparison.Ordinal) Then a = a.Substring(2)
                    a = Centena(x, n) & a
                End If
                If a.StartsWith("y", StringComparison.Ordinal) Then a = a.Substring(2)
                If n = 4 Then
                    If CInt(Left(x, 3)) = 1 Then
                        a = "millón " & a
                    ElseIf CInt(Left(x, 3)) <> 0 Then
                        a = "millones " & a
                    End If
                ElseIf n = 7 AndAlso CInt(Mid(x, 4, 3)) <> 0 Then
                    a = "mil " & a
                End If
                ' El grupo de los millones siempre va en masculino.
                If n = 1 Then masculino = True
                a = a.Replace("$", If(masculino, "o", "a"))
            Next

            If a.StartsWith("un mil ", StringComparison.Ordinal) Then
                a = a.Substring(3)
            ElseIf a.StartsWith("una mil ", StringComparison.Ordinal) Then
                a = a.Substring(4)
            End If

            Dim texto = (UCase(Left(a, 1)) & a.Substring(1)).Trim()
            If centimos Then texto &= " Con " & Centavos(fraccion) & "/100"
            Return texto
        End Function

        Private Shared Function Decenas(x As String, n As Integer, masculino As Boolean, a As String) As String
            Select Case CInt(Mid(x, n + 1, 2))
                Case 10 : Return "diez " & a
                Case 11 : Return "once " & a
                Case 12 : Return "doce " & a
                Case 13 : Return "trece " & a
                Case 14 : Return "catorce " & a
                Case 15 : Return "quince " & a
                Case 16 : Return "dieciseis " & a
                Case 17 : Return "diecisiete " & a
                Case 18 : Return "dieciocho " & a
                Case 19 : Return "diecinueve " & a
                Case 20 : Return "veinte " & a
                Case 21 : Return If(n > 1, "veintiun$ ", "veintiun ") & a
                Case 22 : Return "veintidos " & a
                Case 23 : Return "veintitrés " & a
                Case 24 : Return "veinticuatro " & a
                Case 25 : Return "veinticinco " & a
                Case 26 : Return "veintiseis " & a
                Case 27 : Return "veintisiete " & a
                Case 28 : Return "veintiocho " & a
                Case 29 : Return "veintinueve " & a
            End Select

            Select Case CInt(Mid(x, n + 2, 1))
                Case 1
                    Select Case n
                        Case 7 : a = "y un$ " & a
                        Case 4 : a = If(masculino, "y un ", "y una ") & a
                        Case 1 : a = "y un " & a
                    End Select
                Case 2 : a = "y dos " & a
                Case 3 : a = "y tres " & a
                Case 4 : a = "y cuatro " & a
                Case 5 : a = "y cinco " & a
                Case 6 : a = "y seis " & a
                Case 7 : a = "y siete " & a
                Case 8 : a = "y ocho " & a
                Case 9 : a = "y nueve " & a
            End Select

            Select Case CInt(Mid(x, n + 1, 1))
                Case 3 : a = "treinta " & a
                Case 4 : a = "cuarenta " & a
                Case 5 : a = "cincuenta " & a
                Case 6 : a = "sesenta " & a
                Case 7 : a = "setenta " & a
                Case 8 : a = "ochenta " & a
                Case 9 : a = "noventa " & a
            End Select
            Return a
        End Function

        Private Shared Function Centena(x As String, n As Integer) As String
            Select Case CInt(Mid(x, n, 1))
                Case 1 : Return If(CInt(Mid(x, n + 1, 2)) = 0, "cien ", "ciento ")
                Case 2 : Return "doscient$s "
                Case 3 : Return "trescient$s "
                Case 4 : Return "cuatrocient$s "
                Case 5 : Return "quinient$s "
                Case 6 : Return "seiscient$s "
                Case 7 : Return "setecient$s "
                Case 8 : Return "ochocient$s "
                Case 9 : Return "novecient$s "
                Case Else : Return ""
            End Select
        End Function

        ''' <summary>
        ''' <c>Ajusta(FormatNumber(FormatNumber(fraccion, 2) * 100, 0), 2, ...)</c>. VB6 redondea sobre
        ''' 15 cifras significativas; por eso se pasa por Decimal y no se usa el redondeo de Double.
        ''' </summary>
        Private Shared Function Centavos(fraccion As Double) As String
            Dim exacto = Decimal.Parse(fraccion.ToString("G15", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture)
            Dim redondeado = Math.Round(exacto, 2, MidpointRounding.AwayFromZero)
            Return Right("00" & CLng(redondeado * 100).ToString(CultureInfo.InvariantCulture), 2)
        End Function

    End Class

End Namespace
