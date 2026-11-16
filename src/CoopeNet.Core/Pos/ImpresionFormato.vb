Imports System.Text

Namespace Pos

    ''' <summary>Las alineaciones de Ajusta (SDTipoAjuste de Comun\SD_MOD_UTILES02.bas).</summary>
    Public Enum AjusteTexto
        Centrado = 0
        ''' <summary>SDAjusteDerecho: rellena a la derecha, así que el texto queda a la izquierda.</summary>
        RellenoDerecha = 1
        ''' <summary>SDAjusteIzquierdo: rellena a la izquierda, así que el texto queda a la derecha.</summary>
        RellenoIzquierda = 2
    End Enum

    ''' <summary>
    ''' El intérprete de formatos de impresión: SDPrintInfoAPI de SD0502\FACPOS\ModPOS.bas. Recibe las líneas de un
    ''' formato de gatprintapi y los nombres y valores de las variables (dos textos separados por |, como los arma
    ''' VB6) y devuelve el texto que va a la impresora, con sus códigos de control.
    ''' </summary>
    ''' <remarks>
    ''' Es un traslado literal, con las mismas funciones de texto, porque la forma en que VB6 recorre los tokens
    ''' decide qué se imprime. Cada token de una línea es: NL$ (salto), TL$n (letra n), CT$ (corte), SR$ y ER$
    ''' (retroceso Samsung y Epson), AC$ (abrir caja), QR$ (código QR con la clave), o un dato "PIIICODIGOLLL":
    ''' P es la alineación (I izquierda, D derecha, C centro), III la sangría, LLL el ancho y CODIGO la variable o
    ''' ET$texto para un texto fijo. Múltiple -1 imprime la línea una vez, -2 solo si alguno de sus datos tiene valor,
    ''' y 0 o más la repite cambiando # por 0, 1, 2… mientras la variable tenga valor (con n &gt; 0, al menos n + 1 veces).
    ''' </remarks>
    Public NotInheritable Class ImpresionFormato

        Private Const Esc As Char = ChrW(&H1B)
        Public Shared ReadOnly Property AbrirCaja As String = Esc & "p" & ChrW(0) & ChrW(100) & ChrW(250)

        Private Sub New()
        End Sub

        ' ---- Funciones de texto de VB6 ----

        ''' <summary>Str_GetLateral con pIzqDer = True: lo que hay antes del separador, o "" si no está.</summary>
        Public Shared Function Lateral(texto As String, busca As String) As String
            Dim i = texto.IndexOf(busca, StringComparison.Ordinal)
            Return If(i >= 0, texto.Substring(0, i), "")
        End Function

        ''' <summary>Mid$ de VB6 (base 1); sin largo, hasta el final. Fuera de rango devuelve "".</summary>
        Public Shared Function Medio(texto As String, inicio As Integer, Optional largo As Integer = -1) As String
            If inicio < 1 Then inicio = 1
            If inicio > texto.Length Then Return ""
            Dim resto = texto.Length - inicio + 1
            If largo < 0 OrElse largo > resto Then largo = resto
            Return texto.Substring(inicio - 1, largo)
        End Function

        Public Shared Function Izquierda(texto As String, largo As Integer) As String
            If largo <= 0 Then Return ""
            Return If(largo >= texto.Length, texto, texto.Substring(0, largo))
        End Function

        Public Shared Function Derecha(texto As String, largo As Integer) As String
            If largo <= 0 Then Return ""
            Return If(largo >= texto.Length, texto, texto.Substring(texto.Length - largo))
        End Function

        ''' <summary>Replicate: repite el texto hasta llenar el tamaño.</summary>
        Public Shared Function Replicar(relleno As String, tamano As Integer) As String
            If tamano <= 0 OrElse String.IsNullOrEmpty(relleno) Then Return ""
            Dim sb As New StringBuilder()
            While sb.Length < tamano
                sb.Append(relleno)
            End While
            Return sb.ToString(0, tamano)
        End Function

        ''' <summary>
        ''' Ajusta de Comun\SD_MOD_UTILES02.bas. Centrado reparte el relleno con la división de VB6, que al pasar a
        ''' Integer redondea al par: (40 - 5) / 2 = 17.5 da 18.
        ''' </summary>
        Public Shared Function Ajusta(cadena As String, tam As Integer, Optional posicion As AjusteTexto = AjusteTexto.RellenoIzquierda, Optional caracter As String = " ") As String
            cadena = If(cadena, "")
            If tam > cadena.Length Then
                Select Case posicion
                    Case AjusteTexto.Centrado
                        Dim relleno = Replicar(caracter, CInt(Math.Round((tam - cadena.Length) / 2, MidpointRounding.ToEven)))
                        Dim r = relleno & cadena & relleno
                        Return If(r.Length < tam, caracter & r, Izquierda(r, tam))
                    Case AjusteTexto.RellenoDerecha
                        Return Izquierda(cadena & Replicar(caracter, tam), tam)
                    Case Else
                        Return Derecha(Replicar(caracter, tam) & cadena, tam)
                End Select
            End If
            Return If(posicion = AjusteTexto.RellenoDerecha, Izquierda(cadena, tam), Derecha(cadena, tam))
        End Function

        ''' <summary>
        ''' El ciclo de SDPrintInfoAPI que guarda cada nombre con su valor (SD_SaveInCollection): un nombre repetido se
        ''' queda con el último valor y las mayúsculas no cuentan, como en una Collection de VB6.
        ''' </summary>
        Public Shared Function Variables(parametros As String, valores As String) As Dictionary(Of String, String)
            Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim p = If(parametros, "")
            Dim v = If(valores, "")
            While p <> ""
                Dim cod = Lateral(p, "|")
                If cod = "" AndAlso p <> "" AndAlso p.IndexOf("|"c) < 0 Then cod = p
                Dim valor = Lateral(v, "|")
                If valor = "" AndAlso v <> "" AndAlso v.IndexOf("|"c) < 0 Then valor = v
                d(cod) = valor
                p = Medio(p, cod.Length + 2)
                v = Medio(v, valor.Length + 2)
            End While
            Return d
        End Function

        Private Shared Function Valor(d As Dictionary(Of String, String), clave As String) As String
            Dim v As String = Nothing
            Return If(d.TryGetValue(clave, v), v, "")
        End Function

        Private Shared Function CodigoInterno(token As String) As String
            Return If(token.Length >= 7, Medio(token, 5, token.Length - 7), "")
        End Function

        ''' <summary>
        ''' SDPrintInfoAPI sin la impresión: arma el texto. <paramref name="inicioRepeticion"/> es 1 cuando VB6 tomó el
        ''' formato del local global (-1), porque reusaba ese contador para numerar las líneas repetidas.
        ''' <paramref name="imprimir"/> solo decide si se incluye el código QR.
        ''' </summary>
        Public Shared Function Interpretar(lineas As IEnumerable(Of LineaFormato), parametros As String, valores As String,
                                           Optional inicioRepeticion As Integer = 0, Optional imprimir As Boolean = False, Optional clave As String = "") As String
            Dim vars = Variables(parametros, valores)
            Dim filas = lineas.OrderBy(Function(l) l.Pos).ToList()
            Dim salida As New StringBuilder()
            Dim linea As New StringBuilder()
            Dim nl As New StringBuilder()
            Dim ix = inicioRepeticion
            Dim i = 0
            While i < filas.Count
                Dim fila = filas(i)
                Dim texto As String
                Dim saltar = False
                If fila.Multiple > -1 Then
                    ' Busca el primer dato de la línea para saber si la variable de esta vuelta tiene valor.
                    Dim pos = ""
                    texto = If(fila.Linea, "").Replace("#", ix.ToString(Globalization.CultureInfo.InvariantCulture))
                    Dim cod = ""
                    While texto <> ""
                        cod = Lateral(texto, "|")
                        If cod = "" AndAlso texto <> "" Then cod = texto
                        If Izquierda(cod, 3).ToUpperInvariant().Contains("$"c) Then
                            texto = Medio(texto, cod.Length + 2)
                        Else
                            texto = ""
                        End If
                    End While
                    If cod <> "" Then pos = Valor(vars, CodigoInterno(cod))
                    If pos.Trim() = "" Then
                        If fila.Multiple = 0 OrElse ix > fila.Multiple Then saltar = True
                    Else
                        texto = If(fila.Linea, "").Replace("#", ix.ToString(Globalization.CultureInfo.InvariantCulture))
                    End If
                Else
                    texto = If(fila.Linea, "")
                End If

                If Not saltar Then
                    While texto <> ""
                        Dim cod = Lateral(texto, "|")
                        If cod = "" AndAlso texto <> "" Then cod = texto
                        Select Case Izquierda(cod, 3).ToUpperInvariant()
                            Case "NL$" : linea.Append(vbCrLf)
                            Case "TL$" : linea.Append(Esc).Append("!"c).Append(ChrW(CInt(Core.Vb6.Val(Medio(cod, 4)))))
                            Case "CT$" : linea.Append(Esc).Append("i"c)
                            Case "SR$" : linea.Append(Esc).Append("Kn")
                            Case "ER$" : linea.Append(Esc).Append("e"c).Append(ChrW(2))
                            Case "AC$" : linea.Append(AbrirCaja)
                            Case "QR$"
                                If imprimir Then linea.Append(CodigoQr(clave))
                            Case Else
                                If cod.Length > 7 Then
                                    Dim alineacion = Izquierda(cod, 1).ToUpperInvariant()
                                    Dim sangria = CInt(Core.Vb6.Val(Medio(cod, 2, 3)))
                                    Dim ancho = CInt(Core.Vb6.Val(Derecha(cod, 3)))
                                    Dim nombre = CodigoInterno(cod)
                                    Dim esTexto = Izquierda(nombre, 3) = "ET$"
                                    Dim dato = If(esTexto, Medio(nombre, 4), Valor(vars, nombre))
                                    If (fila.Multiple = -2 AndAlso dato.Trim() <> "") OrElse fila.Multiple <> -2 Then
                                        nl.Append(Valor(vars, nombre))
                                        Dim ajuste = If(alineacion = "I", AjusteTexto.RellenoDerecha, If(alineacion = "D", AjusteTexto.RellenoIzquierda, AjusteTexto.Centrado))
                                        linea.Append(New String(" "c, Math.Max(0, sangria))).Append(Ajusta(dato, ancho, ajuste))
                                    End If
                                    cod = Lateral(texto, "|")
                                    If cod = "" AndAlso texto <> "" AndAlso texto.IndexOf("|"c) < 0 Then cod = texto
                                End If
                        End Select
                        texto = Medio(texto, cod.Length + 2)
                    End While
                    If nl.ToString().Trim() <> "" OrElse fila.Multiple <> -2 Then
                        salida.Append(linea).Append(vbCrLf)
                        linea.Clear()
                    End If
                End If

                If saltar OrElse fila.Multiple < 0 Then
                    ix = 0
                    nl.Clear()
                    linea.Clear()
                    i += 1
                Else
                    ix += 1
                End If
            End While
            Return salida.ToString().Replace(AbrirCaja, "")
        End Function

        ''' <summary>Los comandos QR$ para impresoras Epson (funciones 165 a 182 de GS ( k).</summary>
        Private Shared Function CodigoQr(clave As String) As String
            Dim c = If(clave, "")
            Dim gs = ChrW(29) & "(" & "k"
            Return Esc & "@" & Esc & "a" & ChrW(1) &
                   gs & ChrW(4) & ChrW(0) & "1A2" & ChrW(0) &
                   gs & ChrW(3) & ChrW(0) & "1C" & ChrW(4) &
                   gs & ChrW(3) & ChrW(0) & "1E0" &
                   gs & ChrW((c.Length + 3) Mod 256) & ChrW((c.Length + 3) \ 256) & "1P0" & c &
                   gs & ChrW(3) & ChrW(0) & "1Q0" &
                   gs & ChrW(3) & ChrW(0) & "1R0"
        End Function

        ''' <summary>
        ''' El texto sin los códigos de control de la impresora, para verlo en pantalla: cambio de letra, corte y
        ''' retrocesos. VB6 los mostraba como caracteres raros.
        ''' </summary>
        Public Shared Function ParaPantalla(texto As String) As String
            Dim sb As New StringBuilder()
            Dim t = If(texto, "")
            Dim i = 0
            While i < t.Length
                Dim c = t(i)
                If c = Esc AndAlso i + 1 < t.Length Then
                    Select Case t(i + 1)
                        Case "!"c : i += 3
                        Case "e"c : i += 3
                        Case "K"c : i += 3
                        Case "p"c : i += 5
                        Case Else : i += 2
                    End Select
                    Continue While
                End If
                If c = vbCr OrElse c = vbLf OrElse c = vbTab OrElse c >= " "c Then sb.Append(c)
                i += 1
            End While
            Return sb.ToString()
        End Function

    End Class

End Namespace
