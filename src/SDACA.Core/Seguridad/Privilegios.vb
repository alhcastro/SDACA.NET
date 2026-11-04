Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports SDACA.Core.Datos

Namespace Seguridad

    ''' <summary>
    ''' Privilegios de usuario. Reproduce lo que hace VB6 al iniciar sesión y al abrir cada opción.
    ''' </summary>
    ''' <remarks>
    ''' <para>Un privilegio es un número: <c>Priv \ 1000</c> es el módulo y <c>Priv Mod 1000 = 0</c>
    ''' es el permiso para entrar al módulo. Ejemplo: 6000 = entrar a CXC, 6003 = una opción de CXC.</para>
    ''' <para>gatusuariospriv2 guarda hashes: <c>UID = MD5("local,uid")</c> y <c>PRI = MD5("local,uid,priv")</c>.</para>
    ''' <para>Fuentes VB6: <c>SDACAERP\Module01.bas → ObtenerUsuario</c> (carga) y
    ''' <c>Comun\SD_MOD_UTILES02.bas → SD_ValidarPriv</c> (validación).</para>
    ''' </remarks>
    Public NotInheritable Class Privilegios

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Shared Function HashUsuario(local As Long, uid As Long) As String
            Return Md5Vb6.Calcular(local & "," & uid)
        End Function

        Public Shared Function HashPrivilegio(local As Long, uid As Long, privilegio As Long) As String
            Return Md5Vb6.Calcular(local & "," & uid & "," & privilegio)
        End Function

        ''' <summary>Administrador y root reciben todos los privilegios de los módulos del local.</summary>
        Public Shared Function EsAdministrador(login As String) As Boolean
            Return String.Equals(login, "administrador", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(login, "root", StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Carga los privilegios del usuario, como <c>ObtenerUsuario</c> llena <c>Usuario.Privilegios</c>.
        ''' </summary>
        ''' <param name="login">Usuario con el que se inició sesión.</param>
        ''' <param name="local">Local donde inicia sesión (pLocal).</param>
        ''' <param name="localUsuario">Local al que pertenece el usuario (pLocalUsr).</param>
        ''' <param name="uid">Número de usuario.</param>
        ''' <param name="cedulaRegistro">Registro.Cedula tal como viene en la licencia, con guiones.</param>
        ''' <param name="usuariosGlobales">Parámetro server-usr-globales. En VB6 está desactivado.</param>
        ''' <param name="ct">Cancelación.</param>
        Public Async Function CargarAsync(login As String, local As Long, localUsuario As Long, uid As Long, cedulaRegistro As String,
                                          Optional usuariosGlobales As Boolean = False,
                                          Optional ct As CancellationToken = Nothing) As Task(Of ISet(Of String))
            Const candidatos As String =
                "FROM gatmoduloslocal A " &
                "INNER JOIN gatprivilegios B ON (B.Priv - (B.Priv % 1000)) / 1000 = A.Modulo AND (B.IDs = '' OR INSTR(B.IDs, @cedula) > 0) "

            Dim sql As String
            If EsAdministrador(login) Then
                sql = "SELECT B.Priv " & candidatos & "WHERE A.Local = @local ORDER BY B.Priv;"
            Else
                ' VB6 revisa cada candidato con un SELECT COUNT(UID) aparte y lo acepta solo si el
                ' resultado es exactamente 1. Aquí se hace lo mismo en una sola consulta.
                sql = "SELECT B.Priv " & candidatos &
                      If(usuariosGlobales, "WHERE ", "WHERE A.Local = @localUsuario AND ") &
                      "(SELECT COUNT(P.UID) FROM gatusuariospriv2 P WHERE P.UID = @hashUid AND P.PRI = MD5(CONCAT(@prefijo, B.Priv))) = 1 " &
                      If(usuariosGlobales, "GROUP BY B.Priv ", "") &
                      "ORDER BY B.Priv;"
            End If

            Dim parametros = New With {
                .cedula = If(cedulaRegistro, ""),
                .local = local,
                .localUsuario = localUsuario,
                .hashUid = HashUsuario(localUsuario, uid),
                .prefijo = localUsuario & "," & uid & ","
            }

            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of Long)(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                Return New HashSet(Of String)(filas.Select(Function(p) p.ToString(CultureInfo.InvariantCulture)), StringComparer.OrdinalIgnoreCase)
            End Using
        End Function

        ''' <summary>
        ''' Igual que <c>SD_ValidarPriv</c>. <paramref name="requeridos"/> es una lista como "6000;6003".
        ''' Con <paramref name="cualquiera"/> = True basta uno (OR, el valor predeterminado en VB6).
        ''' Con False hacen falta todos (AND). Una lista vacía devuelve False.
        ''' </summary>
        Public Shared Function Validar(privilegios As ISet(Of String), requeridos As String,
                                       Optional separador As String = ";", Optional cualquiera As Boolean = True) As Boolean
            Return Validar(Function(p) privilegios.Contains(p), requeridos, separador, cualquiera)
        End Function

        ''' <summary>
        ''' Igual que la otra sobrecarga, pero con una función de búsqueda. Sirve para la Collection de
        ''' VB6, que no deja recorrer sus llaves: <c>SD_LoadFromCollection(col, priv) = "1"</c>.
        ''' </summary>
        Public Shared Function Validar(tienePrivilegio As Func(Of String, Boolean), requeridos As String,
                                       Optional separador As String = ";", Optional cualquiera As Boolean = True) As Boolean
            Dim resultado = False
            Dim resto = If(requeridos, "")
            Do While resto.Length > 0
                ' Str_GetLateral: lo que hay antes del separador, o "" si no hay separador.
                Dim pos = resto.IndexOf(separador, StringComparison.Ordinal)
                Dim actual = If(pos >= 0, resto.Substring(0, pos), "")
                If actual.Length = 0 Then actual = resto

                If tienePrivilegio(actual) Then
                    resultado = True
                    If cualquiera Then Return True
                Else
                    resultado = False
                    If Not cualquiera Then Return False
                End If

                ' Mid(pPriv, Len(iPriv) + 2)
                Dim siguiente = actual.Length + separador.Length
                resto = If(siguiente < resto.Length, resto.Substring(siguiente), "")
            Loop
            Return resultado
        End Function

        ''' <summary>
        ''' Consulta directa a la base de datos de un solo privilegio. Es la versión corregida de
        ''' <c>Comun\SD_MOD_USU.bas → TienePrivilegio</c>, que no filtraba por el privilegio pedido.
        ''' El usuario 0 tiene todos los privilegios.
        ''' </summary>
        Public Async Function TieneAsync(local As Long, uid As Long, privilegio As Long, cedulaEmpresa As String,
                                         Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            If uid = 0 Then Return True

            Const sql As String =
                "SELECT COUNT(P.PRIV) FROM gatprivilegios P " &
                "INNER JOIN gatusuariospriv2 U ON U.uid = @hashUid AND U.PRI = MD5(CONCAT(@prefijo, P.Priv)) " &
                "WHERE (P.IDs = '' OR INSTR(P.IDs, @cedula) > 0) AND P.Priv = @privilegio;"

            Dim parametros = New With {
                .hashUid = HashUsuario(local, uid),
                .prefijo = local & "," & uid & ",",
                .cedula = If(cedulaEmpresa, "").Replace("-", ""),
                .privilegio = privilegio
            }

            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim total = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                Return total > 0
            End Using
        End Function

    End Class

End Namespace
