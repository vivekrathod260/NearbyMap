using ProximityService.Grpc;
using BusinessService.Grpc;
using Grpc.Core;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.Security;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// gRPC clients for downstream services
var environment = builder.Environment;

builder.Services.AddGrpcClient<BusinessGrpc.BusinessGrpcClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:Business"] ?? "http://localhost:7196"))
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new SocketsHttpHandler();
        handler.EnableMultipleHttp2Connections = true;
        if (environment.IsDevelopment())
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        return handler;
    });

builder.Services.AddGrpcClient<ProximityGrpc.ProximityGrpcClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:Proximity"] ?? "http://localhost:7102"))
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new SocketsHttpHandler();
        handler.EnableMultipleHttp2Connections = true;
        if (environment.IsDevelopment())
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        return handler;
    });

var app = builder.Build();

static bool IsDownstreamUnavailable(Exception? ex)
{
    if (ex == null) return false;
    if (ex is RpcException rex && rex.StatusCode == StatusCode.Unavailable) return true;
    if (ex is SocketException) return true;
    return IsDownstreamUnavailable(ex.InnerException);
}

// Proximity search endpoint
app.MapGet("/api/nearby", async (double lat, double lon, int? radius, string? category, int? limit, ProximityGrpc.ProximityGrpcClient proximity) =>
{
    try
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
    }
    catch (Exception ex)
    {
        if (IsDownstreamUnavailable(ex))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        throw;
    }
});

// Business CRUD endpoints
app.MapGet("/api/business/{id}", async (string id, BusinessGrpc.BusinessGrpcClient client) =>
{
    try
    {
        var response = await client.GetBusinessAsync(new GetBusinessRequest { BusinessId = id });
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        if (IsDownstreamUnavailable(ex))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        throw;
    }
});

app.MapPost("/api/business", async (CreateBusinessDto dto, BusinessGrpc.BusinessGrpcClient client) =>
{
    try
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
    }
    catch (Exception ex)
    {
        if (IsDownstreamUnavailable(ex))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        throw;
    }
});

app.MapPut("/api/business/{id}", async (string id, UpdateBusinessDto dto, BusinessGrpc.BusinessGrpcClient client) =>
{
    try
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
    }
    catch (Exception ex)
    {
        if (IsDownstreamUnavailable(ex))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        throw;
    }
});

app.MapDelete("/api/business/{id}", async (string id, BusinessGrpc.BusinessGrpcClient client) =>
{
    try
    {
        var response = await client.DeleteBusinessAsync(new DeleteBusinessRequest { BusinessId = id });
        return response.Success ? Results.NoContent() : Results.NotFound();
    }
    catch (Exception ex)
    {
        if (IsDownstreamUnavailable(ex))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        throw;
    }
});

app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();

public record CreateBusinessDto(string Name, double Latitude, double Longitude, string Category, string Address, string Phone);
public record UpdateBusinessDto(string Name, double Latitude, double Longitude, string Category, string Address, string Phone);

