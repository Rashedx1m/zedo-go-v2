using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Repositories;

public class UserRepository : Repository<User, int>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _dbSet.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByPhoneAsync(string phone) =>
        await _dbSet.FirstOrDefaultAsync(u => u.Phone == phone);

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role) =>
        await _dbSet.Where(u => u.Role == role).ToListAsync();

    public async Task<bool> IsEmailExistsAsync(string email, int? excludeUserId = null)
    {
        var query = _dbSet.Where(u => u.Email == email);
        if (excludeUserId.HasValue) query = query.Where(u => u.Id != excludeUserId.Value);
        return await query.AnyAsync();
    }

    public async Task<bool> IsPhoneExistsAsync(string phone, int? excludeUserId = null)
    {
        var query = _dbSet.Where(u => u.Phone == phone);
        if (excludeUserId.HasValue) query = query.Where(u => u.Id != excludeUserId.Value);
        return await query.AnyAsync();
    }
}

public class CustomerRepository : Repository<Customer, int>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public async Task<Customer?> GetByUserIdAsync(int userId) =>
        await _dbSet.FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<Customer?> GetWithRequestsAsync(int customerId) =>
        await _dbSet.Include(c => c.Requests).FirstOrDefaultAsync(c => c.Id == customerId);
}

public class DriverRepository : Repository<Driver, int>, IDriverRepository
{
    public DriverRepository(AppDbContext context) : base(context) { }

    public async Task<Driver?> GetByUserIdAsync(int userId) =>
        await _dbSet.FirstOrDefaultAsync(d => d.UserId == userId);

    public async Task<Driver?> GetWithLocationAsync(int driverId) =>
        await _dbSet.Include(d => d.CurrentLocation).FirstOrDefaultAsync(d => d.Id == driverId);

    public async Task<IEnumerable<Driver>> GetAvailableDriversAsync() =>
        await _dbSet.Where(d => d.Status == DriverStatus.Online).ToListAsync();

    public async Task<IEnumerable<Driver>> GetNearbyDriversAsync(double lat, double lon, double radiusKm)
    {
        var availableDrivers = await _dbSet
            .Include(d => d.CurrentLocation)
            .Where(d => d.Status == DriverStatus.Online && d.CurrentLocation != null)
            .ToListAsync();

        return availableDrivers
            .Where(d => d.CurrentLocation!.CalculateDistanceTo(lat, lon) <= radiusKm)
            .OrderBy(d => d.CurrentLocation!.CalculateDistanceTo(lat, lon));
    }
}

public class DriverLocationRepository : Repository<DriverLocation, int>, IDriverLocationRepository
{
    public DriverLocationRepository(AppDbContext context) : base(context) { }

    public async Task<DriverLocation?> GetByDriverIdAsync(int driverId) =>
        await _dbSet.FirstOrDefaultAsync(l => l.DriverId == driverId);

    public async Task UpsertAsync(DriverLocation location)
    {
        var existing = await GetByDriverIdAsync(location.DriverId);
        if (existing != null)
            existing.UpdateLocation(location.Latitude, location.Longitude);
        else
            await _dbSet.AddAsync(location);
    }

    public async Task<IEnumerable<DriverLocation>> GetActiveLocationsAsync(int maxSecondsOld = 60)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-maxSecondsOld);
        return await _dbSet.Where(l => l.LastUpdate >= cutoff).ToListAsync();
    }

    public async Task<int> CleanupOldLocationsAsync(int maxDaysOld)
    {
        var cutoff = DateTime.UtcNow.AddDays(-maxDaysOld);
        var oldLocations = await _dbSet.Where(l => l.LastUpdate < cutoff).ToListAsync();
        _dbSet.RemoveRange(oldLocations);
        return oldLocations.Count;
    }
}

public class RequestRepository : Repository<Request, int>, IRequestRepository
{
    private static readonly RequestStatus[] ActiveStatuses =
    {
        RequestStatus.Pending,
        RequestStatus.Searching,
        RequestStatus.Accepted,
        RequestStatus.Arrived,
        RequestStatus.InProgress
    };

