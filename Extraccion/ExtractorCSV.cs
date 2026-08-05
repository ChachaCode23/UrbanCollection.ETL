using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using UrbanCollection.ETL.Data;

namespace UrbanCollection.ETL.Extraccion;

public class ExtractorCSV
{
    public List<ProductoCSV> LeerProductos(string ruta)
    {
        if (!File.Exists(ruta))
            throw new FileNotFoundException($"Archivo no encontrado: {ruta}");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var reader = new StreamReader(ruta);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<ProductoCSV>().ToList();
    }

    public List<ClienteCSV> LeerClientes(string ruta)
    {
        if (!File.Exists(ruta))
            throw new FileNotFoundException($"Archivo no encontrado: {ruta}");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var reader = new StreamReader(ruta);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<ClienteCSV>().ToList();
    }

    public List<VentaCSV> LeerVentas(string ruta)
    {
        if (!File.Exists(ruta))
            throw new FileNotFoundException($"Archivo no encontrado: {ruta}");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var reader = new StreamReader(ruta);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<VentaCSV>().ToList();
    }
}