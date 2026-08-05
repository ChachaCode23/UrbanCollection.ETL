using Microsoft.Data.SqlClient;
using UrbanCollection.ETL.Data;

namespace UrbanCollection.ETL.Carga;

public class CargadorDatos
{
    private readonly string _connectionString;

    public CargadorDatos(string connectionString)
    {
        _connectionString = connectionString;
    }

    public ResultadoCarga CargarProductos(List<ProductoCSV> productos)
    {
        var resultado = new ResultadoCarga { Entidad = "Productos" };

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        foreach (var p in productos)
        {
            resultado.Procesados++;
            try
            {
                // Verificar duplicado
                var checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM analytics.Productos WHERE Nombre = @Nombre", conn);
                checkCmd.Parameters.AddWithValue("@Nombre", p.nombre);
                var existe = (int)checkCmd.ExecuteScalar() > 0;

                if (existe)
                {
                    resultado.Rechazados++;
                    resultado.Errores.Add($"Producto '{p.nombre}' ya existe en analytics.");
                    continue;
                }

                var cmd = new SqlCommand(@"
                    INSERT INTO analytics.Productos (Nombre, Categoria, Precio)
                    VALUES (@Nombre, @Categoria, @Precio)", conn);

                cmd.Parameters.AddWithValue("@Nombre", p.nombre);
                cmd.Parameters.AddWithValue("@Categoria", "General");
                cmd.Parameters.AddWithValue("@Precio", p.precio_detalle);

                cmd.ExecuteNonQuery();
                resultado.Insertados++;
            }
            catch (Exception ex)
            {
                resultado.Rechazados++;
                resultado.Errores.Add($"Error en producto '{p.nombre}': {ex.Message}");
            }
        }

        return resultado;
    }

    public ResultadoCarga CargarClientes(List<ClienteCSV> clientes)
    {
        var resultado = new ResultadoCarga { Entidad = "Clientes" };

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        foreach (var c in clientes)
        {
            resultado.Procesados++;
            try
            {
                var checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM analytics.Clientes WHERE Nombre = @Nombre", conn);
                checkCmd.Parameters.AddWithValue("@Nombre", c.nombre);
                var existe = (int)checkCmd.ExecuteScalar() > 0;

                if (existe)
                {
                    resultado.Rechazados++;
                    resultado.Errores.Add($"Cliente '{c.nombre}' ya existe en analytics.");
                    continue;
                }

                var cmd = new SqlCommand(@"
                    INSERT INTO analytics.Clientes (Nombre, Email, Region)
                    VALUES (@Nombre, @Email, @Region)", conn);

                cmd.Parameters.AddWithValue("@Nombre", c.nombre);
                cmd.Parameters.AddWithValue("@Email", c.email ?? "");
                cmd.Parameters.AddWithValue("@Region", c.rol);

                cmd.ExecuteNonQuery();
                resultado.Insertados++;
            }
            catch (Exception ex)
            {
                resultado.Rechazados++;
                resultado.Errores.Add($"Error en cliente '{c.nombre}': {ex.Message}");
            }
        }

        return resultado;
    }

    public ResultadoCarga CargarVentas(List<VentaCSV> ventas)
    {
        var resultado = new ResultadoCarga { Entidad = "Ventas" };

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        // Registrar fuente de datos
        var fuenteCmd = new SqlCommand(@"
            INSERT INTO analytics.FuenteDatos (TipoFuente)
            OUTPUT INSERTED.IdFuente
            VALUES ('CSV')", conn);
        var idFuente = (int)fuenteCmd.ExecuteScalar();

        foreach (var v in ventas)
        {
            resultado.Procesados++;
            try
            {
                // Obtener IdCliente desde analytics
                var clienteCmd = new SqlCommand(
                    "SELECT IdCliente FROM analytics.Clientes WHERE Nombre = @Nombre", conn);
                clienteCmd.Parameters.AddWithValue("@Nombre", v.nombre_cliente);
                var idCliente = clienteCmd.ExecuteScalar();

                if (idCliente == null)
                {
                    resultado.Rechazados++;
                    resultado.Errores.Add($"Venta id={v.venta_id}: cliente '{v.nombre_cliente}' no encontrado.");
                    continue;
                }

                // Obtener IdProducto desde analytics
                var productoCmd = new SqlCommand(
                    "SELECT IdProducto FROM analytics.Productos WHERE Nombre = @Nombre", conn);
                productoCmd.Parameters.AddWithValue("@Nombre", v.nombre_producto);
                var idProducto = productoCmd.ExecuteScalar();

                if (idProducto == null)
                {
                    resultado.Rechazados++;
                    resultado.Errores.Add($"Venta id={v.venta_id}: producto '{v.nombre_producto}' no encontrado.");
                    continue;
                }

                // Verificar duplicado
                var checkCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM analytics.Ventas 
                    WHERE IdCliente = @IdCliente AND IdProducto = @IdProducto 
                    AND Fecha = @Fecha AND Cantidad = @Cantidad", conn);
                checkCmd.Parameters.AddWithValue("@IdCliente", (int)idCliente);
                checkCmd.Parameters.AddWithValue("@IdProducto", (int)idProducto);
                checkCmd.Parameters.AddWithValue("@Fecha", v.fecha);
                checkCmd.Parameters.AddWithValue("@Cantidad", v.cantidad);
                var existe = (int)checkCmd.ExecuteScalar() > 0;

                if (existe)
                {
                    resultado.Rechazados++;
                    resultado.Errores.Add($"Venta id={v.venta_id}: duplicada.");
                    continue;
                }

                var cmd = new SqlCommand(@"
                    INSERT INTO analytics.Ventas (IdCliente, IdProducto, Cantidad, Precio, Fecha, Total, IdFuente)
                    VALUES (@IdCliente, @IdProducto, @Cantidad, @Precio, @Fecha, @Total, @IdFuente)", conn);

                cmd.Parameters.AddWithValue("@IdCliente", (int)idCliente);
                cmd.Parameters.AddWithValue("@IdProducto", (int)idProducto);
                cmd.Parameters.AddWithValue("@Cantidad", v.cantidad);
                cmd.Parameters.AddWithValue("@Precio", v.precio_unitario);
                cmd.Parameters.AddWithValue("@Fecha", v.fecha);
                cmd.Parameters.AddWithValue("@Total", v.total);
                cmd.Parameters.AddWithValue("@IdFuente", idFuente);

                cmd.ExecuteNonQuery();
                resultado.Insertados++;
            }
            catch (Exception ex)
            {
                resultado.Rechazados++;
                resultado.Errores.Add($"Error en venta id={v.venta_id}: {ex.Message}");
            }
        }

        return resultado;
    }
}