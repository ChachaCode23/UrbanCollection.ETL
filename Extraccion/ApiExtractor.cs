using Microsoft.Extensions.Logging;
using System.Text.Json;
using UrbanCollection.ETL.Data;

namespace UrbanCollection.ETL.Extraccion;

public class ApiExtractor : IExtractor<ProductoCSV>
{
    private readonly string _apiUrl;
    private readonly ILogger<ApiExtractor> _logger;
    private readonly HttpClient _httpClient;

    public ApiExtractor(string apiUrl, ILogger<ApiExtractor> logger)
    {
        _apiUrl = apiUrl;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<List<ProductoCSV>> ExtraerAsync()
    {
        var productos = new List<ProductoCSV>();

        try
        {
            _logger.LogInformation("ApiExtractor: consumiendo API en {url}...", _apiUrl);

            var response = await _httpClient.GetAsync(_apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ApiExtractor: la API respondio con status {code}", response.StatusCode);
                return productos;
            }

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resultado = JsonSerializer.Deserialize<List<ProductoCSV>>(json, options);

            if (resultado != null)
                productos.AddRange(resultado);

            _logger.LogInformation("ApiExtractor: {count} productos extraidos desde API.", productos.Count);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("ApiExtractor: API no disponible. {message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("ApiExtractor ERROR: {message}", ex.Message);
        }

        return productos;
    }
}