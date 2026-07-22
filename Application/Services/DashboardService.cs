using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DashboardStatsDto>> GetStatsAsync()
    {
        // إجمالي المستخدمين
        var allUsers = await _unitOfWork.Users.GetAllAsync();
        var totalUsers = allUsers.Count();

        // إجمالي السائقين
        var allDrivers = await _unitOfWork.Drivers.GetAllAsync();
        var totalDrivers = allDrivers.Count();

        // السائقين النشطين (Online أو InRide)
        var activeDrivers = allDrivers.Count(d =>
            d.Status == DriverStatus.Online || d.Status == DriverStatus.InRide);

        // إجمالي العملاء
        var allCustomers = await _unitOfWork.Customers.GetAllAsync();
        var totalCustomers = allCustomers.Count();

        // الطلبات المعلقة
        var pendingRequests = await _unitOfWork.Requests.GetByStatusAsync(RequestStatus.Pending);
        var pendingCount = pendingRequests.Count();

        // رحلات اليوم المكتملة
        var today = DateTime.UtcNow.Date;
        var allRequests = await _unitOfWork.Requests.GetAllAsync();
        var todayCompletedRides = allRequests.Count(r =>
            r.Status == RequestStatus.Completed &&
            r.CompletedAt.HasValue &&
            r.CompletedAt.Value.Date == today);

        // إيرادات اليوم
        var allPayments = await _unitOfWork.Payments.GetAllAsync();
        var todayRevenue = allPayments
            .Where(p => p.Status == PaymentStatus.Completed && p.CreatedAt.Date == today)
            .Sum(p => p.Amount);

        var stats = new DashboardStatsDto(
            totalUsers,
            totalDrivers,
            totalCustomers,
            activeDrivers,
            pendingCount,
            todayCompletedRides,
            todayRevenue
        );

        return Result.Success(stats);
    }
}