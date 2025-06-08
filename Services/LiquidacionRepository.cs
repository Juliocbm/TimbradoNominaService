using System.Data;
using System.Data.SqlClient;
using Nomina.WorkerTimbrado.Models;

namespace Nomina.WorkerTimbrado.Services
{
    public class LiquidacionRepository
    {
        private readonly string _connectionString;

        public LiquidacionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection CreateConnection()
            => new SqlConnection(_connectionString);

        public async Task<List<Liquidacion>> GetPendientesAsync(int batchSize, CancellationToken ct)
        {
            var list = new List<Liquidacion>();
            const string query = @"SELECT TOP(@BatchSize) IdLiquidacion, IdCompania, Intentos, UltimoIntento
                                    FROM cfdi.liquidacionOperador
                                    WHERE Estatus = 0
                                      AND (FechaProximoIntento IS NULL OR FechaProximoIntento <= SYSUTCDATETIME())
                                    ORDER BY FechaRegistro";
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@BatchSize", batchSize);
            await conn.OpenAsync(ct);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new Liquidacion
                {
                    IdLiquidacion = reader.GetInt32(0),
                    IdCompania = reader.GetInt32(1),
                    Intentos = reader.GetInt32(2),
                    UltimoIntento = reader.GetInt16(3)
                });
            }
            return list;
        }

        public async Task<bool> MarcarEnProcesoAsync(Liquidacion liq, CancellationToken ct)
        {
            const string update = @"UPDATE cfdi.liquidacionOperador
                                     SET Estatus = 1,
                                         Intentos = Intentos + 1,
                                         UltimoIntento = Intentos + 1
                                   WHERE IdLiquidacion = @IdLiquidacion
                                     AND IdCompania = @IdCompania
                                     AND Estatus = 0";
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(update, conn);
            cmd.Parameters.AddWithValue("@IdLiquidacion", liq.IdLiquidacion);
            cmd.Parameters.AddWithValue("@IdCompania", liq.IdCompania);
            await conn.OpenAsync(ct);
            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }

        public async Task SetRequiereRevisionAsync(Liquidacion liq, CancellationToken ct)
        {
            const string update = @"UPDATE cfdi.liquidacionOperador
                                     SET Estatus = 6
                                   WHERE IdLiquidacion = @IdLiquidacion
                                     AND IdCompania = @IdCompania";
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(update, conn);
            cmd.Parameters.AddWithValue("@IdLiquidacion", liq.IdLiquidacion);
            cmd.Parameters.AddWithValue("@IdCompania", liq.IdCompania);
            await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task SetErrorTransitorioAsync(Liquidacion liq, int backoffMinutes, CancellationToken ct)
        {
            const string update = @"UPDATE cfdi.liquidacionOperador
                                     SET Estatus = 4,
                                         FechaProximoIntento = DATEADD(MINUTE, @Backoff, SYSUTCDATETIME())
                                   WHERE IdLiquidacion = @IdLiquidacion
                                     AND IdCompania = @IdCompania";
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(update, conn);
            cmd.Parameters.AddWithValue("@IdLiquidacion", liq.IdLiquidacion);
            cmd.Parameters.AddWithValue("@IdCompania", liq.IdCompania);
            cmd.Parameters.AddWithValue("@Backoff", backoffMinutes);
            await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task SetErrorAsync(Liquidacion liq, int status, string message, CancellationToken ct)
        {
            const string update = @"UPDATE cfdi.liquidacionOperador
                                     SET Estatus = @Status, MensajeCorto = @Mensaje
                                   WHERE IdLiquidacion = @IdLiquidacion
                                     AND IdCompania = @IdCompania";
            using var conn = CreateConnection();
            using var cmd = new SqlCommand(update, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Mensaje", message);
            cmd.Parameters.AddWithValue("@IdLiquidacion", liq.IdLiquidacion);
            cmd.Parameters.AddWithValue("@IdCompania", liq.IdCompania);
            await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
