using Microsoft.Extensions.Logging;
using UrbanCollection.ETL.Carga;
using UrbanCollection.ETL.Data;
using UrbanCollection.ETL.Extraccion;
using System.Diagnostics;

namespace UrbanCollection.ETL;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly AnalyticsContext _context;

    public Worker(ILogger<Worker> logger, IConfiguration config, AnalyticsContext context)
    {
        _logger = logger;
        _config = config;
        _context = context;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("=== INICIO DEL PROCESO ETL - URBAN COLLECTION ===");
        _logger.LogInformation("Fecha y hora: {time}", DateTimeOffset.Now);

        try
        {
            var connString = _config.GetConnectionString("ECommerceDB")!;
            var rutaProductos = _config["CsvPaths:Productos"]!;
            var rutaClientes = _config["CsvPaths:Clientes"]!;
            var rutaVentas = _config["CsvPaths:Ventas"]!;
            var apiUrl = _config["ApiSettings:ProductosUrl"] ?? "";

            var extractor = new ExtractorCSV();
            var validador = new ValidadorDatos();
            var cargador = new CargadorDatos(connString);
            var cargadorEF = new CargadorEF(_context);
            var staging = new StagingLoader(connString, 
                _logger as ILogger<StagingLoader> ?? 
                LoggerFactory.Create(b => b.AddConsole()).CreateLogger<StagingLoader>());
            var dbExtractor = new DatabaseExtractor(connString,
                LoggerFactory.Create(b => b.AddConsole()).CreateLogger<DatabaseExtractor>());
            var apiExtractor = new ApiExtractor(apiUrl,
                LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ApiExtractor>());

            // ===== EXTRACCIÓN CSV =====
            _logger.LogInformation("--- [CSV] Extrayendo Productos.csv ---");
            var productosRaw = extractor.LeerProductos(rutaProductos);
            _logger.LogInformation("Registros leidos: {count}", productosRaw.Count);
            var (productosValidos, erroresProductos) = validador.ValidarProductos(productosRaw);
            foreach (var e in erroresProductos)
                _logger.LogWarning("VALIDACION: {error}", e);
            await staging.CargarProductosStagingAsync(productosValidos, "CSV");

            _logger.LogInformation("--- [CSV] Extrayendo Clientes.csv ---");
            var clientesRaw = extractor.LeerClientes(rutaClientes);
            _logger.LogInformation("Registros leidos: {count}", clientesRaw.Count);
            var (clientesValidos, erroresClientes) = validador.ValidarClientes(clientesRaw);
            foreach (var e in erroresClientes)
                _logger.LogWarning("VALIDACION: {error}", e);
            await staging.CargarClientesStagingAsync(clientesValidos, "CSV");

            _logger.LogInformation("--- [CSV] Extrayendo Ventas.csv ---");
            var ventasRaw = extractor.LeerVentas(rutaVentas);
            _logger.LogInformation("Registros leidos: {count}", ventasRaw.Count);
            var (ventasValidas, erroresVentas) = validador.ValidarVentas(ventasRaw);
            foreach (var e in erroresVentas)
                _logger.LogWarning("VALIDACION: {error}", e);
            await staging.CargarVentasStagingAsync(ventasValidas, "CSV");

            // ===== EXTRACCIÓN BASE DE DATOS =====
            _logger.LogInformation("--- [BD] Extrayendo desde ECommerceDB transaccional ---");
            var ventasBD = await dbExtractor.ExtraerAsync();
            _logger.LogInformation("Registros extraidos desde BD: {count}", ventasBD.Count);
            await staging.CargarVentasStagingAsync(ventasBD, "BASE_DE_DATOS");

            // ===== EXTRACCIÓN API REST =====
            if (!string.IsNullOrEmpty(apiUrl))
            {
                _logger.LogInformation("--- [API] Extrayendo desde API REST ---");
                var productosApi = await apiExtractor.ExtraerAsync();
                _logger.LogInformation("Registros extraidos desde API: {count}", productosApi.Count);
                await staging.CargarProductosStagingAsync(productosApi, "API_REST");
            }
            else
            {
                _logger.LogWarning("--- [API] URL no configurada, extraccion omitida.");
            }

            // ===== CARGA FINAL =====
            _logger.LogInformation("--- Cargando datos desde staging a analytics ---");
            var resultadoProductos = cargador.CargarProductos(productosValidos);
            var resultadoClientes = cargador.CargarClientes(clientesValidos);
            var resultadoVentas = cargador.CargarVentas(ventasValidas);

            // ===== VERIFICACION EF =====
            _logger.LogInformation("--- Verificacion con Entity Framework ---");
            var efProductos = cargadorEF.ConsultarResumenProductos();
            var efClientes = cargadorEF.ConsultarResumenClientes();
            var efVentas = cargadorEF.ConsultarResumenVentas();
            _logger.LogInformation("EF - Productos en analytics: {count}", efProductos.Procesados);
            _logger.LogInformation("EF - Clientes en analytics: {count}", efClientes.Procesados);
            _logger.LogInformation("EF - Ventas en analytics: {count}", efVentas.Procesados);

            // ===== RESUMEN FINAL =====
            sw.Stop();
            _logger.LogInformation("=== RESUMEN FINAL DEL PROCESO ETL ===");
            _logger.LogInformation("Productos  -> Procesados: {p} | Insertados: {i} | Rechazados: {r}",
                resultadoProductos.Procesados, resultadoProductos.Insertados, resultadoProductos.Rechazados);
            _logger.LogInformation("Clientes   -> Procesados: {p} | Insertados: {i} | Rechazados: {r}",
                resultadoClientes.Procesados, resultadoClientes.Insertados, resultadoClientes.Rechazados);
            _logger.LogInformation("Ventas     -> Procesados: {p} | Insertados: {i} | Rechazados: {r}",
                resultadoVentas.Procesados, resultadoVentas.Insertados, resultadoVentas.Rechazados);
            _logger.LogInformation("Tiempo total de ejecucion: {ms} ms", sw.ElapsedMilliseconds);
            _logger.LogInformation("=== FIN DEL PROCESO ETL ===");
        }
        catch (Exception ex)
        {
            _logger.LogError("ERROR CRITICO: {message}", ex.Message);
        }

        await Task.CompletedTask;
    }
}