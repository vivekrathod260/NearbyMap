using BusinessService.Data;
using BusinessService.GrpcServices;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

builder.Services.AddDbContext<BusinessDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"),
        sql => sql.EnableRetryOnFailure(3)));

var app = builder.Build();

app.MapGrpcService<BusinessGrpcService>();
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();