    public RequestRepository(AppDbContext context) : base(context) { }

    public async Task<Request?> GetWithDetailsAsync(int requestId) =>
        await _dbSet
            .Include(r => r.Customer)
            .Include(r => r.Driver)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == requestId);

    public async Task<IEnumerable<Request>> GetByCustomerIdAsync(int customerId) =>
        await _dbSet
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<Request>> GetByDriverIdAsync(int driverId) =>
        await _dbSet
            .Where(r => r.DriverId == driverId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status) =>
        await _dbSet
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task<Request?> GetActiveByCustomerAsync(int customerId) =>
        await _dbSet.FirstOrDefaultAsync(r =>
            r.CustomerId == customerId &&
            ActiveStatuses.Contains(r.Status));

    public async Task<Request?> GetActiveByDriverAsync(int driverId) =>
        await _dbSet.FirstOrDefaultAsync(r =>
            r.DriverId == driverId &&
            ActiveStatuses.Contains(r.Status));

    public async Task<IEnumerable<Request>> GetPendingRequestsAsync() =>
        await _dbSet
            .Where(r => r.Status == RequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<Request>> GetNearbyRequestsAsync(double lat, double lon, double radiusKm)
    {
        // جلب الطلبات Pending فقط
        var pendingRequests = await _dbSet
            .Where(r => r.Status == RequestStatus.Pending)
            .AsNoTracking()
            .ToListAsync();

        // الفلترة بالمسافة في الذاكرة
        return pendingRequests
            .Where(r => GeoHelper.CalculateDistance(lat, lon, r.PickupLatitude, r.PickupLongitude) <= radiusKm)
            .OrderBy(r => GeoHelper.CalculateDistance(lat, lon, r.PickupLatitude, r.PickupLongitude));
    }

    public async Task<IEnumerable<Request>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _dbSet
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

}


public class PaymentRepository : Repository<Payment, int>, IPaymentRepository
{
    public PaymentRepository(AppDbContext context) : base(context) { }

    public async Task<Payment?> GetByRequestIdAsync(int requestId) =>
        await _dbSet.FirstOrDefaultAsync(p => p.RequestId == requestId);

    public async Task<IEnumerable<Payment>> GetByDriverIdAsync(int driverId, DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet.Where(p => p.DriverId == driverId);
        if (from.HasValue) query = query.Where(p => p.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(p => p.CreatedAt <= to.Value);
        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByCustomerIdAsync(int customerId) =>
        await _dbSet.Where(p => p.CustomerId == customerId).OrderByDescending(p => p.CreatedAt).ToListAsync();

    public async Task<decimal> GetDriverTotalEarningsAsync(int driverId, DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet.Where(p => p.DriverId == driverId && p.Status == PaymentStatus.Completed);
        if (from.HasValue) query = query.Where(p => p.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(p => p.CreatedAt <= to.Value);
        return await query.SumAsync(p => p.DriverEarning);
    }

    public async Task<decimal> GetCompanyTotalCommissionAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet.Where(p => p.Status == PaymentStatus.Completed);
        if (from.HasValue) query = query.Where(p => p.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(p => p.CreatedAt <= to.Value);
        return await query.SumAsync(p => p.CompanyCommission);
    }
}

public class RidePricingRepository : Repository<RidePricing, int>, IRidePricingRepository
{
    public RidePricingRepository(AppDbContext context) : base(context) { }

    public async Task<RidePricing?> GetActiveAsync() =>
        await _dbSet.FirstOrDefaultAsync(p => p.IsActive);

    public async Task DeactivateAllAsync()
    {
        var activePricings = await _dbSet.Where(p => p.IsActive).ToListAsync();
        foreach (var pricing in activePricings)
            pricing.Deactivate();
    }
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expireDays = int.Parse(_configuration["Jwt:ExpireDays"] ?? "7");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expireDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "UserId").Value);

            return userId;
        }
        catch
        {
            return null;
        }
    }
}