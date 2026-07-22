using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentDto>> GetByIdAsync(int id)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(id);
        if (payment == null)
            return Result.Failure<PaymentDto>("الدفعة غير موجودة", "NOT_FOUND");

        return Result.Success(payment.ToDto());
    }

    public async Task<Result<PaymentDto>> GetByRequestIdAsync(int requestId)
    {
        var payment = await _unitOfWork.Payments.GetByRequestIdAsync(requestId);
        if (payment == null)
            return Result.Failure<PaymentDto>("الدفعة غير موجودة", "NOT_FOUND");

        return Result.Success(payment.ToDto());
    }

    public async Task<Result<PaymentResultDto>> ProcessAsync(ProcessPaymentDto dto)
    {
        // التحقق من الطلب
        var request = await _unitOfWork.Requests.GetWithDetailsAsync(dto.RequestId);
        if (request == null)
            return Result.Failure<PaymentResultDto>("الطلب غير موجود", "REQUEST_NOT_FOUND");

        // التحقق من أن الطلب مكتمل
        if (request.Status != RequestStatus.Completed)
            return Result.Failure<PaymentResultDto>("الطلب غير مكتمل", "REQUEST_NOT_COMPLETED");

        // التحقق من عدم وجود دفعة مسبقة
        var existingPayment = await _unitOfWork.Payments.GetByRequestIdAsync(dto.RequestId);
        if (existingPayment != null)
            return Result.Failure<PaymentResultDto>("تم معالجة الدفعة مسبقاً", "ALREADY_PAID");

        // التحقق من وجود سائق
        if (!request.DriverId.HasValue)
            return Result.Failure<PaymentResultDto>("لا يوجد سائق للطلب", "NO_DRIVER");

        // إنشاء الدفعة
        var amount = request.ActualCost ?? request.EstimatedCost ?? 0;
        var payment = Payment.Create(
            dto.RequestId,
            request.CustomerId,
            request.DriverId.Value,
            amount,
            dto.Method
        );

        // معالجة الدفعة حسب طريقة الدفع
        switch (dto.Method)
        {
            case PaymentMethod.Cash:
                payment.MarkCompleted("CASH_" + Guid.NewGuid().ToString("N")[..8].ToUpper());
                break;

            case PaymentMethod.Wallet:
                // هنا يمكن إضافة منطق خصم من المحفظة
                payment.MarkCompleted("WALLET_" + Guid.NewGuid().ToString("N")[..8].ToUpper());
                break;

            case PaymentMethod.Card:
            case PaymentMethod.Online:
                // هنا يمكن إضافة تكامل مع بوابة الدفع
                payment.MarkCompleted("ONLINE_" + Guid.NewGuid().ToString("N")[..8].ToUpper());
                break;
        }

        await _unitOfWork.Payments.AddAsync(payment);

        // تحديث أرباح السائق
        var driver = await _unitOfWork.Drivers.GetByIdAsync(request.DriverId.Value);
        if (driver != null)
        {
            driver.EndRide(payment.DriverEarning);
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Success(payment.ToResultDto());
    }

    public async Task<Result<DriverEarningsReportDto>> GetDriverEarningsAsync(int driverId, DateTime? from, DateTime? to)
    {
        // التحقق من وجود السائق
        var driver = await _unitOfWork.Drivers.GetByIdAsync(driverId);
        if (driver == null)
            return Result.Failure<DriverEarningsReportDto>("السائق غير موجود", "NOT_FOUND");

        var user = await _unitOfWork.Users.GetByIdAsync(driver.UserId);

        // جلب المدفوعات
        var payments = await _unitOfWork.Payments.GetByDriverIdAsync(driverId, from, to);
        var completedPayments = payments.Where(p => p.Status == PaymentStatus.Completed).ToList();

        var totalAmount = completedPayments.Sum(p => p.Amount);
        var totalCommission = completedPayments.Sum(p => p.CompanyCommission);
        var totalEarnings = completedPayments.Sum(p => p.DriverEarning);
        var totalRides = completedPayments.Count;
        var avgPerRide = totalRides > 0 ? totalEarnings / totalRides : 0;

        var report = new DriverEarningsReportDto(
            driverId,
            user!.FullName,
            totalRides,
            totalAmount,
            totalCommission,
            totalEarnings,
            avgPerRide,
            from,
            to
        );

        return Result.Success(report);
    }

    public async Task<Result<CompanyRevenueReportDto>> GetCompanyRevenueAsync(DateTime? from, DateTime? to)
    {
        // جلب جميع المدفوعات المكتملة
        var allPayments = await _unitOfWork.Payments.GetAllAsync();

        var payments = allPayments
            .Where(p => p.Status == PaymentStatus.Completed)
            .Where(p => !from.HasValue || p.CreatedAt >= from.Value)
            .Where(p => !to.HasValue || p.CreatedAt <= to.Value)
            .ToList();

        var totalTransactions = payments.Count;
        var totalRevenue = payments.Sum(p => p.Amount);
        var totalCommission = payments.Sum(p => p.CompanyCommission);
        var totalDriverPayouts = payments.Sum(p => p.DriverEarning);
        var avgTransactionAmount = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

        var report = new CompanyRevenueReportDto(
            totalTransactions,
            totalRevenue,
            totalCommission,
            totalDriverPayouts,
            avgTransactionAmount,
            from,
            to
        );

        return Result.Success(report);
    }
}