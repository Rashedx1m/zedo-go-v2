using Domain.Enums;

namespace Application.DTOs;

// User DTOs
public record UserDto(int Id, string FullName, string Email, string Phone, string Role, bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt);
public record RegisterUserDto(string FullName, string Email, string Phone, string Password, UserRole Role = UserRole.Customer);
public record LoginDto(string Email, string Password);
public record LoginResultDto(int UserId, string FullName, string Email, string Role, string Token, DateTime ExpiresAt , int? CustomerId = null,  int? DriverId = null);
public record UpdateUserDto(string FullName, string Phone);
public record ChangePasswordDto(string CurrentPassword, string NewPassword);

// Customer DTOs
public record CustomerDto(int Id, int UserId, string FullName, string Email, string Phone, decimal Rating, int TotalRides, DateTime CreatedAt);

// Driver DTOs
public record DriverDto(int Id, int UserId, string FullName, string Phone, string CarModel, string CarColor, string PlateNumber, string Status, decimal Rating, int TotalRides, decimal TotalEarnings, DateTime CreatedAt);
public record RegisterDriverDto(int UserId, string CarModel, string CarColor, string PlateNumber, string LicenseNumber);
public record UpdateDriverDto(string CarModel, string CarColor, string PlateNumber);
public record NearbyDriverDto(int DriverId, string FullName, string CarModel, string CarColor, string PlateNumber, double DistanceKm, int EstimatedMinutes);
public record DriverLocationDto(int DriverId, double Latitude, double Longitude, DateTime LastUpdate, bool IsFresh);
public record UpdateLocationDto(double Latitude, double Longitude);

// Request DTOs
public record RequestDto(int Id, int CustomerId, int? DriverId, double PickupLatitude, double PickupLongitude, double DropLatitude, double DropLongitude, string Status, decimal? EstimatedCost, decimal? ActualCost, double? DistanceKm, int? DurationMinutes, DateTime CreatedAt, DateTime? AcceptedAt, DateTime? CompletedAt);
public record RequestFullDto(int Id, string Status, double PickupLatitude,
    double PickupLongitude,
    double DropLatitude,
    double DropLongitude, 
    decimal? EstimatedCost,
    decimal? ActualCost,
    double? DistanceKm,
    int? DurationMinutes,
    DateTime CreatedAt,
    int CustomerId, 
    string CustomerName,
    string CustomerPhone,
    int? DriverId,
    string? DriverName,
    string? DriverPhone, 
    string? CarModel,
    string? CarColor,
    string? PlateNumber, 
    double? DriverLatitude,
    double? DriverLongitude);
public record CreateRequestDto(double PickupLatitude, double PickupLongitude, double DropLatitude, double DropLongitude);
public record NearbyRequestDto(int RequestId, double PickupLatitude, double PickupLongitude, double DropLatitude, double DropLongitude, double DistanceToPickup, decimal? EstimatedCost, DateTime CreatedAt, int MinutesSinceCreated);

// Payment DTOs
public record PaymentDto(int Id, int RequestId, decimal Amount, decimal CompanyCommission, decimal DriverEarning, string PaymentMethod, string Status, DateTime CreatedAt);
public record ProcessPaymentDto(int RequestId, PaymentMethod Method);
public record PaymentResultDto(int PaymentId, decimal Amount, decimal CompanyCommission, decimal DriverEarning, string PaymentMethod, DateTime ProcessedAt);

// Pricing DTOs
public record PricingDto(int Id, string Name, decimal BaseFare, decimal PricePerKm, decimal PricePerMinute, decimal MinimumFare, bool IsActive, DateTime CreatedAt);
public record CreatePricingDto(string Name, decimal BaseFare, decimal PricePerKm, decimal PricePerMinute, decimal MinimumFare = 10.0m);
public record CostEstimateDto(decimal BaseFare, double DistanceKm, decimal DistanceCost, int EstimatedMinutes, decimal TimeCost, decimal Subtotal, decimal MinimumFare, decimal Total);

// Report DTOs
public record DriverEarningsReportDto(int DriverId, string DriverName, int TotalRides, decimal TotalAmount, decimal TotalCommission, decimal TotalEarnings, decimal AvgPerRide, DateTime? FromDate, DateTime? ToDate);
public record CompanyRevenueReportDto(int TotalTransactions, decimal TotalRevenue, decimal TotalCommission, decimal TotalDriverPayouts, decimal AvgTransactionAmount, DateTime? FromDate, DateTime? ToDate);
public record DashboardStatsDto(int TotalUsers, int TotalDrivers, int TotalCustomers, int ActiveDrivers, int PendingRequests, int TodayCompletedRides, decimal TodayRevenue);

