using Domain.Enums;

namespace Domain.Entities;

public class Request : BaseEntity<int>
{
    public int CustomerId { get; private set; }
    public int? DriverId { get; private set; }
    public double PickupLatitude { get; private set; }
    public double PickupLongitude { get; private set; }
    public double DropLatitude { get; private set; }
    public double DropLongitude { get; private set; }
    public RequestStatus Status { get; private set; } = RequestStatus.Pending;
    public decimal? EstimatedCost { get; private set; }
    public decimal? ActualCost { get; private set; }
    public double? DistanceKm { get; private set; }
    public int? DurationMinutes { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? ArrivedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CanceledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CanceledBy { get; private set; }

    public Customer Customer { get; private set; } = null!;
    public Driver? Driver { get; private set; }
    public Payment? Payment { get; private set; }

    private Request() { }

    public static Request Create(
     int customerId,
     double pickupLat,
     double pickupLon,
     double dropLat,
     double dropLon,
     decimal? estimatedCost = null)
    {
        if (!GeoHelper.IsValidCoordinates(pickupLat, pickupLon))
            throw new ArgumentException("إحداثيات نقطة الانطلاق غير صالحة");

        if (!GeoHelper.IsValidCoordinates(dropLat, dropLon))
            throw new ArgumentException("إحداثيات نقطة الوصول غير صالحة");

        return new Request
        {
            CustomerId = customerId,
            PickupLatitude = pickupLat,
            PickupLongitude = pickupLon,
            DropLatitude = dropLat,
            DropLongitude = dropLon,
            Status = RequestStatus.Pending,
            EstimatedCost = estimatedCost,
            DistanceKm = GeoHelper.CalculateDistance(pickupLat, pickupLon, dropLat, dropLon),
            CreatedAt = DateTime.UtcNow
        };
    }


    public void StartSearching()
    {
        EnsureStatus(RequestStatus.Pending);
        Status = RequestStatus.Searching;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignDriver(int driverId)
    {
        EnsureStatus(RequestStatus.Pending, RequestStatus.Searching);
        DriverId = driverId;
        Status = RequestStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DriverArrived()
    {
        EnsureStatus(RequestStatus.Accepted);
        Status = RequestStatus.Arrived;
        ArrivedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartRide()
    {
        EnsureStatus(RequestStatus.Arrived);
        Status = RequestStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(decimal actualCost, int durationMinutes)
    {
        EnsureStatus(RequestStatus.InProgress);
        Status = RequestStatus.Completed;
        ActualCost = actualCost;
        DurationMinutes = durationMinutes;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string canceledBy, string? reason = null)
    {
        if (Status == RequestStatus.Completed || Status == RequestStatus.Canceled)
            throw new InvalidOperationException("لا يمكن إلغاء طلب منتهي أو ملغى");

        Status = RequestStatus.Canceled;
        CanceledBy = canceledBy;
        CancellationReason = reason;
        CanceledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkNoDriversAvailable()
    {
        EnsureStatus(RequestStatus.Pending, RequestStatus.Searching);
        Status = RequestStatus.NoDriversAvailable;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = RequestStatus.Failed;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(params RequestStatus[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status))
            throw new InvalidOperationException($"العملية غير مسموحة في الحالة الحالية: {Status}");
    }

    public bool IsActive => Status is RequestStatus.Pending or RequestStatus.Searching
        or RequestStatus.Accepted or RequestStatus.Arrived or RequestStatus.InProgress;

    public bool IsFinished => Status is RequestStatus.Completed or RequestStatus.Canceled
        or RequestStatus.NoDriversAvailable or RequestStatus.Failed;

    public bool CanBeCanceled => !IsFinished;
}
