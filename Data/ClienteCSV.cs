namespace UrbanCollection.ETL.Data;

public class ClienteCSV
{
    public int cliente_id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string telefono { get; set; } = string.Empty;
    public string rol { get; set; } = string.Empty;
}