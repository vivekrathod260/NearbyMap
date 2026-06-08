using Microsoft.EntityFrameworkCore;
using ProximityService.Data;
using ProximityService.GrpcServices;
using ProximityService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// Redis - distributed cache for geohash cells
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379")
);

// SQL Server - read-optimized with geohash indexes
builder.Services.AddDbContext<ProximityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"), sql => sql.EnableRetryOnFailure(3))
);

builder.Services.AddDbContext<ProximityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer"),
        sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(3);
            sqlServerOptions.MigrationsHistoryTable("__ProximityMigrationsHistory");
        }
    )
);

builder.Services.AddScoped<ProximitySearchService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProximityDbContext>();
    db.Database.Migrate();
}

app.MapGrpcService<ProximityGrpcService>();
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();
