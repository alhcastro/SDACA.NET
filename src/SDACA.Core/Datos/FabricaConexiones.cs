using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace SDACA.Core.Datos
{
    /// <summary>Abre conexiones a MySQL. Reemplaza a SD_CLS_CNN.Conectar / DB_Conectar.</summary>
    public interface IFabricaConexiones
    {
        Task<MySqlConnection> AbrirAsync(CancellationToken ct = default);
    }

    public sealed class FabricaConexiones : IFabricaConexiones
    {
        private readonly string _cadena;

        public FabricaConexiones(string cadenaConexion) => _cadena = cadenaConexion;

        public async Task<MySqlConnection> AbrirAsync(CancellationToken ct = default)
        {
            var cn = new MySqlConnection(_cadena);
            await cn.OpenAsync(ct).ConfigureAwait(false);
            return cn;
        }
    }
}
