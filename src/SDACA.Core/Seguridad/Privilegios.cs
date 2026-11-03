using System.Threading;
using System.Threading.Tasks;
using Dapper;
using SDACA.Core.Datos;

namespace SDACA.Core.Seguridad
{
    /// <summary>Cómo validar privilegios mientras conviven VB6 y .NET.</summary>
    public enum ModoPrivilegios
    {
        /// <summary>
        /// Igual que VB6 (<c>Comun\SD_MOD_USU.bas → TienePrivilegio</c>): la consulta no filtra
        /// por el privilegio pedido, así que basta con que el usuario tenga uno cualquiera.
        /// </summary>
        CompatibleVb6,

        /// <summary>Solo da acceso si el usuario tiene el privilegio pedido.</summary>
        Estricto,
    }

    /// <summary>Valida privilegios contra <c>gatprivilegios</c> y <c>gatusuariospriv2</c>.</summary>
    /// <remarks>
    /// gatusuariospriv2 guarda hashes: <c>uid = MD5("local,uid")</c> y <c>PRI = MD5("local,uid,priv")</c>.
    /// Un privilegio con <c>IDs</c> vacío vale para todas las empresas. Si no, la cédula de la
    /// empresa (sin guiones) tiene que aparecer en <c>IDs</c>. El usuario 0 tiene todos los privilegios.
    /// </remarks>
    public sealed class Privilegios
    {
        private readonly IFabricaConexiones _fabrica;
        private readonly ModoPrivilegios _modo;

        public Privilegios(IFabricaConexiones fabrica, ModoPrivilegios modo = ModoPrivilegios.CompatibleVb6)
        {
            _fabrica = fabrica;
            _modo = modo;
        }

        public static string HashUsuario(long local, long uid) => Md5Vb6.Calcular(local + "," + uid);

        public static string HashPrivilegio(long local, long uid, long privilegio) => Md5Vb6.Calcular(local + "," + uid + "," + privilegio);

        public async Task<bool> TieneAsync(long local, long uid, long privilegio, string cedulaEmpresa, CancellationToken ct = default)
        {
            if (uid == 0) return true;

            var sql =
                "SELECT COUNT(P.PRIV) FROM gatprivilegios P " +
                "INNER JOIN gatusuariospriv2 U ON U.uid = @hashUid AND U.PRI = MD5(CONCAT(@prefijo, P.Priv)) " +
                "WHERE (P.IDs = '' OR INSTR(P.IDs, @cedula) > 0)" +
                (_modo == ModoPrivilegios.Estricto ? " AND P.Priv = @privilegio;" : ";");

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
