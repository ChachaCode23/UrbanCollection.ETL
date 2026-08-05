using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using UrbanCollection.ETL.Data;

namespace UrbanCollection.ETL.Extraccion;

public class DatabaseExtractor : IExtractor<VentaCSV>
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseExtractor> _logger;

    public DatabaseExtractor(string connectionString, ILogger<DatabaseExtractor> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<List<VentaCSV>> ExtraerAsync()
    {
        var ventas = new List<VentaCSV>();

        try
        {
            _logger.LogInformation("DatabaseExtractor: iniciando extraccion desde ECommerceDB...");

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                SELECT 
                    p.pedido_id       AS venta_id,
                    p.usuario_id      AS cliente_id,
                    u.nombre          AS nombre_cliente,
                    ip.producto_id    AS producto_id,
                    pr.nombre         AS nombre_producto,
                    ip.cantidad       AS cantidad,
                    ip.precio_unitario AS precio_unitario,
                    GETDATE()         AS fecha,
                    ip.cantidad * ip.precio_unitario AS total
                FROM core.Pedido p
                JOIN core.Usuario u      ON p.usuario_id   = u.usuario_id
                JOIN core.item_pedido ip ON p.pedido_id    = ip.pedido_id
                JOIN core.Producto pr    ON ip.producto_id = pr.producto_id";

            using var cmd = new SqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ventas.Add(new VentaCSV
                {
                    venta_id        = reader.GetInt64(0) > int.MaxValue ? int.MaxValue : (int)reader.GetInt64(0),
                    cliente_id      = reader.GetInt64(1) > int.MaxValue ? int.MaxValue : (int)reader.GetInt64(1),
                    nombre_cliente  = reader.GetString(2),
                    producto_id     = reader.GetInt64(3) > int.MaxValue ? int.MaxValue : (int)reader.GetInt64(3),
                    nombre_producto = reader.GetString(4),
                    cantidad        = reader.GetInt32(5),
                    precio_unitario = reader.GetDecimal(6),
                    fecha           = reader.GetDateTime(7),
                    total           = reader.GetDecimal(8)
                });
            }

            _logger.LogInformation("DatabaseExtractor: {count} registros extraidos.", ventas.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError("DatabaseExtractor ERROR: {message}", ex.Message);
        }

        return ventas;
    }
}