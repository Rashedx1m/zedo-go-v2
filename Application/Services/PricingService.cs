using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class PricingService : IPricingService
{
    private readonly IUnitOfWork _unitOfWork;

    public PricingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PricingDto>> GetByIdAsync(int id)
    {
        var pricing = await _unitOfWork.RidePricings.GetByIdAsync(id);
        if (pricing == null)
            return Result.Failure<PricingDto>("التسعيرة غير موجودة", "NOT_FOUND");

        return Result.Success(pricing.ToDto());
    }

    public async Task<Result<PricingDto>> GetActiveAsync()
    {
        var pricing = await _unitOfWork.RidePricings.GetActiveAsync();
        if (pricing == null)
            return Result.Failure<PricingDto>("لا توجد تسعيرة نشطة", "NO_ACTIVE_PRICING");

        return Result.Success(pricing.ToDto());
    }

    public async Task<Result<IEnumerable<PricingDto>>> GetAllAsync()
    {
        var pricings = await _unitOfWork.RidePricings.GetAllAsync();
        return Result.Success(pricings.Select(p => p.ToDto()));
    }

    public async Task<Result<int>> CreateAsync(CreatePricingDto dto)
    {
        try
        {
            var pricing = RidePricing.Create(
                dto.Name,
                dto.BaseFare,
                dto.PricePerKm,
                dto.PricePerMinute,
                dto.MinimumFare
            );

            await _unitOfWork.RidePricings.AddAsync(pricing);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success(pricing.Id);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<int>(ex.Message, "INVALID_DATA");
        }
    }

    public async Task<Result> ActivateAsync(int id)
    {
        var pricing = await _unitOfWork.RidePricings.GetByIdAsync(id);
        if (pricing == null)
            return Result.Failure("التسعيرة غير موجودة", "NOT_FOUND");

        // إلغاء تفعيل جميع التسعيرات الأخرى
        await _unitOfWork.RidePricings.DeactivateAllAsync();

        // تفعيل التسعيرة المطلوبة
        pricing.Activate();
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        var pricing = await _unitOfWork.RidePricings.GetByIdAsync(id);
        if (pricing == null)
            return Result.Failure("التسعيرة غير موجودة", "NOT_FOUND");

        if (!pricing.IsActive)
            return Result.Failure("التسعيرة غير مفعّلة أصلاً", "ALREADY_INACTIVE");

        pricing.Deactivate();
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<CostEstimateDto>> EstimateCostAsync(
        double pickupLat, double pickupLon,
        double dropLat, double dropLon)
    {
        // التحقق من الإحداثيات
        if (!GeoHelper.IsValidCoordinates(pickupLat, pickupLon))
            return Result.Failure<CostEstimateDto>("إحداثيات نقطة الانطلاق غير صالحة", "INVALID_PICKUP");

        if (!GeoHelper.IsValidCoordinates(dropLat, dropLon))
            return Result.Failure<CostEstimateDto>("إحداثيات نقطة الوصول غير صالحة", "INVALID_DROP");

        // جلب التسعيرة النشطة
        var pricing = await _unitOfWork.RidePricings.GetActiveAsync();
        if (pricing == null)
            return Result.Failure<CostEstimateDto>("لا توجد تسعيرة نشطة", "NO_ACTIVE_PRICING");

        // حساب المسافة والوقت المتوقع
        var distanceKm = GeoHelper.CalculateDistance(pickupLat, pickupLon, dropLat, dropLon);
        var eta = GeoHelper.CalculateETA(distanceKm);
        var estimatedMinutes = (int)eta.TotalMinutes;

        // حساب التكلفة
        var breakdown = pricing.GetCostBreakdown(distanceKm, estimatedMinutes);

        return Result.Success(breakdown.ToCostEstimateDto());
    }
}