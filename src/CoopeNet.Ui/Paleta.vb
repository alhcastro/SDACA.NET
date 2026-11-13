Imports System.Drawing

''' <summary>Lo que elige el usuario en el menú. Se guarda en preferencias.ini como TEMA.MODO.</summary>
Public Enum PreferenciaTema
    Claro
    Oscuro
    ''' <summary>Sigue el modo de aplicación de Windows.</summary>
    Sistema
End Enum

''' <summary>
''' Los colores de <see cref="Tema"/>. Hay dos, clara y oscura; Tema expone la que esté en uso con los mismos
''' nombres de siempre, así que las pantallas no cambian. La oscura aclara el acento y el rojo para que se
''' lean sobre fondo oscuro, y AcentoOscuro pasa a ser más claro: es el color de los botones al pasar el
''' ratón y de los textos destacados.
''' </summary>
Public NotInheritable Class Paleta

    Public Property Marino As Color
    Public Property MarinoClaro As Color
    Public Property TextoSobreMarino As Color
    Public Property Acento As Color
    Public Property AcentoOscuro As Color
    Public Property AcentoSuave As Color
    Public Property Peligro As Color
    Public Property PeligroSuave As Color
    Public Property Fondo As Color
    Public Property Superficie As Color
    Public Property CampoLectura As Color
    Public Property Borde As Color
    Public Property BordeFuerte As Color
    Public Property Texto As Color
    Public Property TextoTenue As Color
    ''' <summary>Campo que se está editando o dato a la vista (SystemColors.Info en VB6).</summary>
    Public Property Resaltado As Color
    ''' <summary>Fondo de una fila o celda marcada como buena, por ejemplo el precio del día.</summary>
    Public Property ExitoSuave As Color

    Private Sub New()
    End Sub

    Private Shared Function H(html As String) As Color
        Return ColorTranslator.FromHtml(html)
    End Function

    Public Shared ReadOnly Clara As New Paleta With {
        .Marino = H("#102A43"), .MarinoClaro = H("#1E3F5F"), .TextoSobreMarino = H("#B6C6D6"),
        .Acento = H("#0E7490"), .AcentoOscuro = H("#0B5C73"), .AcentoSuave = H("#E3F1F5"),
        .Peligro = H("#B42318"), .PeligroSuave = H("#FDECEA"),
        .Fondo = H("#F2F4F7"), .Superficie = Color.White, .CampoLectura = H("#F5F7FA"),
        .Borde = H("#D5DCE4"), .BordeFuerte = H("#B8C3CF"), .Texto = H("#1F2933"), .TextoTenue = H("#52667A"),
        .Resaltado = H("#FFFFE1"), .ExitoSuave = Color.FromArgb(198, 239, 206)
    }

    Public Shared ReadOnly Oscura As New Paleta With {
        .Marino = H("#0B1520"), .MarinoClaro = H("#18293A"), .TextoSobreMarino = H("#9FB3C8"),
        .Acento = H("#1789A8"), .AcentoOscuro = H("#4FB8D6"), .AcentoSuave = H("#14394A"),
        .Peligro = H("#F97066"), .PeligroSuave = H("#3D1E1C"),
        .Fondo = H("#12171D"), .Superficie = H("#1B222B"), .CampoLectura = H("#232C37"),
        .Borde = H("#323E4B"), .BordeFuerte = H("#4A5866"), .Texto = H("#E5E9ED"), .TextoTenue = H("#9AA8B6"),
        .Resaltado = H("#3A3520"), .ExitoSuave = H("#1E3B2B")
    }

    ''' <summary>Fondos: se cambian en BackColor al pasar de una paleta a la otra.</summary>
    Friend Function Fondos() As Color()
        Return {Marino, MarinoClaro, AcentoSuave, PeligroSuave, Fondo, Superficie, CampoLectura, Borde, BordeFuerte, Acento, AcentoOscuro, Resaltado, ExitoSuave}
    End Function

    ''' <summary>Textos: se cambian en ForeColor. El blanco no está: es texto sobre el acento o el marino en las dos.</summary>
    Friend Function Textos() As Color()
        Return {Texto, TextoTenue, TextoSobreMarino, Acento, AcentoOscuro, Peligro}
    End Function

End Class
