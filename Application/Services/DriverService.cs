using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class DriverService : IDriverService
{
    private readonly IUnitOfWork _unitOfWork;

    public DriverService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriverDto>> GetByIdAsync(int id)
    {
        var driver = await _unitOfWork.Drivers.GetByIdAsync(id);
        if (driver == null)
            return Result.Failure<DriverDto>("السائق غير موجود", "NOT_FOUND");

        var user = await _unitOfWork.Users.GetByIdAsync(driver.UserId);
        return Result.Success(driver.ToDto(user!));
    }

    public async Task<Result<DriverDto>> GetByUserIdAsync(int userId)
    {
        var driver = await _unitOfWork.Drivers.GetByUserIdAsync(userId);
        if (driver == null)
            return Result.Failure<DriverDto>("السائق غير موجود", "NOT_FOUND");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return Result.Success(driver.ToDto(user!));
    }

    public async Task<Result<IEnumerable<DriverDto>>> GetAllAsync()
    {
        var drivers = await _unitOfWork.Drivers.GetAllAsync();
        var driverDtos = new List<DriverDto>();

        foreach (var driver in drivers)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(driver.UserId);
            driverDtos.Add(driver.ToDto(user!));
        }

        return Result.Success<IEnumerable<DriverDto>>(driverDtos);
    }

    public async Task<Result<IEnumerable<DriverDto>>> GetAvailableAsync()
    {
        var drivers = await _unitOfWork.Drivers.GetAvailableDriversAsync();
        var driverDtos = new List<DriverDto>();

        foreach (var driver in drivers)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(driver.UserId);
            driverDtos.Add(driver.ToDto(user!));
        }

        return Result.Success<IEnumerable<DriverDto>>(driverDtos);
    }

    public async Task<Result<int>> RegisterAsync(RegisterDriverDto dto)
    {
        // التحقق من وجود المستخدم
        var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
        if (user == null)
            return Result.Failure<int>("المستخدم غير موجود", "USER_NOT_FOUND");

        // التحقق من أن المستخدم ليس سائقاً بالفعل
        var existingDriver = await _unitOfWork.Drivers.GetByUserIdAsync(dto.UserId);
        if (existingDriver != null)
            return Result.Failure<int>("المستخدم مسجل كسائق بالفعل", "ALREADY_DRIVER");

        var driver = Driver.Create(
            dto.UserId,
            dto.CarModel,
            dto.CarColor,
            dto.PlateNumber,
            dto.LicenseNumber
        );

        await _unitOfWork.Drivers.AddAsync(driver);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(driver.Id);
    }

    public async Task<Result> UpdateAsync(int id, UpdateDriverDto dto)
    {
        var driver = await _unitOfWork.Drivers.GetByIdAsync(id);
        if (driver == null)
            return Result.Failure("السائق غير موجود", "NOT_FOUND");

        driver.UpdateCarInfo(dto.CarModel, dto.CarColor, dto.PlateNumber);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> GoOnlineAsync(int driverId)
    {
        var driver = await _unitOfWork.Drivers.GetByIdAsync(driverId);
        if (driver == null)
            return Result.Failure("السائق غير موجود", "NOT_FOUND");

        try
        {
            driver.GoOnline();
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "INVALID_OPERATION");
        }
    }

    public async Task<Result> GoOfflineAsync(int driverId)
    {
        var driver = await _unitOfWork.Drivers.GetByIdAsync(driverId);
        if (driver == null)
            return Result.Failure("السائق غير موجود", "NOT_FOUND");

        try
        {
            driver.GoOffline();
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "INVALID_OPERATION");
        }
    }

    public async Task<Result> UpdateLocationAsync(int driverId, UpdateLocationDto dto)
    {
        var driver = await _unitOfWork.Drivers.GetByIdAsync(driverId);
        if (driver == null)
            return Result.Failure("السائق غير موجود", "NOT_FOUND");

        if (!GeoHelper.IsValidCoordinates(dto.Latitude, dto.Longitude))
            return Result.Failure("الإحداثيات غير صالحة", "INVALID_COORDINATES");

        var location = await _unitOfWork.DriverLocations.GetByDriverIdAsync(driverId);

        if (location == null)
        {
            location = DriverLocation.Create(driverId, dto.Latitude, dto.Longitude);
            await _unitOfWork.DriverLocations.AddAsync(location);
        }
        else
        {
            location.UpdateLocation(dto.Latitude, dto.Longitude);
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<DriverLocationDto>> GetLocationAsync(int driverId)
    {
        var driver = await _unitOfWork.Drivers.GetByIdAsync(driverId);
        if (driver == null)
            return Result.Failure<DriverLocationDto>("السائق غير موجود", "NOT_FOUND");

        var location = await _unitOfWork.DriverLocations.GetByDriverIdAsync(driverId);
        if (location == null)
            return Result.Failure<DriverLocationDto>("موقع السائق غير متاح", "LOCATION_NOT_FOUND");

        return Result.Success(location.ToDto());
    }

    public async Task<Result<IEnumerable<NearbyDriverDto>>> GetNearbyAsync(double lat, double lon, double radiusKm = 5)
    {
        if (!GeoHelper.IsValidCoordinates(lat, lon))
            return Result.Failure<IEnumerable<NearbyDriverDto>>("الإحداثيات غير صالحة", "INVALID_COORDINATES");

        var nearbyDrivers = await _unitOfWork.Drivers.GetNearbyDriversAsync(lat, lon, radiusKm);
        var nearbyDriverDtos = new List<NearbyDriverDto>();

        foreach (var driver in nearbyDrivers)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(driver.UserId);
            var location = await _unitOfWork.DriverLocations.GetByDriverIdAsync(driver.Id);

            if (location != null)
            {
                var distance = location.CalculateDistanceTo(lat, lon);
                var eta = GeoHelper.CalculateETA(distance);

                nearbyDriverDtos.Add(new NearbyDriverDto(
                    driver.Id,
                    user!.FullName,
                    driver.CarModel,
                    driver.CarColor,
                    driver.PlateNumber,
                    Math.Round(distance, 2),
                    (int)eta.TotalMinutes
                ));
            }
        }

        return Result.Success<IEnumerable<NearbyDriverDto>>(nearbyDriverDtos);
    }
}