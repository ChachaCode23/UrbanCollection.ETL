using Microsoft.EntityFrameworkCore;
using UrbanCollection.ETL.Data;

namespace UrbanCollection.ETL.Carga;

public class CargadorEF
{
    private readonly AnalyticsContext _context;

    public CargadorEF(AnalyticsContext context)
    {
        _context = context;
    }

    public ResultadoCarga ConsultarResumenProductos()
    {
        var resultado = new ResultadoCarga { Entidad = "Productos (EF - Consulta)" };
        try
        {
            var productos = _context.Productos.AsNoTracking().ToList();
            resultado.Procesados = productos.Count;
            resultado.Insertados = productos.Count;
        }
        catch (Exception ex)
        {
            resultado.Errores.Add($"Error EF: {ex.Message}");
        }
        return resultado;
    }

    public ResultadoCarga ConsultarResumenClientes()
    {
        var resultado = new ResultadoCarga { Entidad = "Clientes (EF - Consulta)" };
        try
        {
            var clientes = _context.Clientes.AsNoTracking().ToList();
            resultado.Procesados = clientes.Count;
            resultado.Insertados = clientes.Count;
        }
        catch (Exception ex)
        {
            resultado.Errores.Add($"Error EF: {ex.Message}");
        }
        return resultado;
    }

    public ResultadoCarga ConsultarResumenVentas()
    {
        var resultado = new ResultadoCarga { Entidad = "Ventas (EF - Consulta)" };
        try
        {
            var ventas = _context.Ventas.AsNoTracking().ToList();
            resultado.Procesados = ventas.Count;
            resultado.Insertados = ventas.Count;
        }
        catch (Exception ex)
        {
            resultado.Errores.Add($"Error EF: {ex.Message}");
        }
        return resultado;
    }
}