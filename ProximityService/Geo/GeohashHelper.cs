namespace ProximityService.Geo;

/// <summary>
/// High-performance geohash encoder/decoder with neighbor calculation.
/// Geohash precision 6 = ~1.2km x 0.6km cells - optimal for proximity search.
/// </summary>
public static class GeohashHelper
{
    private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";
    private static readonly Dictionary<char, int> Base32Lookup = Base32
        .Select((c, i) => (c, i))
        .ToDictionary(x => x.c, x => x.i);

    private static readonly (int dLat, int dLon)[] NeighborOffsets =
    [
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1),           (0, 1),
        (1, -1),  (1, 0),  (1, 1)
    ];

    public static string Encode(double latitude, double longitude, int precision = 6)
    {
        double latMin = -90, latMax = 90;
        double lonMin = -180, lonMax = 180;
        bool isLon = true;
        int bit = 0;
        int ch = 0;
        var hash = new char[precision];
        int hashIndex = 0;

        while (hashIndex < precision)
        {
            if (isLon)
            {
                double mid = (lonMin + lonMax) / 2;
                if (longitude >= mid) { ch |= 1 << (4 - bit); lonMin = mid; }
                else { lonMax = mid; }
            }
            else
            {
                double mid = (latMin + latMax) / 2;
                if (latitude >= mid) { ch |= 1 << (4 - bit); latMin = mid; }
                else { latMax = mid; }
            }

            isLon = !isLon;
            if (++bit == 5)
            {
                hash[hashIndex++] = Base32[ch];
                bit = 0;
                ch = 0;
            }
        }

        return new string(hash);
    }

    public static (double Latitude, double Longitude) Decode(string geohash)
    {
        double latMin = -90, latMax = 90;
        double lonMin = -180, lonMax = 180;
        bool isLon = true;

        foreach (char c in geohash)
        {
            int val = Base32Lookup[c];
            for (int bit = 4; bit >= 0; bit--)
            {
                bool isSet = false;
                if((val & (1 << bit)) != 0) isSet = true;

                if (isLon)
                {
                    double mid = (lonMin + lonMax) / 2;

                    if (isSet) lonMin = mid;
                    else lonMax = mid;
                }
                else
                {
                    double mid = (latMin + latMax) / 2;

                    if (isSet) latMin = mid;
                    else latMax = mid;
                }

                isLon = !isLon;
            }
        }

        return ((latMin + latMax) / 2, (lonMin + lonMax) / 2);
    }

    /// <summary>
    /// Returns the center geohash + 8 surrounding neighbors for complete coverage.
    /// This ensures no edge-case misses for radius searches.
    /// </summary>
    public static List<string> GetNeighbors(string geohash)
    {
        var result = new List<string>(9) { geohash };
        var (lat, lon) = Decode(geohash);
        int precision = geohash.Length;

        // Calculate cell dimensions for the precision level
        double latErr = 90.0;
        double lonErr = 180.0;
        for (int i = 0; i < precision; i++)
        {
            if (i % 2 == 0) { lonErr /= 8; latErr /= 4; }
            else { lonErr /= 4; latErr /= 8; }
        }

        double latStep = latErr * 2;
        double lonStep = lonErr * 2;

        foreach (var (dLat, dLon) in NeighborOffsets)
        {
            double nLat = lat + dLat * latStep;
            double nLon = lon + dLon * lonStep;
            if (nLat is >= -90 and <= 90 && nLon is >= -180 and <= 180)
            {
                result.Add(Encode(nLat, nLon, precision));
            }
        }

        return result;
    }

    /// <summary>
    /// Determines optimal geohash precision based on search radius.
    /// Larger radius = lower precision (bigger cells, fewer lookups).
    /// </summary>
    public static int GetPrecisionForRadius(int radiusMeters) => radiusMeters switch
    {
        <= 500 => 7,    // ~153m x 153m
        <= 2000 => 6,   // ~1.2km x 0.6km
        <= 5000 => 5,   // ~4.9km x 4.9km
        <= 20000 => 4,  // ~39km x 19.5km
        _ => 3          // ~156km x 156km
    };

    /// <summary>
    /// Haversine formula for exact distance calculation (meters).
    /// Used as final filter after geohash-based candidate retrieval.
    /// </summary>
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
