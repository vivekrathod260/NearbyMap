namespace BusinessService.Geo;

public static class GeohashHelper
{
    private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

    public static string Encode(double latitude, double longitude, int precision = 6)
    {
        double latMin = -90, latMax = 90;
        double lonMin = -180, lonMax = 180;
        bool isLon = true;
        int bit = 0, ch = 0;
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
}
