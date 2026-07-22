using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

public interface IRepository<T, TId> where T : BaseEntity<TId>
{
    Task<T?> GetByIdAsync(TId id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<bool> ExistsAsync(TId id);
}

public interface IUserRepository : IRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phone);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
    Task<bool> IsEmailExistsAsync(string email, int? excludeUserId = null);
    Task<bool> IsPhoneExistsAsync(string phone, int? excludeUserId = null);
}

public interface ICustomerRepository : IRepository<Customer, int>
{
    Task<Customer?> GetByUserIdAsync(int userId);
    Task<Customer?> GetWithRequestsAsync(int customerId);
}

public interface IDriverRepository : IRepository<Driver, int>
{
    Task<Driver?> GetByUserIdAsync(int userId);
    Task<Driver?> GetWithLocationAsync(int driverId);
    Task<IEnumerable<Driver>> GetAvailableDriversAsync();
    Task<IEnumerable<Driver>> GetNearbyDriversAsync(double lat, double lon, double radiusKm);
}

public interface IDriverLocationRepository : IRepository<DriverLocation, int>
{
    Task<DriverLocation?> GetByDriverIdAsync(int driverId);
    Task UpsertAsync(DriverLocation location);
    Task<IEnumerable<DriverLocation>> GetActiveLocationsAsync(int maxSecondsOld = 60);
    Task<int> CleanupOldLocationsAsync(int maxDaysOld);
}

public interface IRequestRepository : IRepository<Request, int>
    {
        // Details
        Task<Request?> GetWithDetailsAsync(int requestId);
        // تشمل: Customer + Driver + Payment

        // By Relations
        Task<IEnumerable<Request>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Request>> GetByDriverIdAsync(int driverId);

        // By Status
        Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status);
        Task<IEnumerable<Request>> GetPendingRequestsAsync();

        // Active
        Task<Request?> GetActiveByCustomerAsync(int customerId);
        Task<Request?> GetActiveByDriverAsync(int driverId);

        // Geo Queries
        Task<IEnumerable<Request>> GetNearbyRequestsAsync(
            double lat,
            double lon,
            double radiusKm);

        // Optional – لو أردت تقارير
        Task<IEnumerable<Request>> GetByDateRangeAsync(
            DateTime from,
            DateTime to);
    }

public interface IPaymentRepository : IRepository<Payment, int>
{
    Task<Payment?> GetByRequestIdAsync(int requestId);
    Task<IEnumerable<Payment>> GetByDriverIdAsync(int driverId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<Payment>> GetByCustomerIdAsync(int customerId);
    Task<decimal> GetDriverTotalEarningsAsync(int driverId, DateTime? from = null, DateTime? to = null);
    Task<decimal> GetCompanyTotalCommissionAsync(DateTime? from = null, DateTime? to = null);
}

public interface IRidePricingRepository : IRepository<RidePricing, int>
{
    Task<RidePricing?> GetActiveAsync();
    Task DeactivateAllAsync();
}

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ICustomerRepository Customers { get; }
    IDriverRepository Drivers { get; }
    IDriverLocationRepository DriverLocations { get; }
    IRequestRepository Requests { get; }
    IPaymentRepository Payments { get; }
    IRidePricingRepository RidePricings { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
