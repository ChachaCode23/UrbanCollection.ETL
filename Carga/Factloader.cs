using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
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

    // ============================================================
    // FACT VENTAS
    // ============================================================
    public async Task LimpiarFactVentasAsync()
    {
        try
        {
            _logger.LogInformation("FactLoader: limpiando tabla analytics.FactVentas...");
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
        int insertados = 0;
        try
        {
            _logger.LogInformation("FactLoader: iniciando carga de FactVentas...");
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var fuenteCmd = new SqlCommand(
                "SELECT IdFuente FROM analytics.DimFuente WHERE TipoFuente = 'CSV'", conn);
            var idFuente = (int)(await fuenteCmd.ExecuteScalarAsync() ?? 1);

            foreach (var v in ventas)
            {
                var prodCmd = new SqlCommand(
                    "SELECT IdProducto FROM analytics.DimProducto WHERE Nombre = @Nombre", conn);
                prodCmd.Parameters.AddWithValue("@Nombre", v.nombre_producto);
                var idProducto = await prodCmd.ExecuteScalarAsync();
                if (idProducto == null)
                {
                    _logger.LogWarning("FactLoader: producto '{p}' no encontrado en DimProducto.", v.nombre_producto);
                    continue;
                }

                var clienteCmd = new SqlCommand(
                    "SELECT IdCliente FROM analytics.DimCliente WHERE Nombre = @Nombre", conn);
                clienteCmd.Parameters.AddWithValue("@Nombre", v.nombre_cliente);
                var idCliente = await clienteCmd.ExecuteScalarAsync();
                if (idCliente == null)
                {
                    _logger.LogWarning("FactLoader: cliente '{c}' no encontrado en DimCliente.", v.nombre_cliente);
                    continue;
                }

                var fecha = v.fecha.ToString("yyyyMMdd");
                var tiempoCmd = new SqlCommand(
                    "SELECT IdTiempo FROM analytics.DimTiempo WHERE IdTiempo = @IdTiempo", conn);
                tiempoCmd.Parameters.AddWithValue("@IdTiempo", int.Parse(fecha));
                var idTiempo = await tiempoCmd.ExecuteScalarAsync();
                if (idTiempo == null)
                {
                    _logger.LogWarning("FactLoader: fecha '{f}' no encontrada en DimTiempo.", fecha);
                    continue;
                }

                var cmd = new SqlCommand(@"
                    INSERT INTO analytics.FactVentas 
                    (IdTiempo, IdProducto, IdCliente, IdFuente, Cantidad, PrecioUnitario, Total)
                    VALUES (@IdTiempo, @IdProducto, @IdCliente, @IdFuente, @Cantidad, @Precio, @Total)", conn);
                cmd.Parameters.AddWithValue("@IdTiempo", (int)idTiempo);
                cmd.Parameters.AddWithValue("@IdProducto", (int)idProducto);
                cmd.Parameters.AddWithValue("@IdCliente", (int)idCliente);
                cmd.Parameters.AddWithValue("@IdFuente", idFuente);
                cmd.Parameters.AddWithValue("@Cantidad", v.cantidad);
                cmd.Parameters.AddWithValue("@Precio", v.precio_unitario);
                cmd.Parameters.AddWithValue("@Total", v.total);
                await cmd.ExecuteNonQueryAsync();
                insertados++;
            }

            _logger.LogInformation("FactLoader: {count} registros insertados en FactVentas.", insertados);
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR carga FactVentas: {message}", ex.Message);
        }
        return insertados;
    }

    // ============================================================
    // FACT ENVIOS
    // ============================================================
    public async Task LimpiarFactEnviosAsync()
    {
        try
        {
            _logger.LogInformation("FactLoader: limpiando tabla analytics.FactEnvios...");
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
        int insertados = 0;
        try
        {
            _logger.LogInformation("FactLoader: cargando FactEnvios desde ECommerceDB...");
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var idFuente = 3;

            var query = @"
                SELECT e.id, p.usuario_id, e.tracking, e.estado, e.created_at
                FROM core.envios e
                JOIN core.Pedido p ON e.pedido_id = p.pedido_id";

            using var cmd = new SqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var rows = new List<(long usuarioId, string tracking, string estado, DateTime fecha)>();
            while (await reader.ReadAsync())
                rows.Add((reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetDateTime(4)));
            reader.Close();

            foreach (var row in rows)
            {
                var fecha = row.fecha.ToString("yyyyMMdd");
                var tiempoCmd = new SqlCommand("SELECT IdTiempo FROM analytics.DimTiempo WHERE IdTiempo = @Id", conn);
                tiempoCmd.Parameters.AddWithValue("@Id", int.Parse(fecha));
                var idTiempo = await tiempoCmd.ExecuteScalarAsync();
                if (idTiempo == null) continue;

                var clienteCmd = new SqlCommand("SELECT IdCliente FROM analytics.DimCliente WHERE IdCliente = @Id", conn);
                clienteCmd.Parameters.AddWithValue("@Id", (int)row.usuarioId);
                var idCliente = await clienteCmd.ExecuteScalarAsync();
                if (idCliente == null) continue;

                var ins = new SqlCommand(@"
                    INSERT INTO analytics.FactEnvios (IdTiempo, IdCliente, IdFuente, Tracking, Estado)
                    VALUES (@IdTiempo, @IdCliente, @IdFuente, @Tracking, @Estado)", conn);
                ins.Parameters.AddWithValue("@IdTiempo", (int)idTiempo);
                ins.Parameters.AddWithValue("@IdCliente", (int)idCliente);
                ins.Parameters.AddWithValue("@IdFuente", idFuente);
                ins.Parameters.AddWithValue("@Tracking", row.tracking);
                ins.Parameters.AddWithValue("@Estado", row.estado);
                await ins.ExecuteNonQueryAsync();
                insertados++;
            }

            _logger.LogInformation("FactLoader: {count} registros insertados en FactEnvios.", insertados);
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR carga FactEnvios: {message}", ex.Message);
        }
        return insertados;
    }

    // ============================================================
    // FACT PAGOS
    // ============================================================
    public async Task LimpiarFactPagosAsync()
    {
        try
        {
            _logger.LogInformation("FactLoader: limpiando tabla analytics.FactPagos...");
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
        int insertados = 0;
        try
        {
            _logger.LogInformation("FactLoader: cargando FactPagos desde ECommerceDB...");
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var idFuente = 3;

            var query = @"
                SELECT t.metodo, t.estado, t.monto, p.usuario_id, t.creado_en
                FROM core.transaccion_pago t
                JOIN core.Pedido p ON t.pedido_id = p.pedido_id";

            using var cmd = new SqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var rows = new List<(string metodo, string estado, decimal monto, long usuarioId, DateTime fecha)>();
            while (await reader.ReadAsync())
                rows.Add((reader.GetString(0), reader.GetString(1), reader.GetDecimal(2), reader.GetInt64(3), reader.GetDateTime(4)));
            reader.Close();

            foreach (var row in rows)
            {
                var fecha = row.fecha.ToString("yyyyMMdd");
                var tiempoCmd = new SqlCommand("SELECT IdTiempo FROM analytics.DimTiempo WHERE IdTiempo = @Id", conn);
                tiempoCmd.Parameters.AddWithValue("@Id", int.Parse(fecha));
                var idTiempo = await tiempoCmd.ExecuteScalarAsync();
                if (idTiempo == null) continue;

                var clienteCmd = new SqlCommand("SELECT IdCliente FROM analytics.DimCliente WHERE IdCliente = @Id", conn);
                clienteCmd.Parameters.AddWithValue("@Id", (int)row.usuarioId);
                var idCliente = await clienteCmd.ExecuteScalarAsync();
                if (idCliente == null) continue;

                var ins = new SqlCommand(@"
                    INSERT INTO analytics.FactPagos (IdTiempo, IdCliente, IdFuente, MetodoPago, Estado, Monto)
                    VALUES (@IdTiempo, @IdCliente, @IdFuente, @MetodoPago, @Estado, @Monto)", conn);
                ins.Parameters.AddWithValue("@IdTiempo", (int)idTiempo);
                ins.Parameters.AddWithValue("@IdCliente", (int)idCliente);
                ins.Parameters.AddWithValue("@IdFuente", idFuente);
                ins.Parameters.AddWithValue("@MetodoPago", row.metodo);
                ins.Parameters.AddWithValue("@Estado", row.estado);
                ins.Parameters.AddWithValue("@Monto", row.monto);
                await ins.ExecuteNonQueryAsync();
                insertados++;
            }

            _logger.LogInformation("FactLoader: {count} registros insertados en FactPagos.", insertados);
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR carga FactPagos: {message}", ex.Message);
        }
        return insertados;
    }

    // ============================================================
    // FACT INVENTARIO
    // ============================================================
    public async Task LimpiarFactInventarioAsync()
    {
        try
        {
            _logger.LogInformation("FactLoader: limpiando tabla analytics.FactInventario...");
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqlCommand("TRUNCATE TABLE analytics.FactInventario", conn);
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("FactLoader: analytics.FactInventario limpiada correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR limpieza FactInventario: {message}", ex.Message);
        }
    }

    public async Task<int> CargarFactInventarioAsync()
    {
        int insertados = 0;
        try
        {
            _logger.LogInformation("FactLoader: cargando FactInventario desde ECommerceDB...");
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var idFuente = 3;
            var idTiempo = 20260811;

            var query = "SELECT producto_id, stock FROM core.Producto WHERE activo = 1";
            using var cmd = new SqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var rows = new List<(int productoId, int stock)>();
            while (await reader.ReadAsync())
                rows.Add(((int)reader.GetInt64(0), reader.GetInt32(1)));
            reader.Close();

            foreach (var row in rows)
            {
                var prodCmd = new SqlCommand("SELECT IdProducto FROM analytics.DimProducto WHERE IdProducto = @Id", conn);
                prodCmd.Parameters.AddWithValue("@Id", row.productoId);
                var idProducto = await prodCmd.ExecuteScalarAsync();
                if (idProducto == null) continue;

                var ins = new SqlCommand(@"
                    INSERT INTO analytics.FactInventario (IdTiempo, IdProducto, IdFuente, Stock, StockMinimo)
                    VALUES (@IdTiempo, @IdProducto, @IdFuente, @Stock, @StockMinimo)", conn);
                ins.Parameters.AddWithValue("@IdTiempo", idTiempo);
                ins.Parameters.AddWithValue("@IdProducto", (int)idProducto);
                ins.Parameters.AddWithValue("@IdFuente", idFuente);
                ins.Parameters.AddWithValue("@Stock", row.stock);
                ins.Parameters.AddWithValue("@StockMinimo", 5);
                await ins.ExecuteNonQueryAsync();
                insertados++;
            }

            _logger.LogInformation("FactLoader: {count} registros insertados en FactInventario.", insertados);
        }
        catch (Exception ex)
        {
            _logger.LogError("FactLoader ERROR carga FactInventario: {message}", ex.Message);
        }
        return insertados;
    }
}