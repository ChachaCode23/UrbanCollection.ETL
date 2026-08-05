using Microsoft.EntityFrameworkCore;

namespace UrbanCollection.ETL.Data;

public class AnalyticsContext : DbContext
{
    public AnalyticsContext(DbContextOptions<AnalyticsContext> options) : base(options) { }

    public DbSet<ProductoAnalytics> Productos { get; set; }
    public DbSet<ClienteAnalytics> Clientes { get; set; }
    public DbSet<VentaAnalytics> Ventas { get; set; }
    public DbSet<FuenteDatosAnalytics> FuenteDatos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductoAnalytics>()
            .ToTable("Productos", "analytics")
            .HasKey(p => p.IdProducto);

        modelBuilder.Entity<ClienteAnalytics>()
            .ToTable("Clientes", "analytics")
            .HasKey(c => c.IdCliente);

        modelBuilder.Entity<VentaAnalytics>()
            .ToTable("Ventas", "analytics")
            .HasKey(v => v.IdVenta);

        modelBuilder.Entity<FuenteDatosAnalytics>()
            .ToTable("FuenteDatos", "analytics")
            .HasKey(f => f.IdFuente);
    }
}

public class ProductoAnalytics
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public decimal Precio { get; set; }
}

public class ClienteAnalytics
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Region { get; set; }
}

public class VentaAnalytics
{
    public int IdVenta { get; set; }
    public int IdCliente { get; set; }
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public int IdFuente { get; set; }
}

public class FuenteDatosAnalytics
{
    public int IdFuente { get; set; }
    public string TipoFuente { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; }
}