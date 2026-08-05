namespace UrbanCollection.ETL.Data;

public class VentaCSV
{
    public int venta_id { get; set; }
    public int cliente_id { get; set; }
    public string nombre_cliente { get; set; } = string.Empty;
    public int producto_id { get; set; }
    public string nombre_producto { get; set; } = string.Empty;
    public int cantidad { get; set; }
    public decimal precio_unitario { get; set; }
    public DateTime fecha { get; set; }
    public decimal total { get; set; }
}