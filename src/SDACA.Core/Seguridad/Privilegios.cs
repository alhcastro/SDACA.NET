using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using SDACA.Core.Datos;

namespace SDACA.Core.Seguridad
{
    /// <summary>
    /// Privilegios de usuario. Reproduce lo que hace VB6 al iniciar sesión y al abrir cada opción.
    /// </summary>
    /// <remarks>
    /// <para>Un privilegio es un número: <c>Priv DIV 1000</c> es el módulo y <c>Priv MOD 1000 = 0</c>
    /// es el permiso para entrar al módulo. Ejemplo: 6000 = entrar a CXC, 6003 = una opción de CXC.</para>
    /// <para>gatusuariospriv2 guarda hashes: <c>UID = MD5("local,uid")</c> y <c>PRI = MD5("local,uid,priv")</c>.</para>
    /// <para>Fuentes VB6: <c>SDACAERP\Module01.bas → ObtenerUsuario</c> (carga) y
    /// <c>Comun\SD_MOD_UTILES02.bas → SD_ValidarPriv</c> (validación).</para>
    /// </remarks>
    public sealed class Privilegios
    {
        private readonly IFabricaConexiones _fabrica;

        public Privilegios(IFabricaConexiones fabrica) => _fabrica = fabrica;

        public static string HashUsuario(long local, long uid) => Md5Vb6.Calcular(local + "," + uid);

        public static string HashPrivilegio(long local, long uid, long privilegio) => Md5Vb6.Calcular(local + "," + uid + "," + privilegio);

        /// <summary>Administrador y root reciben todos los privilegios de los módulos del local.</summary>
        public static bool EsAdministrador(string login) =>
            string.Equals(login, "administrador", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(login, "root", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Carga los privilegios del usuario, como <c>ObtenerUsuario</c> llena <c>Usuario.Privilegios</c>.
        /// </summary>
        /// <param name="login">Usuario con el que se inició sesión.</param>
        /// <param name="local">Local donde inicia sesión (pLocal).</param>
        /// <param name="localUsuario">Local al que pertenece el usuario (pLocalUsr).</param>
        /// <param name="uid">Número de usuario.</param>
        /// <param name="cedulaRegistro">Registro.Cedula tal como viene en la licencia, con guiones.</param>
        /// <param name="usuariosGlobales">Parámetro server-usr-globales. En VB6 está desactivado.</param>
        /// <param name="ct">Cancelación.</param>
        public async Task<ISet<string>> CargarAsync(string login, long local, long localUsuario, long uid, string cedulaRegistro, bool usuariosGlobales = false, CancellationToken ct = default)
        {
            const string candidatos =
                "FROM gatmoduloslocal A " +
                "INNER JOIN gatprivilegios B ON (B.Priv - (B.Priv % 1000)) / 1000 = A.Modulo AND (B.IDs = '' OR INSTR(B.IDs, @cedula) > 0) ";

            string sql;
            if (EsAdministrador(login))
            {
                sql = "SELECT B.Priv " + candidatos + "WHERE A.Local = @local ORDER BY B.Priv;";
            }
            else
            {
                // VB6 revisa cada candidato con un SELECT COUNT(UID) aparte y lo acepta solo si el
                // resultado es exactamente 1. Aquí se hace lo mismo en una sola consulta.
                sql = "SELECT B.Priv " + candidatos +
                      (usuariosGlobales ? "WHERE " : "WHERE A.Local = @localUsuario AND ") +
                      "(SELECT COUNT(P.UID) FROM gatusuariospriv2 P WHERE P.UID = @hashUid AND P.PRI = MD5(CONCAT(@prefijo, B.Priv))) = 1 " +
                      (usuariosGlobales ? "GROUP BY B.Priv " : "") +
                      "ORDER BY B.Priv;";
            }

            using var cn = await _fabrica.AbrirAsync(ct).ConfigureAwait(false);
            var filas = await cn.QueryAsync<long>(new CommandDefinition(sql, new
            {
                cedula = cedulaRegistro ?? "",
                local,
                localUsuario,
                hashUid = HashUsuario(localUsuario, uid),
                prefijo = localUsuario + "," + uid + ",",
            }, cancellationToken: ct)).ConfigureAwait(false);

            return new HashSet<string>(filas.Select(p => p.ToString(System.Globalization.CultureInfo.InvariantCulture)), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Igual que <c>SD_ValidarPriv</c>. <paramref name="requeridos"/> es una lista como "6000;6003".
        /// Con <paramref name="cualquiera"/> = true basta uno (OR, el valor predeterminado en VB6).
        /// Con false hacen falta todos (AND). Una lista vacía devuelve false.
        /// </summary>
        public static bool Validar(ISet<string> privilegios, string? requeridos, string separador = ";", bool cualquiera = true)
        {
            var resultado = false;
            var resto = requeridos ?? "";
            while (resto.Length > 0)
            {
                // Str_GetLateral: lo que hay antes del separador, o "" si no hay separador.
                var pos = resto.IndexOf(separador, StringComparison.Ordinal);
                var actual = pos >= 0 ? resto.Substring(0, pos) : "";
                if (actual.Length == 0) actual = resto;

                if (privilegios.Contains(actual))
                {
                    resultado = true;
                    if (cualquiera) return true;
                }
                else
                {
                    resultado = false;
                    if (!cualquiera) return false;
                }

                // Mid(pPriv, Len(iPriv) + 2)
                var siguiente = actual.Length + separador.Length;
                resto = siguiente < resto.Length ? resto.Substring(siguiente) : "";
            }
            return resultado;
        }

        /// <summary>
        /// Consulta directa a la base de datos de un solo privilegio. Es la versión corregida de
        /// <c>Comun\SD_MOD_USU.bas → TienePrivilegio</c>, que no filtraba por el privilegio pedido.
        /// El usuario 0 tiene todos los privilegios.
        /// </summary>
        public async Task<bool> TieneAsync(long local, long uid, long privilegio, string cedulaEmpresa, CancellationToken ct = default)
        {
            if (uid == 0) return true;

            const string sql =
                "SELECT COUNT(P.PRIV) FROM gatprivilegios P " +
                "INNER JOIN gatusuariospriv2 U ON U.uid = @hashUid AND U.PRI = MD5(CONCAT(@prefijo, P.Priv)) " +
                "WHERE (P.IDs = '' OR INSTR(P.IDs, @cedula) > 0) AND P.Priv = @privilegio;";

            using var cn = await _fabrica.AbrirAsync(ct).ConfigureAwait(false);
            var total = await cn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
            {
                hashUid = HashUsuario(local, uid),
                prefijo = local + "," + uid + ",",
                cedula = (cedulaEmpresa ?? "").Replace("-", ""),
                privilegio,
            }, cancellationToken: ct)).ConfigureAwait(false);
            return total > 0;
        }
    }
}
