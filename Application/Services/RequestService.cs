using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services;

public class RequestService : IRequestService
{
    private readonly IUnitOfWork _unitOfWork;

    public RequestService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RequestDto>> GetByIdAsync(int id)
    {
        var request = await _unitOfWork.Requests.GetByIdAsync(id);
        if (request == null)
            return Result.Failure<RequestDto>("الطلب غير موجود", "NOT_FOUND");
        return Result.Success(request.ToDto());
    }

    public async Task<Result<RequestFullDto>> GetFullInfoAsync(int id)
    {

        var request = await _unitOfWork.Requests.GetWithDetailsAsync(id);
        if (request == null)
            return Result.Failure<RequestFullDto>("الطلب غير موجود", "NOT_FOUND");

        var customerUser = await _unitOfWork.Users.GetByIdAsync(request.Customer.UserId);
        Driver? driver = null;
        User? driverUser = null;
        DriverLocation? driverLocation = null;

        if (request.DriverId.HasValue)
        {
            driver = await _unitOfWork.Drivers.GetByIdAsync(request.DriverId.Value);
            if (driver != null)
            {
                driverUser = await _unitOfWork.Users.GetByIdAsync(driver.UserId);
                driverLocation = await _unitOfWork.DriverLocations.GetByDriverIdAsync(driver.Id);
            }
        }

        var fullDto = new RequestFullDto(
            request.Id, request.Status.ToString(),
            request.PickupLatitude, request.PickupLongitude,
            request.DropLatitude, request.DropLongitude,
            request.EstimatedCost, request.ActualCost,
            request.DistanceKm, request.DurationMinutes, request.CreatedAt,
            request.CustomerId, customerUser!.FullName, customerUser.Phone,
            driver?.Id, driverUser?.FullName, driverUser?.Phone,
            driver?.CarModel, driver?.CarColor, driver?.PlateNumber,
            driverLocation?.Latitude, driverLocation?.Longitude);

        return Result.Success(fullDto);
    }

    public async Task<Result<IEnumerable<RequestDto>>> GetByCustomerAsync(int customerId)
    {
        var requests = await _unitOfWork.Requests.GetByCustomerIdAsync(customerId);
        return Result.Success(requests.Select(r => r.ToDto()));
    }

    public async Task<Result<IEnumerable<RequestDto>>> GetByDriverAsync(int driverId)
    {
        var requests = await _unitOfWork.Requests.GetByDriverIdAsync(driverId);
        return Result.Success(requests.Select(r => r.ToDto()));
    }

    public async Task<Result<IEnumerable<RequestDto>>> GetByStatusAsync(RequestStatus status)
    {
        var requests = await _unitOfWork.Requests.GetByStatusAsync(status);
        return Result.Success(requests.Select(r => r.ToDto()));
    }

    public async Task<Result<RequestDto>> GetActiveByCustomerAsync(int customerId)
    {
        var request = await _unitOfWork.Requests.GetActiveByCustomerAsync(customerId);
        if (request == null)
            return Result.Failure<RequestDto>("لا يوجد طلب نشط", "NO_ACTIVE_REQUEST");
        return Result.Success(request.ToDto());
    }

    public async Task<Result<RequestDto>> GetActiveByDriverAsync(int driverId)
    {
        var request = await _unitOfWork.Requests.GetActiveByDriverAsync(driverId);
        if (request == null)
            return Result.Failure<RequestDto>("لا يوجد طلب نشط", "NO_ACTIVE_REQUEST");
        return Result.Success(request.ToDto());
    }

    public async Task<Result<int>> CreateAsync(int customerId, CreateRequestDto dto)
    {
        // 1️⃣ جلب العميل الحقيقي
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
        if (customer == null)
            return Result.Failure<int>("العميل غير موجود", "CUSTOMER_NOT_FOUND");

        // 2️⃣ تحقق من الطلب النشط
        var activeRequest = await _unitOfWork.Requests.GetActiveByCustomerAsync(customer.Id);
        if (activeRequest != null)
            return Result.Failure<int>("لديك طلب نشط بالفعل", "ACTIVE_REQUEST_EXISTS");

        // 3️⃣ التسعير
        var pricing = await _unitOfWork.RidePricings.GetActiveAsync();
        decimal? estimatedCost = null;

        if (pricing != null)
        {
            var distance = GeoHelper.CalculateDistance(
                dto.PickupLatitude, dto.PickupLongitude,
                dto.DropLatitude, dto.DropLongitude);

            var eta = GeoHelper.CalculateETA(distance);
            estimatedCost = pricing.CalculateCost(distance, (int)eta.TotalMinutes);
        }

        // 4️⃣ أنشئ الطلب باستخدام الكيان نفسه
        var request = Request.Create(customer.Id,
            dto.PickupLatitude, dto.PickupLongitude,
            dto.DropLatitude, dto.DropLongitude, estimatedCost);

        // 5️⃣ الحفظ
        await _unitOfWork.Requests.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(request.Id);
    }


