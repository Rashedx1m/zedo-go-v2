using Application.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto);
    Task<Result<int>> RegisterAsync(RegisterUserDto dto);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<Result> ValidateTokenAsync(string token);
}

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(int id);
    Task<Result<UserDto>> GetByEmailAsync(string email);
    Task<Result<IEnumerable<UserDto>>> GetAllAsync();
    Task<Result<IEnumerable<UserDto>>> GetByRoleAsync(UserRole role);
    Task<Result> UpdateAsync(int id, UpdateUserDto dto);
    Task<Result> ActivateAsync(int id);
    Task<Result> DeactivateAsync(int id);
    Task<Result> DeleteAsync(int id);
}

public interface ICustomerService
{
    Task<Result<CustomerDto>> GetByIdAsync(int id);
    Task<Result<CustomerDto>> GetByUserIdAsync(int userId);
    Task<Result<IEnumerable<CustomerDto>>> GetAllAsync();
    Task<Result<int>> CreateAsync(int userId);
}

public interface IDriverService
{
    Task<Result<DriverDto>> GetByIdAsync(int id);
    Task<Result<DriverDto>> GetByUserIdAsync(int userId);
    Task<Result<IEnumerable<DriverDto>>> GetAllAsync();
    Task<Result<IEnumerable<DriverDto>>> GetAvailableAsync();
    Task<Result<int>> RegisterAsync(RegisterDriverDto dto);
    Task<Result> UpdateAsync(int id, UpdateDriverDto dto);
    Task<Result> GoOnlineAsync(int driverId);
    Task<Result> GoOfflineAsync(int driverId);
    Task<Result> UpdateLocationAsync(int driverId, UpdateLocationDto dto);
    Task<Result<DriverLocationDto>> GetLocationAsync(int driverId);
    Task<Result<IEnumerable<NearbyDriverDto>>> GetNearbyAsync(double lat, double lon, double radiusKm = 5);
}

public interface IRequestService
{
    Task<Result<RequestDto>> GetByIdAsync(int id);
    Task<Result<RequestFullDto>> GetFullInfoAsync(int id);
    Task<Result<IEnumerable<RequestDto>>> GetByCustomerAsync(int customerId);
    Task<Result<IEnumerable<RequestDto>>> GetByDriverAsync(int driverId);
    Task<Result<IEnumerable<RequestDto>>> GetByStatusAsync(RequestStatus status);
    Task<Result<RequestDto>> GetActiveByCustomerAsync(int customerId);
    Task<Result<RequestDto>> GetActiveByDriverAsync(int driverId);
    Task<Result<int>> CreateAsync(int customerId, CreateRequestDto dto);
    Task<Result> AcceptAsync(int requestId, int driverId);
    Task<Result> DriverArrivedAsync(int requestId);
    Task<Result> StartRideAsync(int requestId);
    Task<Result<PaymentResultDto>> CompleteAsync(int requestId, PaymentMethod paymentMethod);
    Task<Result> CancelAsync(int requestId, string canceledBy, string? reason = null);
    Task<Result<IEnumerable<NearbyRequestDto>>> GetNearbyForDriverAsync(int driverId, double radiusKm = 10);
    Task<Result<int>> CreateByUserIdAsync(int userId, CreateRequestDto dto);
}

public interface IPaymentService
{
    Task<Result<PaymentDto>> GetByIdAsync(int id);
    Task<Result<PaymentDto>> GetByRequestIdAsync(int requestId);
    Task<Result<PaymentResultDto>> ProcessAsync(ProcessPaymentDto dto);
    Task<Result<DriverEarningsReportDto>> GetDriverEarningsAsync(int driverId, DateTime? from, DateTime? to);
    Task<Result<CompanyRevenueReportDto>> GetCompanyRevenueAsync(DateTime? from, DateTime? to);
}

public interface IPricingService
{
    Task<Result<PricingDto>> GetByIdAsync(int id);
    Task<Result<PricingDto>> GetActiveAsync();
    Task<Result<IEnumerable<PricingDto>>> GetAllAsync();
    Task<Result<int>> CreateAsync(CreatePricingDto dto);
    Task<Result> ActivateAsync(int id);
    Task<Result> DeactivateAsync(int id);
    Task<Result<CostEstimateDto>> EstimateCostAsync(double pickupLat, double pickupLon, double dropLat, double dropLon);
}

public interface IDashboardService
{
    Task<Result<DashboardStatsDto>> GetStatsAsync();
}

public interface IJwtService
{
    string GenerateToken(User user);
    int? ValidateToken(string token);
}
