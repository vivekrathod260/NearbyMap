using Microsoft.EntityFrameworkCore;
using ProximityService.Data;
using ProximityService.Geo;
using StackExchange.Redis;
using System.Text.Json;

namespace ProximityService.Services;

/// <summary>
/// Core proximity search engine.
/// Strategy:
/// 1. Compute geohash for query point at optimal precision
/// 2. Get center + 8 neighbor geohashes (covers all edge cases)
/// 3. Check Redis for cached business IDs per geohash cell
/// 4. On cache miss, query SQL Server with geohash prefix index
/// 5. Filter candidates by exact Haversine distance
/// 6. Sort by distance, return top N
/// </summary>
public sealed class ProximitySearchService
{
    private readonly ProximityDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    private static readonly TimeSpan GeoCellTtl = TimeSpan.FromMinutes(5);

    public ProximitySearchService(ProximityDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    public async Task<List<BusinessWithDistance>> SearchNearbyAsync(double latitude, double longitude, int radiusMeters, string? category, int maxResults = 20)
    {
        int precision = GeohashHelper.GetPrecisionForRadius(radiusMeters);
        string centerHash = GeohashHelper.Encode(latitude, longitude, precision);
        var geohashes = GeohashHelper.GetNeighbors(centerHash);

        var candidates = new List<BusinessLocation>();
        var db = _redis.GetDatabase();

        foreach (var hash in geohashes)
        {
            string cacheKey = string.IsNullOrEmpty(category) ? $"geo:{hash}" : $"geo:{hash}:{category}";

            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                var items = JsonSerializer.Deserialize<List<BusinessLocation>>(cached!);
                if (items != null) candidates.AddRange(items);
            }
            else
            {
                // Cache miss - query database using geohash prefix index
                var query = _db.BusinessLocations.AsNoTracking().Where(b => b.Geohash.StartsWith(hash));

                if (!string.IsNullOrEmpty(category)) query = query.Where(b => b.Category == category);

                var items = await query.ToListAsync();
                candidates.AddRange(items);

                // Cache the result (even if empty to prevent repeated misses)
                var json = JsonSerializer.Serialize(items);
                await db.StringSetAsync(cacheKey, json, GeoCellTtl);
            }
        }

        // Exact distance filter using Haversine
        var results = candidates
            .Select(b => new BusinessWithDistance
            {
                Business = b,
                DistanceMeters = GeohashHelper.CalculateDistance(latitude, longitude, b.Latitude, b.Longitude)
            })
            .Where(x => x.DistanceMeters <= radiusMeters)
            .OrderBy(x => x.DistanceMeters)
            .Take(maxResults)
            .ToList();

        return results;
    }
}

public class BusinessWithDistance
{
    public BusinessLocation Business { get; set; } = null!;
    public double DistanceMeters { get; set; }
}