    public async Task<Result> AcceptAsync(int requestId, int driverId)
    {
        var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
        if (request == null)
            return Result.Failure("الطلب غير موجود", "NOT_FOUND");

        var driver = await _unitOfWork.Drivers.GetByIdAsync(driverId);
        if (driver == null)
            return Result.Failure("السائق غير موجود", "DRIVER_NOT_FOUND");

        if (!driver.IsAvailable)
            return Result.Failure("السائق غير متاح", "DRIVER_NOT_AVAILABLE");

        try
        {
            request.AssignDriver(driverId);
            driver.StartRide();
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "INVALID_OPERATION");
        }
    }

    public async Task<Result> DriverArrivedAsync(int requestId)
    {
        var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
        if (request == null)
            return Result.Failure("الطلب غير موجود", "NOT_FOUND");

        try
        {
            request.DriverArrived();
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "INVALID_OPERATION");
        }
    }

    public async Task<Result> StartRideAsync(int requestId)
    {
        var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
        if (request == null)
            return Result.Failure("الطلب غير موجود", "NOT_FOUND");

        try
        {
            request.StartRide();
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "INVALID_OPERATION");
        }
    }

    public async Task<Result<PaymentResultDto>> CompleteAsync(int requestId, PaymentMethod paymentMethod)
    {
        var request = await _unitOfWork.Requests.GetWithDetailsAsync(requestId);
        if (request == null)
            return Result.Failure<PaymentResultDto>("الطلب غير موجود", "NOT_FOUND");

        if (!request.DriverId.HasValue)
            return Result.Failure<PaymentResultDto>("لا يوجد سائق معين للطلب", "NO_DRIVER");

        var pricing = await _unitOfWork.RidePricings.GetActiveAsync();
        if (pricing == null)
            return Result.Failure<PaymentResultDto>("لا توجد تسعيرة نشطة", "NO_PRICING");

        var duration = (int)(DateTime.UtcNow - (request.StartedAt ?? request.CreatedAt)).TotalMinutes;
        var actualCost = pricing.CalculateCost(request.DistanceKm ?? 0, duration);

        try
        {
            request.Complete(actualCost, duration);

            var payment = Payment.Create(requestId, request.CustomerId,
                request.DriverId.Value, actualCost, paymentMethod);
            
            payment.MarkCompleted();
            await _unitOfWork.Payments.AddAsync(payment);

            var driver = await _unitOfWork.Drivers.GetByIdAsync(request.DriverId.Value);
            driver?.EndRide(payment.DriverEarning);

            request.Customer.IncrementRides();
            await _unitOfWork.SaveChangesAsync();

            return Result.Success(payment.ToResultDto());
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<PaymentResultDto>(ex.Message, "INVALID_OPERATION");
        }
    }

    public async Task<Result> CancelAsync(int requestId, string canceledBy, string? reason = null)
    {
        var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
        if (request == null)
            return Result.Failure("الطلب غير موجود", "NOT_FOUND");

        try
        {
            if (request.DriverId.HasValue)
            {
                var driver = await _unitOfWork.Drivers.GetByIdAsync(request.DriverId.Value);
                driver?.GoOnline();
            }

            request.Cancel(canceledBy, reason);
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "INVALID_OPERATION");
        }
    }

    public async Task<Result<IEnumerable<NearbyRequestDto>>> GetNearbyForDriverAsync(int driverId, double radiusKm = 10)
    {
        var location = await _unitOfWork.DriverLocations.GetByDriverIdAsync(driverId);
        if (location == null)
            return Result.Failure<IEnumerable<NearbyRequestDto>>("موقع السائق غير متاح", "LOCATION_NOT_FOUND");

        var requests = await _unitOfWork.Requests.GetNearbyRequestsAsync(
            location.Latitude, location.Longitude, radiusKm);

        var nearbyRequests = requests.Select(r => new NearbyRequestDto(
            r.Id, r.PickupLatitude, r.PickupLongitude,
            r.DropLatitude, r.DropLongitude,
            location.CalculateDistanceTo(r.PickupLatitude, r.PickupLongitude),
            r.EstimatedCost, r.CreatedAt,
            (int)(DateTime.UtcNow - r.CreatedAt).TotalMinutes));

        return Result.Success(nearbyRequests);
    }

    public async Task<Result<int>> CreateByUserIdAsync(int userId, CreateRequestDto dto)
    {
        // جلب الـ Customer من الـ UserId
        var customer = await _unitOfWork.Customers.GetByUserIdAsync(userId);

        if (customer == null)
            return Result.Failure<int>("لم يتم العثور على حساب العميل", "CUSTOMER_NOT_FOUND");

        return await CreateAsync(customer.Id, dto);
    }
}
