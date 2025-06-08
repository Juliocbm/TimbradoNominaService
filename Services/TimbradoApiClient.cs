using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Nomina.WorkerTimbrado.Models;

namespace Nomina.WorkerTimbrado.Services
{
    public class TimbradoApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TimbradoApiClient> _logger;

        public TimbradoApiClient(HttpClient httpClient, ILogger<TimbradoApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> TimbrarAsync(Liquidacion liq, CancellationToken ct)
        {
            var url = $"api/timbrar/{liq.IdLiquidacion}/{liq.IdCompania}";
            try
            {
                _logger.LogInformation("Invocando {Url}", url);
                return await _httpClient.PostAsync(url, null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error HTTP invocando {Url}", url);
                throw;
            }
        }
    }
}
