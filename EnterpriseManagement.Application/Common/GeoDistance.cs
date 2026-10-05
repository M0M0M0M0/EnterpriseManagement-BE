namespace EnterpriseManagement.Application.Common;

public static class GeoDistance
{
    private const double EarthRadiusMeters = 6_371_000;

    // Khoảng cách đường chim bay giữa 2 toạ độ (công thức Haversine), đơn vị mét.
    public static double Meters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
