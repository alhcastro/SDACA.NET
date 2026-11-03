using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace SDACA.Core.Datos
{
    /// <summary>Tipo de registro de bitácora (SDACA_BITACORA_TIPO_CNN).</summary>
    public enum TipoBitacora
    {
        Suceso = 0,
        Error = 1,
    }

    /// <summary>Bitácora en <c>gatbitacora</c>. Reemplaza a <c>SD_CLS_CNN.BITACORA_AGREGAR</c>.</summary>
    public sealed class Bitacora
    {
        private readonly IFabricaConexiones _fabrica;

        public Bitacora(IFabricaConexiones fabrica) => _fabrica = fabrica;

        /// <summary>Agrega un registro y devuelve su id (LAST_INSERT_ID).</summary>
        public async Task<long> AgregarAsync(long local, long modulo, long uid, TipoBitacora tipo, string estacionId, string detalle, string detalleSql, CancellationToken ct = default)
        {
            const string sql =
                "INSERT INTO gatbitacora (tid, local, modulo, estacion, tipo, det, detsql, uid, fe, fc) " +
                "VALUES (NULL, @local, @modulo, @estacionId, @tipo, @detalle, @detalleSql, @uid, NOW(), NOW()); " +
                "SELECT LAST_INSERT_ID();";
            using var cn = await _fabrica.AbrirAsync(ct).ConfigureAwait(false);
            return await cn.ExecuteScalarAsync<long>(new CommandDefinition(sql,
                new { local, modulo, estacionId, tipo = (int)tipo, detalle, detalleSql, uid }, cancellationToken: ct)).ConfigureAwait(false);
        }
    }
}
