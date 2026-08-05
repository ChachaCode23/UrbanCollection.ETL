using Microsoft.EntityFrameworkCore;
using UrbanCollection.ETL;
using UrbanCollection.ETL.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AnalyticsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ECommerceDB")),
    ServiceLifetime.Singleton);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();