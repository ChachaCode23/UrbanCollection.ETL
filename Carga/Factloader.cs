using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using UrbanCollection.ETL.Data;

namespace UrbanCollection.ETL.Carga;

public class FactLoader
{
    private readonly string _connectionString;
    private readonly ILogger<FactLoader> _logger;

    public FactLoader(string connectionString, ILogger<FactLoader> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private async Task RegistrarErrorAsync(SqlConnection conn, string mensaje)
    {
        try
        {
            var cmd = new SqlCommand(
                "INSERT INTO analytics.ErrorLog (Mensaje, FechaError) VALUES (@Mensaje, GETDATE())", conn);
            cmd.Parameters.AddWithValue("@Mensaje", mensaje);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("No se pudo registrar en ErrorLog: {message}", ex.Message);
        }
    }

    public async Task LimpiarFactVentasAsync()
    {
        try
        {
            _logger.LogInformation("FactLoader: limpiando analytics.FactVentas...");
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqlCommand("TRUNCATE TABLE analytics.FactVentas", conn);
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("FactLoader: analytics.FactVentas limpiada correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR limpieza FactVentas: {message}", ex.Message);
        }
    }

    public async Task<int> CargarFactVentasAsync(List<VentaCSV> ventas)
    {
        int totalInsertados = 0;
        try
        {
            _logger.LogInformation("FactLoader: preparando DataTable para FactVentas ({count} registros)...", ventas.Count);

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var fuenteCmd = new SqlCommand("SELECT IdFuente FROM analytics.DimFuente WHERE TipoFuente = 'CSV'", conn);
            var idFuente = (int)(await fuenteCmd.ExecuteScalarAsync() ?? 1);

            var dt = new DataTable();
            dt.Columns.Add("IdTiempo", typeof(int));
            dt.Columns.Add("IdProducto", typeof(int));
            dt.Columns.Add("IdCliente", typeof(int));
            dt.Columns.Add("IdFuente", typeof(int));
            dt.Columns.Add("Cantidad", typeof(int));
            dt.Columns.Add("PrecioUnitario", typeof(decimal));
            dt.Columns.Add("Total", typeof(decimal));

            foreach (var v in ventas)
            {
                var prodCmd = new SqlCommand("SELECT IdProducto FROM analytics.DimProducto WHERE Nombre = @Nombre", conn);
                prodCmd.Parameters.AddWithValue("@Nombre", v.nombre_producto);
                var idProducto = await prodCmd.ExecuteScalarAsync();
                if (idProducto == null) continue;

                var clienteCmd = new SqlCommand("SELECT IdCliente FROM analytics.DimCliente WHERE Nombre = @Nombre", conn);
                clienteCmd.Parameters.AddWithValue("@Nombre", v.nombre_cliente);
                var idCliente = await clienteCmd.ExecuteScalarAsync();
                if (idCliente == null) continue;

                var fecha = v.fecha.ToString("yyyyMMdd");
                var tiempoCmd = new SqlCommand("SELECT IdTiempo FROM analytics.DimTiempo WHERE IdTiempo = @IdTiempo", conn);
                tiempoCmd.Parameters.AddWithValue("@IdTiempo", int.Parse(fecha));
                var idTiempo = await tiempoCmd.ExecuteScalarAsync();
                if (idTiempo == null) continue;

                dt.Rows.Add((int)idTiempo, (int)idProducto, (int)idCliente, idFuente,
                    v.cantidad, v.precio_unitario, v.total);
            }

            _logger.LogInformation("FactLoader: {count} filas preparadas en DataTable.", dt.Rows.Count);

            using var spCmd = new SqlCommand("analytics.sp_InsertarFactVentas", conn);
            spCmd.CommandType = CommandType.StoredProcedure;
            var param = spCmd.Parameters.AddWithValue("@datos", dt);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "analytics.FactVentasType";

            using var reader = await spCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                totalInsertados = reader.GetInt32(0);

            _logger.LogInformation("FactLoader: {count} registros insertados en FactVentas.", totalInsertados);
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR carga FactVentas: {message}", ex.Message);
            using var connErr = new SqlConnection(_connectionString);
            await connErr.OpenAsync();
            await RegistrarErrorAsync(connErr, $"CargarFactVentas: {ex.Message}");
        }
        return totalInsertados;
    }

    public async Task LimpiarFactEnviosAsync()
    {
        try
        {
            _logger.LogInformation("FactLoader: limpiando analytics.FactEnvios...");
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqlCommand("TRUNCATE TABLE analytics.FactEnvios", conn);
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("FactLoader: analytics.FactEnvios limpiada correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR limpieza FactEnvios: {message}", ex.Message);
        }
    }

    public async Task<int> CargarFactEnviosAsync()
    {
        int totalInsertados = 0;
        try
        {
            _logger.LogInformation("FactLoader: preparando DataTable para FactEnvios...");

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var dt = new DataTable();
            dt.Columns.Add("IdTiempo", typeof(int));
            dt.Columns.Add("IdCliente", typeof(int));
            dt.Columns.Add("IdFuente", typeof(int));
            dt.Columns.Add("Tracking", typeof(string));
            dt.Columns.Add("Estado", typeof(string));

            dt.Rows.Add(20260610, 5, 3, "TRK-001-URB", "ENTREGADO");
            dt.Rows.Add(20260615, 10, 3, "TRK-002-URB", "ENTREGADO");
            dt.Rows.Add(20260620, 18, 3, "TRK-003-URB", "EN_TRANSITO");
            dt.Rows.Add(20260622, 23, 3, "TRK-004-URB", "PENDIENTE");
            dt.Rows.Add(20260626, 1, 3, "TRK-005-URB", "ENTREGADO");

            _logger.LogInformation("FactLoader: {count} filas preparadas en DataTable.", dt.Rows.Count);

            using var spCmd = new SqlCommand("analytics.sp_InsertarFactEnvios", conn);
            spCmd.CommandType = CommandType.StoredProcedure;
            var param = spCmd.Parameters.AddWithValue("@datos", dt);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "analytics.FactEnviosType";

            using var reader = await spCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                totalInsertados = reader.GetInt32(0);

            _logger.LogInformation("FactLoader: {count} registros insertados en FactEnvios.", totalInsertados);
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR carga FactEnvios: {message}", ex.Message);
            using var connErr = new SqlConnection(_connectionString);
            await connErr.OpenAsync();
            await RegistrarErrorAsync(connErr, $"CargarFactEnvios: {ex.Message}");
        }
        return totalInsertados;
    }

    public async Task LimpiarFactPagosAsync()
    {
        try
        {
            _logger.LogInformation("FactLoader: limpiando analytics.FactPagos...");
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqlCommand("TRUNCATE TABLE analytics.FactPagos", conn);
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("FactLoader: analytics.FactPagos limpiada correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR limpieza FactPagos: {message}", ex.Message);
        }
    }

    public async Task<int> CargarFactPagosAsync()
    {
        int totalInsertados = 0;
        try
        {
            _logger.LogInformation("FactLoader: preparando DataTable para FactPagos...");

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var dt = new DataTable();
            dt.Columns.Add("IdTiempo", typeof(int));
            dt.Columns.Add("IdCliente", typeof(int));
            dt.Columns.Add("IdFuente", typeof(int));
            dt.Columns.Add("MetodoPago", typeof(string));
            dt.Columns.Add("Estado", typeof(string));
            dt.Columns.Add("Monto", typeof(decimal));

            dt.Rows.Add(20260610, 5, 3, "TARJETA", "APROBADO", 17600.00m);
            dt.Rows.Add(20260615, 10, 3, "TARJETA", "APROBADO", 7200.00m);
            dt.Rows.Add(20260620, 18, 3, "TRANSFERENCIA", "APROBADO", 13200.00m);
            dt.Rows.Add(20260622, 23, 3, "TARJETA", "APROBADO", 11000.00m);
            dt.Rows.Add(20260626, 1, 3, "TARJETA", "APROBADO", 9000.00m);

            _logger.LogInformation("FactLoader: {count} filas preparadas en DataTable.", dt.Rows.Count);

            using var spCmd = new SqlCommand("analytics.sp_InsertarFactPagos", conn);
            spCmd.CommandType = CommandType.StoredProcedure;
            var param = spCmd.Parameters.AddWithValue("@datos", dt);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "analytics.FactPagosType";

            using var reader = await spCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                totalInsertados = reader.GetInt32(0);

            _logger.LogInformation("FactLoader: {count} registros insertados en FactPagos.", totalInsertados);
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR carga FactPagos: {message}", ex.Message);
            using var connErr = new SqlConnection(_connectionString);
            await connErr.OpenAsync();
            await RegistrarErrorAsync(connErr, $"CargarFactPagos: {ex.Message}");
        }
        return totalInsertados;
    }

    public async Task<int> CargarFactInventarioAsync()
    {
        int totalInsertados = 0;
        try
        {
            _logger.LogInformation("FactLoader: preparando DataTable para FactInventario...");

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var dt = new DataTable();
            dt.Columns.Add("IdTiempo", typeof(int));
            dt.Columns.Add("IdProducto", typeof(int));
            dt.Columns.Add("IdFuente", typeof(int));
            dt.Columns.Add("Stock", typeof(int));
            dt.Columns.Add("StockMinimo", typeof(int));

            dt.Rows.Add(20260811, 1, 1, 50, 5);
            dt.Rows.Add(20260811, 2, 1, 100, 10);
            dt.Rows.Add(20260811, 3, 1, 100, 10);
            dt.Rows.Add(20260811, 4, 1, 100, 10);
            dt.Rows.Add(20260811, 5, 1, 100, 10);
            dt.Rows.Add(20260811, 6, 1, 200, 20);
            dt.Rows.Add(20260811, 7, 1, 80, 5);
            dt.Rows.Add(20260811, 8, 1, 60, 5);
            dt.Rows.Add(20260811, 9, 1, 300, 30);
            dt.Rows.Add(20260811, 10, 1, 75, 5);

            _logger.LogInformation("FactLoader: {count} filas preparadas en DataTable.", dt.Rows.Count);

            using var spCmd = new SqlCommand("analytics.sp_InsertarFactInventario", conn);
            spCmd.CommandType = CommandType.StoredProcedure;
            var param = spCmd.Parameters.AddWithValue("@datos", dt);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "analytics.FactInventarioType";

            using var reader = await spCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                totalInsertados = reader.GetInt32(0);

            _logger.LogInformation("FactLoader: {count} registros insertados en FactInventario.", totalInsertados);
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR carga FactInventario: {message}", ex.Message);
            using var connErr = new SqlConnection(_connectionString);
            await connErr.OpenAsync();
            await RegistrarErrorAsync(connErr, $"CargarFactInventario: {ex.Message}");
        }
        return totalInsertados;
    }
}