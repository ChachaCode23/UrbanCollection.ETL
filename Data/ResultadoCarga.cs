namespace UrbanCollection.ETL.Data;

public class ResultadoCarga
{
    public string Entidad { get; set; } = string.Empty;
    public int Procesados { get; set; }
    public int Insertados { get; set; }
    public int Rechazados { get; set; }
    public List<string> Errores { get; set; } = new();
}