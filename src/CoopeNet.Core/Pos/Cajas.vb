Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos
Imports CoopeNet.Core.Seguridad

Namespace Pos

    ''' <summary>Option1 de frmMantCajas: el índice del botón es el valor de poscajas.Facturacion.</summary>
    Public Enum ModoFacturacion
        Pos = 1
        Facturacion = 2
        Tactil = 3
    End Enum

    ''' <summary>Una caja (poscajas) con uno de sus usuarios (poscajasusuarios).</summary>
    Public NotInheritable Class Caja
        Public Property Estacion As String = ""
        ''' <summary>poscajas.Caja: char(3), único en todos los locales.</summary>
        Public Property Numero As String = ""
        ''' <summary>Número de la caja seguido de seis dígitos: la caja 86 empieza en 86000000.</summary>
        Public Property Consecutivo As Double
        Public Property Facturacion As ModoFacturacion = ModoFacturacion.Pos
        Public Property Activa As Boolean
        Public Property Uid As Long
        Public Property FC As Date?
        Public Property FM As Date?
        ''' <summary>PCom1 ("Usar consola"): VB6 oculta la casilla pero conserva el valor.</summary>
        Public Property Consola As Boolean
        Public Property LineaRapida As Boolean
    End Class

    Public NotInheritable Class OpcionCaja
        Public Property Codigo As String = ""
        Public Property Descripcion As String = ""
        Public Overrides Function ToString() As String
            Return Descripcion
        End Function
    End Class

    Public NotInheritable Class CatalogosCaja
        Public Property Estaciones As New List(Of OpcionCaja)
        Public Property Usuarios As New List(Of OpcionCaja)
    End Class

    Public Enum ResultadoAutorizacion
        Autorizado
        ''' <summary>El usuario o la contraseña no abren la base.</summary>
        NoAutorizado
        ''' <summary>Entra a la base pero le falta el 5000 o el 5030 en el local.</summary>
        SinPrivilegio
    End Enum

    ''' <summary>
    ''' Cajas de SD0502. Migración de SD0502\frmMantCajas.frm (SD_Combos, SD_Cargar, SD_Guardar, SD_Borrar,
    ''' cmbEstacion_Click, ValidaConsec y ExisteCaja) y de frmModConsec.frm. El panel de factura electrónica
    ''' que VB6 abre con un clic en "Modificada" (solo DEBUG o UID 0) no se migra.
    ''' </summary>
    Public NotInheritable Class CajasRepositorio

        Public Const EstacionConsola As String = "CONSOLA"
        Public Const Replica As String = "poscajas"
        Public Const AvisoReinicio As String = "Por favor, reinicie el sistema POS"

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        Private Shared Function B(valor As Boolean) As String
            Return If(valor, "'1'", "'0'")
        End Function

        ''' <summary>SD_Combos: CONSOLA primero, las estaciones activas y los usuarios con cuenta en MySQL.</summary>
        Public Async Function CatalogosAsync(Optional ct As CancellationToken = Nothing) As Task(Of CatalogosCaja)
            Dim c As New CatalogosCaja()
            c.Estaciones.Add(New OpcionCaja With {.Codigo = EstacionConsola, .Descripcion = EstacionConsola})
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                c.Estaciones.AddRange(Await cn.QueryAsync(Of OpcionCaja)(New CommandDefinition(
                    "SELECT estacion AS Codigo, descripcion AS Descripcion FROM gatestaciones WHERE activo = '1' ORDER BY descripcion", cancellationToken:=ct)).ConfigureAwait(False))
                c.Usuarios.AddRange(Await cn.QueryAsync(Of OpcionCaja)(New CommandDefinition(
                    "SELECT CAST(u.uid AS CHAR) AS Codigo, CONCAT(u.uid, ' - ', CAST(us.User AS CHAR)) AS Descripcion FROM mysql.user us " &
                    "INNER JOIN gatusuarios u ON CAST(u.usuario AS CHAR) = CAST(us.user AS CHAR) " &
                    "INNER JOIN gacdp D ON D.id = u.id WHERE us.Select_priv = 'Y' AND D.activo = '1' ORDER BY u.uid", cancellationToken:=ct)).ConfigureAwait(False))
            End Using
            Return c
        End Function

        ''' <summary>Buscar de la barra. Columnas: Nombre, Caja, Descripción, Usuario (uid) y Estación.</summary>
        Public Async Function BuscarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String =
                "SELECT D.Nombre, a.caja AS Caja, IFNULL(b.descripcion, 'CONSOLA') AS `Descripción`, U.uidc AS Usuario, a.estacion AS `Estación` " &
                "FROM ((poscajas a LEFT JOIN gatestaciones b ON a.estacion = b.estacion) " &
                "INNER JOIN poscajasusuarios U ON a.Estacion = U.Estaci AND a.Caja = U.CajaUs AND a.local = U.local " &
                "INNER JOIN gatusuarios c ON U.uidc = c.uid " &
                "INNER JOIN mysql.user m ON m.User = c.Usuario) INNER JOIN gacdp D ON c.id = D.id " &
                "WHERE a.local = @local AND D.activo = '1'"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        Private NotInheritable Class FilaCaja
            Public Property Estacion As String
            Public Property Caja As String
            Public Property Consecutivo As Double
            Public Property Activo As String
            Public Property Uidc As Long
            Public Property FC As Date?
            Public Property FM As Date?
            Public Property Facturacion As String
            Public Property PCom1 As String
            Public Property LineaRap As String
        End Class

        ''' <summary>SD_Cargar. Nothing si la combinación de estación, caja y usuario no existe en el local.</summary>
        Public Async Function CargarAsync(estacion As String, uid As Long, numero As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Caja)
            Const sql As String =
                "SELECT a.Estacion, a.Caja, a.Consecutivo, U.Activo, U.UIDC AS Uidc, a.FC, a.FM, a.Facturacion, a.PCom1, a.LineaRap " &
                "FROM poscajas a INNER JOIN poscajasusuarios U ON a.Estacion = U.Estaci AND a.Caja = U.CajaUs AND a.local = U.local " &
                "WHERE a.estacion = @estacion AND a.caja = @numero AND U.uidc = @uid AND U.local = @local LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim f = Await cn.QueryFirstOrDefaultAsync(Of FilaCaja)(New CommandDefinition(sql, New With {estacion, numero, uid, local}, cancellationToken:=ct)).ConfigureAwait(False)
                If f Is Nothing Then Return Nothing
                Dim modo = CInt(Vb6.Val(f.Facturacion))
                Return New Caja With {
                    .Estacion = f.Estacion, .Numero = f.Caja, .Consecutivo = f.Consecutivo, .Activa = f.Activo = "1", .Uid = f.Uidc,
                    .FC = f.FC, .FM = f.FM, .Facturacion = If([Enum].IsDefined(GetType(ModoFacturacion), modo), CType(modo, ModoFacturacion), ModoFacturacion.Pos),
                    .Consola = f.PCom1 = "1", .LineaRapida = f.LineaRap = "1"
                }
            End Using
        End Function

        ''' <summary>cmbEstacion_Click: la caja de la estación en el local, o "" si no tiene.</summary>
        Public Async Function CajaDeEstacionAsync(estacion As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(
                    "SELECT caja FROM poscajas WHERE estacion = @estacion AND local = @local LIMIT 1", New With {estacion, local}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ''' <summary>VB6 lee el consecutivo por estación sin filtrar el local.</summary>
        Public Async Function ConsecutivoDeEstacionAsync(estacion As String, Optional ct As CancellationToken = Nothing) As Task(Of Double)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of Double?)(New CommandDefinition(
                    "SELECT consecutivo FROM poscajas WHERE estacion = @estacion LIMIT 1", New With {estacion}, cancellationToken:=ct)).ConfigureAwait(False), 0)
            End Using
        End Function

        ''' <summary>Número propuesto para una estación sin caja: la mayor del local más uno.</summary>
        Public Async Function SiguienteCajaAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Convert.ToString(Await cn.ExecuteScalarAsync(Of Object)(New CommandDefinition(
                    "SELECT IFNULL(MAX(CAST(caja AS UNSIGNED)) + 1, 1) FROM poscajas WHERE local = @local", New With {local}, cancellationToken:=ct)).ConfigureAwait(False), Invariante)
            End Using
        End Function

        ''' <summary>
        ''' ExisteCaja: la caja la tiene otra estación. VB6 solo mira el local, pero poscajas.Caja es único en
        ''' todos: con un número de otro local, el REPLACE borraría esa caja. Por eso aquí se mira en todos.
        ''' </summary>
        Public Async Function CajaOcupadaAsync(estacion As String, numero As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(
                    "SELECT COUNT(*) FROM poscajas WHERE estacion <> @estacion AND caja = @numero", New With {estacion, numero}, cancellationToken:=ct)).ConfigureAwait(False) > 0
            End Using
        End Function

        ''' <summary>
        ''' ValidaConsec: la última factura de la caja en posfacturaencabezado_4, o 0. VB6 tiene una segunda
        ''' consulta sobre posfacturaencabezado con un error de sintaxis ("and BETWEEN") que nunca devuelve
        ''' nada, así que no se migra.
        ''' </summary>
        Public Async Function UltimaFacturaAsync(numero As String, Optional ct As CancellationToken = Nothing) As Task(Of Double)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim valor = Await cn.ExecuteScalarAsync(Of Object)(New CommandDefinition(
                    "SELECT MAX(numfac) FROM posfacturaencabezado_4 WHERE LEFT(numfac, LENGTH(numfac) - 6) = @caja",
                    New With {.caja = CLng(Vb6.Val(numero))}, cancellationToken:=ct)).ConfigureAwait(False)
                Return Conversion.Val(Convert.ToString(valor, Invariante))
            End Using
        End Function

        ''' <summary>ValidaConsec: un consecutivo sin facturas empieza en la caja por un millón.</summary>
        Public Shared Function ConsecutivoInicial(numero As String) As Double
            Return Vb6.Val(numero) * 1000000.0
        End Function

        ''' <summary>El REPLACE de poscajas de SD_Guardar. FM queda en NOW().</summary>
        Friend Shared Function SentenciaCaja(c As Caja, local As Long) As String
            Dim fc = If(c.FC, Date.Now)
            Return "REPLACE INTO poscajas (Estacion, Caja, Consecutivo, Facturacion, FC, FM, Local, PCom1, LineaRap) VALUES (" &
                   String.Join(",", {Q(c.Estacion), Q(c.Numero.Trim()), Q(c.Consecutivo.ToString("0", Invariante)), $"'{CInt(c.Facturacion)}'",
                                     Q(fc.ToString("yyyy-MM-dd HH:mm:ss", Invariante)), "NOW()", $"'{local}'", B(c.Consola), B(c.LineaRapida)}) & ")"
        End Function

        ''' <summary>El REPLACE de poscajasusuarios de SD_Guardar.</summary>
        Friend Shared Function SentenciaUsuario(c As Caja, local As Long) As String
            Return "REPLACE INTO poscajasusuarios(Estaci,CajaUs,Activo,UIDC,Local) VALUES(" &
                   String.Join(",", {Q(c.Estacion), Q(c.Numero.Trim()), B(c.Activa), Q(c.Uid.ToString(Invariante)), local.ToString(Invariante)}) & ")"
        End Function

        ''' <summary>SD_Guardar: las dos sentencias y su réplica, las dos con el identificador poscajas.</summary>
        Public Async Function GuardarAsync(c As Caja, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                For Each sql In {SentenciaCaja(c, local), SentenciaUsuario(c, local)}
                    Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                    Await Replicador.ReplicarAsync(cn, Replica, sql, local, ct).ConfigureAwait(False)
                Next
            End Using
        End Function

        ''' <summary>SD_Borrar: quita al usuario de la caja de esa estación. Como en VB6, no se replica.</summary>
        Public Async Function QuitarUsuarioAsync(estacion As String, uid As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM poscajasusuarios WHERE estaci = @estacion AND uidc = @uid", New With {estacion, uid}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ' ---- frmModConsec ----

        ''' <summary>
        ''' Parte un consecutivo en caja y número de seis dígitos (Left y Right de frmModConsec). Nothing si
        ''' tiene menos de siete dígitos y no se puede partir.
        ''' </summary>
        Public Shared Function PartirConsecutivo(consecutivo As Double) As (Caja As String, Numero As String)?
            Dim texto = consecutivo.ToString("0", Invariante)
            If texto.Length <= 6 Then Return Nothing
            Return (texto.Substring(0, texto.Length - 6), texto.Substring(texto.Length - 6))
        End Function

        ''' <summary>
        ''' cmdValidar_Click: el usuario debe entrar a la base con su contraseña y tener el 5000 y el 5030 en
        ''' el local. root y Administrador no necesitan privilegios.
        ''' </summary>
        Public Async Function AutorizarAsync(login As String, clave As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of ResultadoAutorizacion)
            Dim uid As Long
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim cadena As New MySqlConnectionStringBuilder(cn.ConnectionString)
                Dim credenciales = CadenaConexion.ResolverCredenciales(login, clave)
                cadena.UserID = credenciales.UsuarioMySql
                cadena.Password = credenciales.Contrasena
                Try
                    Using prueba As New MySqlConnection(cadena.ConnectionString)
                        Await prueba.OpenAsync(ct).ConfigureAwait(False)
                    End Using
                Catch ex As MySqlException
                    Return ResultadoAutorizacion.NoAutorizado
                End Try
                If Privilegios.EsAdministrador(login) Then Return ResultadoAutorizacion.Autorizado
                uid = CLng(Vb6.Val(Convert.ToString(Await cn.ExecuteScalarAsync(Of Object)(New CommandDefinition(
                    "SELECT UID FROM gatusuarios WHERE USUARIO = @login", New With {login}, cancellationToken:=ct)).ConfigureAwait(False), Invariante)))
                Dim total = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(
                    "SELECT COUNT(uid) FROM gatusuariospriv2 WHERE uid = @hash AND pri IN (@p5000, @p5030)",
                    New With {.hash = Privilegios.HashUsuario(local, uid), .p5000 = Privilegios.HashPrivilegio(local, uid, 5000), .p5030 = Privilegios.HashPrivilegio(local, uid, 5030)},
                    cancellationToken:=ct)).ConfigureAwait(False)
                Return If(total >= 2, ResultadoAutorizacion.Autorizado, ResultadoAutorizacion.SinPrivilegio)
            End Using
        End Function

        ''' <summary>
        ''' cmdAceptar_Click: cambia el consecutivo de la estación, lo anota en posvitmovimientos y pide a la
        ''' estación que reinicie el POS. Devuelve el consecutivo nuevo.
        ''' </summary>
        Public Async Function CambiarConsecutivoAsync(estacion As String, caja As String, actual As String, nuevo As String,
                                                      local As Long, uid As Long, estacionSesion As String, Optional ct As CancellationToken = Nothing) As Task(Of Double)
            Dim consecutivo = Double.Parse(caja & nuevo, Invariante)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("UPDATE poscajas SET CONSECUTIVO = @consecutivo WHERE ESTACION = @estacion", New With {consecutivo, estacion}, cancellationToken:=ct)).ConfigureAwait(False)
                Await ArticulosRepositorio.RegistrarAsync(cn, local, uid, "Cambio de consecutivo", $"Cambio del {caja}{actual} al {caja}{nuevo}", estacionSesion, ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("UPDATE gatestaciones SET Apagado = @aviso WHERE ESTACION = @estacion", New With {.aviso = AvisoReinicio, estacion}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
            Return consecutivo
        End Function

    End Class

End Namespace
