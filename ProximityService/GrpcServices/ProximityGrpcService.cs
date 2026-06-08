using Grpc.Core;
using ProximityService.Grpc;
using ProximityService.Services;
using Microsoft.Data.SqlClient;
using System.Net.Sockets;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace ProximityService.GrpcServices;

public sealed class ProximityGrpcService : ProximityGrpc.ProximityGrpcBase
{
    private readonly ProximitySearchService _searchService;
    private readonly ILogger<ProximityGrpcService> _logger;

    public ProximityGrpcService(ProximitySearchService searchService, ILogger<ProximityGrpcService> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    public override async Task<NearbySearchResponse> SearchNearby(NearbySearchRequest request, ServerCallContext context)
    {
        try
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
        catch (Exception ex)
        {
            // Log the underlying exception for diagnostics
            _logger.LogError(ex, "Error in SearchNearby handler");

            // Map known infrastructure exceptions to Unavailable so clients can retry
            if (ex is SocketException || ex is RedisConnectionException || ex is SqlException || (ex.InnerException is SocketException))
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "Downstream resource unavailable"));
            }

            // For other exceptions expose a generic internal status
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
