using Grpc.Core;
using ProximityService.Grpc;
using ProximityService.Services;

namespace ProximityService.GrpcServices;

public sealed class ProximityGrpcService : ProximityGrpc.ProximityGrpcBase
{
    private readonly ProximitySearchService _searchService;

    public ProximityGrpcService(ProximitySearchService searchService)
    {
        _searchService = searchService;
    }

    public override async Task<NearbySearchResponse> SearchNearby(
        NearbySearchRequest request, ServerCallContext context)
    {
        var maxResults = request.MaxResults > 0 ? request.MaxResults : 20;
        var radius = request.RadiusMeters > 0 ? request.RadiusMeters : 5000;

        var results = await _searchService.SearchNearbyAsync(
            request.Latitude,
            request.Longitude,
            radius,
            string.IsNullOrEmpty(request.Category) ? null : request.Category,
            maxResults);

        var response = new NearbySearchResponse();
        foreach (var r in results)
        {
            response.Businesses.Add(new BusinessResult
            {
                BusinessId = r.Business.BusinessId,
                Name = r.Business.Name,
                Latitude = r.Business.Latitude,
                Longitude = r.Business.Longitude,
                DistanceMeters = r.DistanceMeters,
                Category = r.Business.Category,
                Rating = r.Business.Rating
            });
        }

        return response;
    }
}
