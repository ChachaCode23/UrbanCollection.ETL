using UrbanCollection.ETL.Data;

namespace UrbanCollection.ETL.Extraccion;

public class ValidadorDatos
{
    public (List<ProductoCSV> validos, List<string> errores) ValidarProductos(List<ProductoCSV> productos)
    {
        var validos = new List<ProductoCSV>();
        var errores = new List<string>();
        var skusVistos = new HashSet<string>();

        foreach (var p in productos)
        {
            if (string.IsNullOrWhiteSpace(p.nombre))
            {
                errores.Add($"Producto id={p.producto_id}: nombre es obligatorio.");
                continue;
            }

            if (p.precio_detalle <= 0)
            {
                errores.Add($"Producto '{p.nombre}': precio_detalle debe ser mayor que 0.");
                continue;
            }

            if (p.stock < 0)
            {
                errores.Add($"Producto '{p.nombre}': stock no puede ser negativo.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(p.sku))
            {
                errores.Add($"Producto '{p.nombre}': SKU es obligatorio.");
                continue;
            }

            if (skusVistos.Contains(p.sku))
            {
                errores.Add($"Producto '{p.nombre}': SKU duplicado '{p.sku}'.");
                continue;
            }

            skusVistos.Add(p.sku);
            validos.Add(p);
        }

        return (validos, errores);
    }

    public (List<ClienteCSV> validos, List<string> errores) ValidarClientes(List<ClienteCSV> clientes)
    {
        var validos = new List<ClienteCSV>();
        var errores = new List<string>();
        var nombresVistos = new HashSet<string>();

        foreach (var c in clientes)
        {
            if (string.IsNullOrWhiteSpace(c.nombre))
            {
                errores.Add($"Cliente id={c.cliente_id}: nombre es obligatorio.");
                continue;
            }

            if (nombresVistos.Contains(c.nombre.ToLower()))
            {
                errores.Add($"Cliente '{c.nombre}': duplicado.");
                continue;
            }

            nombresVistos.Add(c.nombre.ToLower());
            validos.Add(c);
        }

        return (validos, errores);
    }

    public (List<VentaCSV> validos, List<string> errores) ValidarVentas(List<VentaCSV> ventas)
    {
        var validos = new List<VentaCSV>();
        var errores = new List<string>();
        var idsVistos = new HashSet<int>();

        foreach (var v in ventas)
        {
            if (v.cliente_id <= 0)
            {
                errores.Add($"Venta id={v.venta_id}: cliente_id inválido.");
                continue;
            }

            if (v.producto_id <= 0)
            {
                errores.Add($"Venta id={v.venta_id}: producto_id inválido.");
                continue;
            }

            if (v.cantidad <= 0)
            {
                errores.Add($"Venta id={v.venta_id}: cantidad debe ser mayor que 0.");
                continue;
            }

            if (v.precio_unitario <= 0)
            {
                errores.Add($"Venta id={v.venta_id}: precio_unitario debe ser mayor que 0.");
                continue;
            }

            if (v.fecha == DateTime.MinValue)
            {
                errores.Add($"Venta id={v.venta_id}: fecha inválida.");
                continue;
            }

            if (idsVistos.Contains(v.venta_id))
            {
                errores.Add($"Venta id={v.venta_id}: duplicada.");
                continue;
            }

            // Recalcular total
            v.total = v.cantidad * v.precio_unitario;

            idsVistos.Add(v.venta_id);
            validos.Add(v);
        }

        return (validos, errores);
    }
}