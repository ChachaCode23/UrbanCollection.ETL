namespace UrbanCollection.ETL.Data;

public class ProductoCSV
{
    public int producto_id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public string descripcion { get; set; } = string.Empty;
    public decimal precio_detalle { get; set; }
    public decimal precio_mayoreo { get; set; }
    public int stock { get; set; }
    public string sku { get; set; } = string.Empty;
    public int activo { get; set; }
}