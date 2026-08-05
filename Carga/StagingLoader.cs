using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using UrbanCollection.ETL.Data;

namespace UrbanCollection.ETL.Carga;

public class StagingLoader
{
    private readonly string _connectionString;
    private readonly ILogger<StagingLoader> _logger;

    public StagingLoader(string connectionString, ILogger<StagingLoader> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task CargarProductosStagingAsync(List<ProductoCSV> productos, string fuente = "CSV")
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var p in productos)
            {
                var cmd = new SqlCommand(@"
                    INSERT INTO analytics.stg_Productos 
                    (producto_id, nombre, descripcion, precio_detalle, precio_mayoreo, stock, sku, activo, Fuente)
                    VALUES (@Id, @Nombre, @Descripcion, @PrecioDetalle, @PrecioMayoreo, @Stock, @Sku, @Activo, @Fuente)", conn);

                cmd.Parameters.AddWithValue("@Id", p.producto_id);
                cmd.Parameters.AddWithValue("@Nombre", p.nombre);
                cmd.Parameters.AddWithValue("@Descripcion", p.descripcion ?? "");
                cmd.Parameters.AddWithValue("@PrecioDetalle", p.precio_detalle);
                cmd.Parameters.AddWithValue("@PrecioMayoreo", p.precio_mayoreo);
                cmd.Parameters.AddWithValue("@Stock", p.stock);
                cmd.Parameters.AddWithValue("@Sku", p.sku);
                cmd.Parameters.AddWithValue("@Activo", p.activo);
                cmd.Parameters.AddWithValue("@Fuente", fuente);

                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("StagingLoader: {count} productos cargados en staging.", productos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError("StagingLoader ERROR productos: {message}", ex.Message);
        }
    }

    public async Task CargarClientesStagingAsync(List<ClienteCSV> clientes, string fuente = "CSV")
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var c in clientes)
            {
                var cmd = new SqlCommand(@"
                    INSERT INTO analytics.stg_Clientes 
                    (cliente_id, nombre, email, telefono, rol, Fuente)
                    VALUES (@Id, @Nombre, @Email, @Telefono, @Rol, @Fuente)", conn);

                cmd.Parameters.AddWithValue("@Id", c.cliente_id);
                cmd.Parameters.AddWithValue("@Nombre", c.nombre);
                cmd.Parameters.AddWithValue("@Email", c.email ?? "");
                cmd.Parameters.AddWithValue("@Telefono", c.telefono ?? "");
                cmd.Parameters.AddWithValue("@Rol", c.rol ?? "CLIENTE");
                cmd.Parameters.AddWithValue("@Fuente", fuente);

                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("StagingLoader: {count} clientes cargados en staging.", clientes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError("StagingLoader ERROR clientes: {message}", ex.Message);
        }
    }

    public async Task CargarVentasStagingAsync(List<VentaCSV> ventas, string fuente = "CSV")
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var v in ventas)
            {
                var cmd = new SqlCommand(@"
                    INSERT INTO analytics.stg_Ventas 
                    (venta_id, cliente_id, nombre_cliente, producto_id, nombre_producto, cantidad, precio_unitario, fecha, total, Fuente)
                    VALUES (@VentaId, @ClienteId, @NombreCliente, @ProductoId, @NombreProducto, @Cantidad, @Precio, @Fecha, @Total, @Fuente)", conn);

                cmd.Parameters.AddWithValue("@VentaId", v.venta_id);
                cmd.Parameters.AddWithValue("@ClienteId", v.cliente_id);
                cmd.Parameters.AddWithValue("@NombreCliente", v.nombre_cliente);
                cmd.Parameters.AddWithValue("@ProductoId", v.producto_id);
                cmd.Parameters.AddWithValue("@NombreProducto", v.nombre_producto);
                cmd.Parameters.AddWithValue("@Cantidad", v.cantidad);
                cmd.Parameters.AddWithValue("@Precio", v.precio_unitario);
                cmd.Parameters.AddWithValue("@Fecha", v.fecha);
                cmd.Parameters.AddWithValue("@Total", v.total);
                cmd.Parameters.AddWithValue("@Fuente", fuente);

                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("StagingLoader: {count} ventas cargadas en staging.", ventas.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError("StagingLoader ERROR ventas: {message}", ex.Message);
        }
    }
}