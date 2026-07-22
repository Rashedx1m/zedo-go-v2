using Domain.Enums;

namespace Domain.Entities;

public class Driver : BaseEntity<int>
{
    public int UserId { get; private set; }
    public string CarModel { get; private set; } = string.Empty;
    public string CarColor { get; private set; } = string.Empty;
    public string PlateNumber { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public DriverStatus Status { get; private set; } = DriverStatus.Offline;
    public decimal Rating { get; private set; } = 5.0m;
    public int TotalRides { get; private set; } = 0;
    public decimal TotalEarnings { get; private set; } = 0;

    public User User { get; private set; } = null!;
    public DriverLocation? CurrentLocation { get; private set; }
    public ICollection<Request> Requests { get; private set; } = new List<Request>();

    private Driver() { }

    public static Driver Create(int userId, string carModel, string carColor, string plateNumber, string licenseNumber)
    {
        return new Driver
        {
            UserId = userId,
            CarModel = carModel,
            CarColor = carColor,
            PlateNumber = plateNumber,
            LicenseNumber = licenseNumber,
            Status = DriverStatus.Offline,
            Rating = 5.0m,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateCarInfo(string carModel, string carColor, string plateNumber)
    {
        CarModel = carModel;
        CarColor = carColor;
        PlateNumber = plateNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public void GoOnline()
    {
        if (Status == DriverStatus.InRide)
            throw new InvalidOperationException("لا يمكن تغيير الحالة أثناء الرحلة");
        Status = DriverStatus.Online;
        UpdatedAt = DateTime.UtcNow;
    }

    public void GoOffline()
    {
        if (Status == DriverStatus.InRide)
            throw new InvalidOperationException("لا يمكن تغيير الحالة أثناء الرحلة");
        Status = DriverStatus.Offline;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartRide()
    {
        Status = DriverStatus.InRide;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EndRide(decimal earning)
    {
        Status = DriverStatus.Online;
        TotalRides++;
        TotalEarnings += earning;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRating(decimal newRating)
    {
        Rating = ((Rating * TotalRides) + newRating) / (TotalRides + 1);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsAvailable => Status == DriverStatus.Online;
    public bool IsInRide => Status == DriverStatus.InRide;
}
