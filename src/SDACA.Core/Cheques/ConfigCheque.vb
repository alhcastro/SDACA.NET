Imports System.Globalization

Namespace Cheques

    ''' <summary>Fila de eccchequesconfig, con los nombres de sus columnas.</summary>
    Public NotInheritable Class FilaConfigCheque
        Public Property Local As Long
        Public Property Banco As Integer
        Public Property Campo As Integer
        Public Property Tipo As String = ""
        Public Property Imprime As String = ""
        Public Property Ctop As Double
        Public Property Cleft As Double
        Public Property Cheight As Double
        Public Property Cwidth As Double
        Public Property Fuente As String = ""
        Public Property Size As Double
        Public Property Bold As String = ""
        Public Property Italic As String = ""
        Public Property Alineacion As String = ""
        Public Property Formato As String = ""
    End Class

    Public Enum TipoFormatoCampo
        Fecha
        Monto
        Texto
    End Enum

    ''' <summary>Un campo impreso en el cheque. La posición y el tamaño van en twips, como en la tabla.</summary>
    Public NotInheritable Class CampoCheque
        Public Const TwipsPorCm As Double = 572
        ''' <summary>Tamaño de FrPresenta, que limita la posición de los campos.</summary>
        Public Const VistaAncho As Double = 10005
        Public Const VistaAlto As Double = 4125

        Public Sub New(indice As Integer)
            Me.Indice = indice
        End Sub

        Public ReadOnly Property Indice As Integer
        Public Property Imprime As Boolean = True
        Public Property Arriba As Double
        Public Property Izquierda As Double
        Public Property Alto As Double = 300
        Public Property Ancho As Double
        Public Property Fuente As String = "MS Sans Serif"
        Public Property Tamano As Double = 10
        Public Property Negrita As Boolean
        Public Property Cursiva As Boolean
        ''' <summary>0 izquierda, 1 derecha, 2 centrada.</summary>
        Public Property Alineacion As Integer
        ''' <summary>Fechas y montos guardan la máscara. Los textos, la posición en <see cref="Opciones"/>.</summary>
        Public Property Formato As String = "0"

        Public ReadOnly Property TipoFormato As TipoFormatoCampo
            Get
                Select Case Indice
                    Case 0, 5 : Return TipoFormatoCampo.Fecha
                    Case 2, 10, 11 : Return TipoFormatoCampo.Monto
                    Case Else : Return TipoFormatoCampo.Texto
                End Select
            End Get
        End Property

        ''' <summary>Igual que SD_CargarFormato, incluida la falta de ortografía que ve el usuario.</summary>
        Public ReadOnly Property Opciones As IReadOnlyList(Of String)
            Get
                Select Case TipoFormato
                    Case TipoFormatoCampo.Fecha
                        Return {"Long Date", "Short Date", "dd/MM/yyyy", "dd/MMM/yyyy", "dddd, dd/MMM/yyyy", "MMMM dd, yyyy"}
                    Case TipoFormatoCampo.Monto
                        Return {"#,##0.00", "0.00", "¢#,##0.00", "$#,##0.00"}
                    Case Else
                        Return {"Inicialies en Mayuscula", "Todo en Mayuscula", "Todo en Minuscula"}
                End Select
            End Get
        End Property

        ''' <summary>
        ''' Posición del formato en <see cref="Opciones"/>. La columna es varchar(16), así que
        ''' "dddd, dd/MMM/yyyy" vuelve con un carácter menos y se reconoce por el comienzo.
        ''' </summary>
        Public Function IndiceFormato() As Integer
            If TipoFormato = TipoFormatoCampo.Texto Then
                Dim n As Integer
                Integer.TryParse(Formato, n)
                Return If(n < 0 OrElse n >= Opciones.Count, 0, n)
            End If
            If String.IsNullOrEmpty(Formato) OrElse Formato = "0" Then Return 0
            For i = 0 To Opciones.Count - 1
                If Opciones(i).StartsWith(Formato, StringComparison.Ordinal) Then Return i
            Next
            Return 0
        End Function

        Public Sub ElegirFormato(indiceOpcion As Integer)
            Formato = If(TipoFormato = TipoFormatoCampo.Texto, indiceOpcion.ToString(CultureInfo.InvariantCulture), Opciones(indiceOpcion))
        End Sub

        ''' <summary>El "Top" de la pantalla mide hasta el borde de abajo del campo.</summary>
        Public ReadOnly Property ArribaCm As Double
            Get
                Return (Arriba + Alto) / TwipsPorCm
            End Get
        End Property

        ''' <returns>El límite en cm si se pasó, para avisar. Nothing si el valor cabe.</returns>
        Public Function PonerArribaCm(cm As Double) As Double?
            Dim limite As Double? = Nothing
            Arriba = Redondear(cm) * TwipsPorCm - Alto
            If Arriba < 0 Then
                Arriba = 0
            ElseIf Arriba - 70 > VistaAlto Then
                limite = (VistaAlto - 70) / TwipsPorCm
                Arriba = limite.Value * TwipsPorCm - Alto
            End If
            Return limite
        End Function

        Public Function PonerIzquierdaCm(cm As Double) As Double?
            Izquierda = Redondear(cm) * TwipsPorCm
            If Izquierda + Ancho <= VistaAncho Then Return Nothing
            Izquierda = VistaAncho - Ancho
            Return Izquierda / TwipsPorCm
        End Function

        ''' <summary>Cambia el alto sin mover el borde de abajo, como TxtHeight_Change.</summary>
        Public Function PonerAltoCm(cm As Double) As Double?
            Dim abajo = Arriba + Alto
            Dim limite As Double? = Nothing
            Alto = Redondear(cm) * TwipsPorCm
            If Alto > VistaAlto Then
                Alto = VistaAlto - 60
                limite = Alto / TwipsPorCm
            End If
            Arriba = Math.Max(0, abajo - Alto)
            Return limite
        End Function

        Public Function PonerAnchoCm(cm As Double) As Double?
            Ancho = Redondear(cm) * TwipsPorCm
            If Izquierda + Ancho <= VistaAncho Then Return Nothing
            Ancho = VistaAncho - Izquierda
            Return Ancho / TwipsPorCm
        End Function

        ''' <summary>Igual que FormatNumber, que deja dos decimales.</summary>
        Private Shared Function Redondear(cm As Double) As Double
            Return Math.Round(cm, 2, MidpointRounding.AwayFromZero)
        End Function

    End Class

    ''' <summary>
    ''' Configuración de impresión de un banco. Migración de la lectura y grabación de
    ''' SD1501\FrmConfigCheque.frm. Las filas 100 y 101 reusan columnas con otro sentido:
    ''' italic es la posición en la hoja, alineacion la forma de impresión, y bold e italic
    ''' indican si se imprimen el detalle y la contabilidad.
    ''' </summary>
    Public NotInheritable Class ConfigCheque

        ''' <summary>Local de las filas: el del usuario o -1 si son las generales.</summary>
        Public Property Local As Long
        ''' <summary>El banco no tiene filas en ningún local.</summary>
        Public Property SinRegistro As Boolean
        Public Property AltoCm As Double
        Public Property AnchoCm As Double
        ''' <summary>Posición en la lista Vertical, Horizontal, Centrada, Derecha, Izquierda.</summary>
        Public Property FormaImpresion As Integer
        ''' <summary>0 abajo, 1 arriba.</summary>
        Public Property PosicionHoja As Integer
        Public Property ImprimeDetalle As Boolean
        Public Property ImprimeContabilidad As Boolean
        Public Property AltoDetalleCm As Double
        Public Property AltoContabilidadCm As Double
        Public ReadOnly Property Campos As CampoCheque()

        Public Sub New()
            ' Posición de LblP en el diseño. Los campos 0 a 4 la reciben de Predeterminar.
            Dim diseno = {
                (0.0, 0.0, 0.0), (0.0, 0.0, 0.0), (0.0, 0.0, 0.0), (0.0, 0.0, 0.0), (0.0, 0.0, 0.0),
                (2595.0, 3315.0, 1470.0), (4245.0, 2925.0, 1470.0), (5025.0, 3330.0, 1110.0),
                (7215.0, 2730.0, 1470.0), (7320.0, 3120.0, 1275.0), (8970.0, 3180.0, 855.0), (8970.0, 2715.0, 855.0)
            }
            Campos = New CampoCheque(11) {}
            For i = 0 To 11
                Campos(i) = New CampoCheque(i) With {.Izquierda = diseno(i).Item1, .Arriba = diseno(i).Item2, .Ancho = diseno(i).Item3}
                Predeterminar(i)
            Next
        End Sub

        ''' <summary>CmdPredeterminados_Click. Los campos 5 a 11 solo recuperan el estilo.</summary>
        Public Sub Predeterminar(indice As Integer)
            Dim c = Campos(indice)
            c.Fuente = "MS Sans Serif"
            c.Tamano = 10
            c.Negrita = False
            c.Cursiva = False
            c.Imprime = True
            c.Alineacion = 0
            c.ElegirFormato(0)
            Select Case indice
                Case 0
                    c.Alineacion = 2
                    Colocar(c, 4975, 1073, 300, 4635)
                Case 1
                    Colocar(c, 1888, 1530, 300, 5320)
                Case 2
                    c.Alineacion = 1
                    Colocar(c, 7608, 1530, 300, 2002)
                Case 3
                    Colocar(c, 1305, 1995, 300, 7500)
                Case 4
                    c.Fuente = "Arial"
                    c.Tamano = 4
                    Colocar(c, 629, 2732, 150, 3255)
            End Select
        End Sub

        Private Shared Sub Colocar(c As CampoCheque, izquierda As Double, arriba As Double, alto As Double, ancho As Double)
            c.Izquierda = izquierda
            c.Arriba = arriba
            c.Alto = alto
            c.Ancho = ancho
        End Sub

        ''' <summary>Índice de LblP para una fila, o -1 si la fila no es de un campo.</summary>
        Public Shared Function IndiceDe(tipo As String, campo As Integer) As Integer
            Dim indice As Integer
            Select Case tipo
                Case "E" : indice = campo
                Case "D" : indice = campo - 300 + 5
                Case "C" : indice = campo - 200 + 7
                Case Else : Return -1
            End Select
            Return If(indice < 0 OrElse indice > 11, -1, indice)
        End Function

        Private Shared Function CodigoDe(indice As Integer) As (Campo As Integer, Tipo As String)
            If indice <= 4 Then Return (indice, "E")
            If indice <= 7 Then Return (indice + 295, "D")
            Return (indice + 193, "C")
        End Function

        Public Shared Function DesdeFilas(filas As IEnumerable(Of FilaConfigCheque), local As Long) As ConfigCheque
            Dim config As New ConfigCheque With {.Local = local}
            For Each f In filas
                If f.Tipo = "A" Then
                    config.LeerGeneral(f)
                Else
                    Dim indice = IndiceDe(f.Tipo, f.Campo)
                    If indice >= 0 Then config.LeerCampo(indice, f)
                End If
            Next
            Return config
        End Function

        Private Sub LeerGeneral(f As FilaConfigCheque)
            If f.Campo = 100 Then
                AltoCm = f.Cheight
                AnchoCm = f.Cwidth
                FormaImpresion = Math.Max(0, Numero(f.Alineacion) - 1)
                PosicionHoja = Numero(f.Italic)
            ElseIf f.Campo = 101 Then
                ImprimeDetalle = f.Bold = "1"
                ImprimeContabilidad = f.Italic = "1"
                AltoDetalleCm = f.Ctop
                AltoContabilidadCm = f.Cheight - f.Ctop
            End If
        End Sub

        Private Sub LeerCampo(indice As Integer, f As FilaConfigCheque)
            Dim c = Campos(indice)
            c.Imprime = f.Imprime = "1"
            c.Alineacion = Numero(f.Alineacion)
            ' Los campos de contabilidad son filas de una tabla: el original no usa su Top y al leer lo fija en 1000.
            c.Arriba = If(f.Tipo = "C", 1000, f.Ctop)
            c.Izquierda = f.Cleft
            c.Alto = f.Cheight
            c.Ancho = f.Cwidth
            c.Fuente = f.Fuente
            c.Tamano = f.Size
            c.Negrita = f.Bold = "1"
            c.Cursiva = f.Italic = "1"
            c.Formato = f.Formato
            ' Fechas y montos pueden venir con "0" o vacíos: el original los cambia por la primera opción.
            If c.TipoFormato <> TipoFormatoCampo.Texto Then c.ElegirFormato(c.IndiceFormato())
        End Sub

        Private Shared Function Numero(valor As String) As Integer
            Dim n As Integer
            Integer.TryParse(valor, n)
            Return n
        End Function

        Public Function AFilas(banco As Integer) As List(Of FilaConfigCheque)
            Dim filas As New List(Of FilaConfigCheque) From {
                New FilaConfigCheque With {
                    .Local = Local, .Banco = banco, .Campo = 100, .Tipo = "A", .Imprime = "1",
                    .Cheight = AltoCm, .Cwidth = AnchoCm,
                    .Italic = PosicionHoja.ToString(CultureInfo.InvariantCulture),
                    .Alineacion = (FormaImpresion + 1).ToString(CultureInfo.InvariantCulture)
                },
                New FilaConfigCheque With {
                    .Local = Local, .Banco = banco, .Campo = 101, .Tipo = "A", .Imprime = "1",
                    .Ctop = AltoDetalleCm, .Cheight = AltoContabilidadCm + AltoDetalleCm,
                    .Bold = Bandera(ImprimeDetalle), .Italic = Bandera(ImprimeContabilidad)
                }
            }
            For Each c In Campos
                Dim codigo = CodigoDe(c.Indice)
                filas.Add(New FilaConfigCheque With {
                    .Local = Local, .Banco = banco, .Campo = codigo.Campo, .Tipo = codigo.Tipo, .Imprime = Bandera(c.Imprime),
                    .Ctop = c.Arriba, .Cleft = c.Izquierda, .Cheight = c.Alto, .Cwidth = c.Ancho,
                    .Fuente = c.Fuente, .Size = c.Tamano, .Bold = Bandera(c.Negrita), .Italic = Bandera(c.Cursiva),
                    .Alineacion = c.Alineacion.ToString(CultureInfo.InvariantCulture), .Formato = c.Formato
                })
            Next
            Return filas
        End Function

        Private Shared Function Bandera(valor As Boolean) As String
            Return If(valor, "1", "0")
        End Function

    End Class

    ''' <summary>Cómo se escribe cada campo según su formato. Lo usan la vista previa y la impresión.</summary>
    Public NotInheritable Class FormatoCheque

        Private Sub New()
        End Sub

        Public Shared Function Fecha(valor As Date, formato As String) As String
            Select Case formato
                Case "Long Date" : Return valor.ToString("D", CultureInfo.CurrentCulture)
                Case "Short Date" : Return valor.ToString("d", CultureInfo.CurrentCulture)
                Case Else : Return valor.ToString(formato, CultureInfo.CurrentCulture)
            End Select
        End Function

        Public Shared Function Monto(valor As Double, formato As String) As String
            Return valor.ToString(formato, CultureInfo.CurrentCulture)
        End Function

        ''' <param name="opcion">0 iniciales, 1 mayúsculas, 2 minúsculas.</param>
        Public Shared Function Texto(valor As String, opcion As Integer) As String
            Select Case opcion
                Case 1 : Return valor.ToUpper(CultureInfo.CurrentCulture)
                Case 2 : Return valor.ToLower(CultureInfo.CurrentCulture)
                Case Else : Return Iniciales(valor)
            End Select
        End Function

        ''' <summary>USpaceCase de Comun\SD_MOD_UTILES02.bas.</summary>
        Public Shared Function Iniciales(valor As String) As String
            Dim letras = If(valor, "").ToLower(CultureInfo.CurrentCulture).ToCharArray()
            For i = 0 To letras.Length - 1
                If i = 0 OrElse letras(i - 1) = " "c Then letras(i) = Char.ToUpper(letras(i), CultureInfo.CurrentCulture)
            Next
            Return New String(letras)
        End Function

    End Class

End Namespace
