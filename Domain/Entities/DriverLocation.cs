namespace Domain.Entities;

public class DriverLocation : BaseEntity<int>
{
    public int DriverId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public DateTime LastUpdate { get; private set; }

    public Driver Driver { get; private set; } = null!;

    private DriverLocation() { }

    public static DriverLocation Create(int driverId, double latitude, double longitude)
    {
        return new DriverLocation
        {
            DriverId = driverId,
            Latitude = latitude,
            Longitude = longitude,
            LastUpdate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
        LastUpdate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLocationFresh(int maxSecondsOld = 60)
    {
        return (DateTime.UtcNow - LastUpdate).TotalSeconds <= maxSecondsOld;
    }

    public double CalculateDistanceTo(double targetLat, double targetLon)
    {
        return GeoHelper.CalculateDistance(Latitude, Longitude, targetLat, targetLon);
    }
}

public static class GeoHelper
{
    private const double EarthRadiusKm = 6371.0;

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    public static TimeSpan CalculateETA(double distanceKm, double avgSpeedKmh = 40.0)
    {
        var hours = distanceKm / avgSpeedKmh * 1.2;
        return TimeSpan.FromHours(hours);
    }

    public static bool IsValidCoordinates(double lat, double lon)
    {
        return lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
