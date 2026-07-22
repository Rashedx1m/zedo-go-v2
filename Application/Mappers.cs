using Application.DTOs;
using Domain.Entities;

namespace Application;

public static class Mappers
{
    public static UserDto ToDto(this User user) => new(
        user.Id, user.FullName, user.Email, user.Phone, user.Role.ToString(),
        user.IsActive, user.CreatedAt, user.LastLoginAt);

    public static CustomerDto ToDto(this Customer customer, User user) => new(
        customer.Id, customer.UserId, user.FullName, user.Email, user.Phone,
        customer.Rating, customer.TotalRides, customer.CreatedAt);

    public static DriverDto ToDto(this Driver driver, User user) => new(
        driver.Id, driver.UserId, user.FullName, user.Phone, driver.CarModel,
        driver.CarColor, driver.PlateNumber, driver.Status.ToString(),
        driver.Rating, driver.TotalRides, driver.TotalEarnings, driver.CreatedAt);

    public static DriverLocationDto ToDto(this DriverLocation location) => new(
        location.DriverId, location.Latitude, location.Longitude,
        location.LastUpdate, location.IsLocationFresh());

    public static RequestDto ToDto(this Request request) => new(
        request.Id, request.CustomerId, request.DriverId,
        request.PickupLatitude, request.PickupLongitude,
        request.DropLatitude, request.DropLongitude, request.Status.ToString(),
        request.EstimatedCost, request.ActualCost, request.DistanceKm,
        request.DurationMinutes, request.CreatedAt, request.AcceptedAt, request.CompletedAt);

    public static PaymentDto ToDto(this Payment payment) => new(
        payment.Id, payment.RequestId, payment.Amount, payment.CompanyCommission,
        payment.DriverEarning, payment.Method.ToString(), payment.Status.ToString(), payment.CreatedAt);

    public static PaymentResultDto ToResultDto(this Payment payment) => new(
        payment.Id, payment.Amount, payment.CompanyCommission,
        payment.DriverEarning, payment.Method.ToString(), payment.CreatedAt);

    public static PricingDto ToDto(this RidePricing pricing) => new(
        pricing.Id, pricing.Name, pricing.BaseFare, pricing.PricePerKm,
        pricing.PricePerMinute, pricing.MinimumFare, pricing.IsActive, pricing.CreatedAt);

    public static CostEstimateDto ToCostEstimateDto(this RideCostBreakdown breakdown) => new(
        breakdown.BaseFare, breakdown.DistanceKm, breakdown.DistanceCost,
        breakdown.DurationMinutes, breakdown.TimeCost, breakdown.Subtotal,
        breakdown.MinimumFare, breakdown.Total);
}
