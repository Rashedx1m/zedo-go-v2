namespace Domain.Enums;

public enum RequestStatus
{
    Pending = 1,
    Searching = 2,
    Accepted = 3,
    Arrived = 4,
    InProgress = 5,
    Completed = 6,
    Canceled = 7,
    NoDriversAvailable = 8,
    Failed = 9
}

public enum DriverStatus
{
    Offline = 0,
    Online = 1,
    Busy = 2,
    InRide = 3
}

public enum PaymentMethod
{
    Cash = 1,
    Wallet = 2,
    Card = 3,
    Online = 4
}

public enum UserRole
{
    Customer = 1,
    Driver = 2,
    Admin = 3
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}
