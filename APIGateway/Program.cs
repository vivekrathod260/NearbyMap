using ProximityService.Grpc;
using BusinessService.Grpc;

var builder = WebApplication.CreateBuilder(args);

// gRPC clients for downstream services
builder.Services.AddGrpcClient<ProximityGrpc.ProximityGrpcClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:Proximity"] ?? "https://localhost:5101"));

builder.Services.AddGrpcClient<BusinessGrpc.BusinessGrpcClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:Business"] ?? "https://localhost:5102"));

var app = builder.Build();

// Proximity search endpoint
app.MapGet("/api/nearby", async (
    double lat, double lon, int? radius, string? category, int? limit,
    ProximityGrpc.ProximityGrpcClient proximity) =>
{
    var response = await proximity.SearchNearbyAsync(new NearbySearchRequest
    {
        Latitude = lat,
        Longitude = lon,
        RadiusMeters = radius ?? 5000,
        Category = category ?? "",
        MaxResults = limit ?? 20
    });

    return Results.Ok(response.Businesses.Select(b => new
    {
        b.BusinessId,
        b.Name,
        b.Latitude,
        b.Longitude,
        b.DistanceMeters,
        b.Category,
        b.Rating
    }));
});

// Business CRUD endpoints
app.MapGet("/api/business/{id}", async (string id, BusinessGrpc.BusinessGrpcClient client) =>
{
    var response = await client.GetBusinessAsync(new GetBusinessRequest { BusinessId = id });
    return Results.Ok(response);
});

app.MapPost("/api/business", async (CreateBusinessDto dto, BusinessGrpc.BusinessGrpcClient client) =>
{
    var response = await client.CreateBusinessAsync(new CreateBusinessRequest
    {
        Name = dto.Name,
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        Category = dto.Category,
        Address = dto.Address,
        Phone = dto.Phone
    });
    return Results.Created($"/api/business/{response.BusinessId}", response);
});

app.MapPut("/api/business/{id}", async (string id, UpdateBusinessDto dto, BusinessGrpc.BusinessGrpcClient client) =>
{
    var response = await client.UpdateBusinessAsync(new UpdateBusinessRequest
    {
        BusinessId = id,
        Name = dto.Name,
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        Category = dto.Category,
        Address = dto.Address,
        Phone = dto.Phone
    });
    return Results.Ok(response);
});

app.MapDelete("/api/business/{id}", async (string id, BusinessGrpc.BusinessGrpcClient client) =>
{
    var response = await client.DeleteBusinessAsync(new DeleteBusinessRequest { BusinessId = id });
    return response.Success ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();

public record CreateBusinessDto(string Name, double Latitude, double Longitude, string Category, string Address, string Phone);
public record UpdateBusinessDto(string Name, double Latitude, double Longitude, string Category, string Address, string Phone);

