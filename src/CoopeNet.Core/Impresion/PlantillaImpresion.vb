Imports System.Text

Namespace Impresion

    ''' <summary>Fila de gatprintapi.</summary>
    Public NotInheritable Class LineaPlantilla
        Public Property Linea As String = ""
        ''' <summary>-1 una vez. -2 solo si trae datos. De 0 en adelante repite la línea con # = 0, 1, 2...</summary>
        Public Property Multiple As Integer
    End Class

    ''' <summary>
    ''' SDPrintInfoAPI2 de Comun\SD_MOD_UTILES01.bas: arma el texto que recibe la impresora matricial.
    ''' Cada línea tiene tokens separados por |. NL$ salta de línea, TL$n cambia la letra (ESC ! n) y
    ''' un dato se escribe como I065FechaLg050: alineación (I, D o centrado), espacios antes, nombre
    ''' del dato y ancho. El nombre ET$ seguido de un texto imprime ese texto.
    ''' </summary>
    Public NotInheritable Class PlantillaImpresion

        Private Const Esc As Char = ChrW(27)

        Private Sub New()
        End Sub

        Public Shared Function Armar(lineas As IEnumerable(Of LineaPlantilla), valores As IDictionary(Of String, String)) As String
            Dim salida As New StringBuilder()
            Dim linea As New StringBuilder()
            Dim impreso = ""
            For Each fila In lineas
                Dim x = 0
                Do
                    Dim tokens = fila.Linea
                    If fila.Multiple > -1 Then
                        tokens = fila.Linea.Replace("#", x.ToString())
                        If Dato(PrimerDato(tokens), valores).Trim().Length = 0 Then
                            If fila.Multiple = 0 OrElse x > fila.Multiple Then
                                impreso = ""
                                linea.Clear()
                                Exit Do
                            End If
                            ' Sin dato, pero dentro de las repeticiones: una línea en blanco que guarda el espacio.
                            tokens = ""
                        End If
                    End If

                    impreso &= Escribir(tokens, fila.Multiple, valores, linea)

                    If impreso.Trim().Length > 0 OrElse fila.Multiple <> -2 Then
                        salida.Append(linea).Append(vbCrLf)
                        linea.Clear()
                    End If
                    If fila.Multiple < 0 Then
                        impreso = ""
                        linea.Clear()
                        Exit Do
                    End If
                    x += 1
                Loop
            Next
            Return salida.ToString()
        End Function

        Private Shared Function Escribir(tokens As String, multiple As Integer, valores As IDictionary(Of String, String), linea As StringBuilder) As String
            Dim impreso As New StringBuilder()
            For Each token In tokens.Split("|"c)
                Select Case Left3(token)
                    Case "NL$" : linea.Append(vbCrLf)
                    Case "TL$" : linea.Append(Esc).Append("!"c).Append(ChrW(CInt(Vb6.Val(token.Substring(3)))))
                    Case "CT$" : linea.Append(Esc).Append("i"c)
                    Case "SR$" : linea.Append(Esc).Append("Kn")
                    Case "ER$" : linea.Append(Esc).Append("e"c).Append(ChrW(2))
                    Case "AC$" : linea.Append(Esc).Append("p"c).Append(ChrW(0)).Append(ChrW(100)).Append(ChrW(250))
                    Case Else
                        If token.Length <= 7 Then Continue For
                        Dim codigo = Codigo_(token)
                        Dim esTexto = codigo.StartsWith("ET$", StringComparison.Ordinal)
                        Dim texto = If(esTexto, codigo.Substring(3), Dato(codigo, valores))
                        If multiple = -2 AndAlso texto.Trim().Length = 0 Then Continue For
                        If Not esTexto Then impreso.Append(texto)
                        linea.Append(" "c, CInt(Vb6.Val(token.Substring(1, 3))))
                        linea.Append(Ajusta(texto, CInt(Vb6.Val(token.Substring(token.Length - 3))), token(0)))
                End Select
            Next
            Return impreso.ToString()
        End Function

        ''' <summary>El primer token que no es un comando: decide si la línea repetida sigue teniendo datos.</summary>
        Private Shared Function PrimerDato(tokens As String) As String
            For Each token In tokens.Split("|"c)
                If Not Left3(token).Contains("$"c) AndAlso token.Length > 7 Then Return Codigo_(token)
            Next
            Return ""
        End Function

        Private Shared Function Codigo_(token As String) As String
            Return token.Substring(4, token.Length - 7)
        End Function

        Private Shared Function Left3(token As String) As String
            Return token.Substring(0, Math.Min(3, token.Length)).ToUpperInvariant()
        End Function

        Private Shared Function Dato(codigo As String, valores As IDictionary(Of String, String)) As String
            Dim valor As String = Nothing
            Return If(valores.TryGetValue(codigo, valor), If(valor, ""), "")
        End Function

        ''' <summary>
        ''' Ajusta de Comun. I rellena a la derecha, D a la izquierda y lo demás centra. Un texto
        ''' más largo que el ancho se corta: por la derecha con I, y si no conserva el final.
        ''' </summary>
        Public Shared Function Ajusta(texto As String, ancho As Integer, alineacion As Char) As String
            Dim izquierda = Char.ToUpperInvariant(alineacion) = "I"c
            Dim derecha = Char.ToUpperInvariant(alineacion) = "D"c
            If ancho <= texto.Length Then
                Return If(izquierda, texto.Substring(0, ancho), texto.Substring(texto.Length - ancho))
            End If
            If izquierda Then Return texto.PadRight(ancho)
            If derecha Then Return texto.PadLeft(ancho)
            ' Math.Round redondea .5 al par, igual que la conversión de VB6.
            Dim relleno = New String(" "c, CInt(Math.Round((ancho - texto.Length) / 2)))
            Dim centrado = relleno & texto & relleno
            Return If(centrado.Length < ancho, " " & centrado, centrado.Substring(0, ancho))
        End Function

    End Class

End Namespace
