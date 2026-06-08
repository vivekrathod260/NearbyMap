using BusinessService.Data;
using BusinessService.Geo;
using BusinessService.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace BusinessService.GrpcServices;

public sealed class BusinessGrpcService : BusinessGrpc.BusinessGrpcBase
{
    private readonly BusinessDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan BusinessTtl = TimeSpan.FromMinutes(15);

    public BusinessGrpcService(BusinessDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    public override async Task<BusinessResponse> GetBusiness(GetBusinessRequest request, ServerCallContext context)
    {
        var cache = _redis.GetDatabase();
        string cacheKey = $"biz:{request.BusinessId}";

        var cached = await cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var b = JsonSerializer.Deserialize<Business>(cached!);
            if (b != null) return MapToResponse(b);
        }

        var business = await _db.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.BusinessId == request.BusinessId);

        if (business == null) throw new RpcException(new Status(StatusCode.NotFound, "Business not found"));

        await cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(business), BusinessTtl);

        return MapToResponse(business);
    }

    public override async Task<GetBusinessesByIdsResponse> GetBusinessesByIds(GetBusinessesByIdsRequest request, ServerCallContext context)
    {
        var response = new GetBusinessesByIdsResponse();
        var cache = _redis.GetDatabase();
        var missingIds = new List<string>();

        foreach (var id in request.BusinessIds)
        {
            var cached = await cache.StringGetAsync($"biz:{id}");
            if (cached.HasValue)
            {
                var b = JsonSerializer.Deserialize<Business>(cached!);
                if (b != null) response.Businesses.Add(MapToResponse(b));
            }
            else
            {
                missingIds.Add(id);
            }
        }

        if (missingIds.Count > 0)
        {
            var businesses = await _db.Businesses.AsNoTracking()
                .Where(b => missingIds.Contains(b.BusinessId))
                .ToListAsync();

            foreach (var b in businesses)
            {
                await cache.StringSetAsync($"biz:{b.BusinessId}", JsonSerializer.Serialize(b), BusinessTtl);
                response.Businesses.Add(MapToResponse(b));
            }
        }

        return response;
    }

    public override async Task<BusinessResponse> CreateBusiness(CreateBusinessRequest request, ServerCallContext context)
    {
        var geohash = GeohashHelper.Encode(request.Latitude, request.Longitude);
        var business = new Business
        {
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Geohash = geohash,
            Category = request.Category,
            Address = request.Address,
            Phone = request.Phone
        };

        _db.Businesses.Add(business);
        await _db.SaveChangesAsync();

        // Invalidate geohash cell cache
        var cache = _redis.GetDatabase();
        await cache.KeyDeleteAsync($"geo:{geohash}");
        await cache.KeyDeleteAsync($"geo:{geohash}:{business.Category}");

        return MapToResponse(business);
    }

    public override async Task<BusinessResponse> UpdateBusiness(UpdateBusinessRequest request, ServerCallContext context)
    {
        var business = await _db.Businesses.FindAsync(request.BusinessId);
        if (business == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Business not found"));

        var oldGeohash = business.Geohash;

        business.Name = request.Name;
        business.Latitude = request.Latitude;
        business.Longitude = request.Longitude;
        business.Geohash = GeohashHelper.Encode(request.Latitude, request.Longitude);
        business.Category = request.Category;
        business.Address = request.Address;
        business.Phone = request.Phone;
        business.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Invalidate caches
        var cache = _redis.GetDatabase();
        await cache.KeyDeleteAsync($"biz:{business.BusinessId}");
        await cache.KeyDeleteAsync($"geo:{oldGeohash}");
        await cache.KeyDeleteAsync($"geo:{business.Geohash}");

        return MapToResponse(business);
    }

    public override async Task<DeleteBusinessResponse> DeleteBusiness(DeleteBusinessRequest request, ServerCallContext context)
    {
        var business = await _db.Businesses.FindAsync(request.BusinessId);
        if (business == null) return new DeleteBusinessResponse { Success = false };

        _db.Businesses.Remove(business);
        await _db.SaveChangesAsync();

        var cache = _redis.GetDatabase();
        await cache.KeyDeleteAsync($"biz:{business.BusinessId}");
        await cache.KeyDeleteAsync($"geo:{business.Geohash}");

        return new DeleteBusinessResponse { Success = true };
    }

    private static BusinessResponse MapToResponse(Business b) => new()
    {
        BusinessId = b.BusinessId,
        Name = b.Name,
        Latitude = b.Latitude,
        Longitude = b.Longitude,
        Category = b.Category,
        Address = b.Address,
        Phone = b.Phone,
        Rating = b.Rating,
        Geohash = b.Geohash
    };
}
