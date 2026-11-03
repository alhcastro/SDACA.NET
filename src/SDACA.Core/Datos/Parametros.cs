using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace SDACA.Core.Datos
{
    /// <summary>
    /// Parámetros por local en <c>gatparametros</c>. Reemplaza a
    /// <c>SD_CLS_CNN.Get_Parametro / Get_Parametro_Lng / Set_Parametro</c>.
    /// </summary>
    /// <remarks>
    /// Si el local no tiene el parámetro, se busca en el local 1000, que guarda los valores globales.
    /// La búsqueda no distingue mayúsculas (<c>ucase(parametro)</c>). Las consultas ahora van
    /// parametrizadas; en el original se concatenaba el texto.
    /// </remarks>
    public sealed class Parametros
    {
        public const long LocalGlobal = 1000;
        private readonly IFabricaConexiones _fabrica;

        public Parametros(IFabricaConexiones fabrica) => _fabrica = fabrica;

        public async Task<string> ObtenerAsync(long local, string parametro, CancellationToken ct = default)
        {
            const string sql = "SELECT valor FROM gatparametros WHERE local = @local AND UPPER(parametro) = UPPER(@parametro) LIMIT 1;";
            using var cn = await _fabrica.AbrirAsync(ct).ConfigureAwait(false);
            var valor = await cn.ExecuteScalarAsync<string?>(new CommandDefinition(sql, new { local, parametro = parametro.Trim() }, cancellationToken: ct)).ConfigureAwait(false);
            if (string.IsNullOrEmpty(valor) && local != LocalGlobal)
            {
                valor = await cn.ExecuteScalarAsync<string?>(new CommandDefinition(sql, new { local = LocalGlobal, parametro = parametro.Trim() }, cancellationToken: ct)).ConfigureAwait(false);
            }
            return valor ?? "";
        }

        /// <summary>Igual que Get_Parametro_Lng: Val() del texto, 0 si no es número.</summary>
        public async Task<long> ObtenerNumeroAsync(long local, string parametro, CancellationToken ct = default)
        {
            var texto = await ObtenerAsync(local, parametro, ct).ConfigureAwait(false);
            return Vb6.Val(texto);
        }

        public async Task<bool> GuardarAsync(long local, string parametro, string valor, long uidc, CancellationToken ct = default)
        {
            using var cn = await _fabrica.AbrirAsync(ct).ConfigureAwait(false);
            var p = new { local, parametro = parametro.Trim(), valor, uidc };
            var existe = await cn.ExecuteScalarAsync<long>(new CommandDefinition(
                "SELECT COUNT(valor) FROM gatparametros WHERE local = @local AND UPPER(parametro) = UPPER(@parametro);", p, cancellationToken: ct)).ConfigureAwait(false);
            var sql = existe == 0
                ? "INSERT INTO gatparametros (local, Parametro, Valor, uidc, fc) VALUES (@local, @parametro, @valor, @uidc, NOW());"
                : "UPDATE gatparametros SET valor = @valor WHERE local = @local AND UPPER(parametro) = UPPER(@parametro);";
            await cn.ExecuteAsync(new CommandDefinition(sql, p, cancellationToken: ct)).ConfigureAwait(false);
            return true;
        }
    }
}
