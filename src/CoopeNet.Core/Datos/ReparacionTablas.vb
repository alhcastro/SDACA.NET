Imports System.Threading
Imports Dapper

Namespace Datos

    Public NotInheritable Class TablaBase
        Public Property Nombre As String = ""
        ''' <summary>MyISAM, InnoDB, o vacío en las vistas.</summary>
        Public Property Motor As String = ""

        Public Overrides Function ToString() As String
            Return If(Motor.Length = 0, $"{Nombre} (vista)", If(String.Equals(Motor, "MyISAM", StringComparison.OrdinalIgnoreCase), Nombre, $"{Nombre} ({Motor})"))
        End Function
    End Class

    ''' <summary>Una fila del resultado de REPAIR TABLE.</summary>
    Public NotInheritable Class ResultadoReparacion
        Public Property Table As String = ""
        Public Property Op As String = ""
        Public Property Msg_type As String = ""
        Public Property Msg_text As String = ""

        Public Overrides Function ToString() As String
            Return $"{Table} - {Op} - {Msg_type} - {Msg_text}"
        End Function
    End Class

    ''' <summary>
    ''' Reparar BD. Migración de SD0001\frmRepararT.frm: lista las tablas del catálogo y repara las marcadas
    ''' con la misma sentencia de VB6, o arma las líneas de myisamchk para correrlas con el servidor detenido.
    ''' REPAIR solo sirve en MyISAM: en InnoDB y en las vistas MySQL responde que no lo admite.
    ''' </summary>
    Public NotInheritable Class ReparacionTablas

        Public Const Todas As String = "<<Todas>>"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>Como SD_Combos: "&lt;&lt;Todas&gt;&gt;" o vacío lista todo; si no, las que empiezan con el texto.</summary>
        Friend Shared Function Patron(filtro As String) As String
            Dim texto = If(filtro, "").Trim()
            If texto.Length = 0 OrElse texto = Todas Then Return "%"
            Return texto.Replace("\", "\\").Replace("%", "\%").Replace("_", "\_") & "%"
        End Function

        Public Async Function TablasAsync(filtro As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of TablaBase))
            Const sql As String = "SELECT TABLE_NAME AS Nombre, IFNULL(ENGINE, '') AS Motor FROM information_schema.tables " &
                                  "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME LIKE @patron ORDER BY TABLE_NAME"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of TablaBase)(New CommandDefinition(sql, New With {.patron = Patron(filtro)}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>La carpeta del catálogo en el servidor, para proponerla a myisamchk.</summary>
        Public Async Function CarpetaDatosAsync(Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim fila = Await cn.QueryFirstAsync(Of (Datos As String, Catalogo As String))(New CommandDefinition("SELECT @@datadir, DATABASE()", cancellationToken:=ct)).ConfigureAwait(False)
                Return IO.Path.Combine(If(fila.Datos, ""), If(fila.Catalogo, ""))
            End Using
        End Function

        Friend Shared Function SentenciaReparar(tabla As String) As String
            Return $"REPAIR NO_WRITE_TO_BINLOG TABLE `{tabla.Replace("`", "``")}` QUICK EXTENDED USE_FRM"
        End Function

        ''' <summary>Puede tardar en tablas grandes: MySQL bloquea la tabla mientras la repara.</summary>
        Public Async Function RepararAsync(tabla As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of ResultadoReparacion))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of ResultadoReparacion)(New CommandDefinition(SentenciaReparar(tabla), commandTimeout:=0, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>
        ''' Las líneas de RepMyIs. myisamchk trabaja sobre los archivos .MYI de la carpeta de datos del servidor,
        ''' así que solo sirve en ese equipo y con MySQL detenido.
        ''' </summary>
        Public Shared Function ComandosMyIsamChk(directorio As String, tablas As IEnumerable(Of String), recuperar As Boolean, rapido As Boolean) As List(Of String)
            Dim opciones = If(recuperar, "-r ", "") & If(rapido, "-q ", "")
            Dim carpeta = If(directorio, "").Trim().TrimEnd("\"c)
            Return tablas.Select(Function(t) $"myisamchk {opciones}""{carpeta}\{t}.MYI""").ToList()
        End Function

    End Class

End Namespace
