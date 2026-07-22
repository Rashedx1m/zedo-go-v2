using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);

        if (user == null)
            return Result.Failure<LoginResultDto>("البريد الإلكتروني غير مسجل", "USER_NOT_FOUND");

        if (!user.IsActive)
            return Result.Failure<LoginResultDto>("الحساب معطل", "ACCOUNT_DISABLED");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Result.Failure<LoginResultDto>("كلمة المرور غير صحيحة", "INVALID_PASSWORD");

        user.UpdateLastLogin();
        await _unitOfWork.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        // ✅ جلب الـ customerId إذا كان المستخدم عميل
        int? customerId = null;
        int? driverId = null;

        if (user.Role == UserRole.Customer)
        {
            var customer = await _unitOfWork.Customers.GetByUserIdAsync(user.Id);
            customerId = customer?.Id;
        }
        else if (user.Role == UserRole.Driver)
        {
            var driver = await _unitOfWork.Drivers.GetByUserIdAsync(user.Id);
            driverId = driver?.Id;
        }

        var result = new LoginResultDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            token,
            DateTime.UtcNow.AddDays(7),
            customerId,   // ✅ جديد
            driverId      // ✅ جديد
        );

        return Result.Success(result);
    }

    public async Task<Result<int>> RegisterAsync(RegisterUserDto dto)
    {
        if (await _unitOfWork.Users.IsEmailExistsAsync(dto.Email))
            return Result.Failure<int>("البريد الإلكتروني مسجل مسبقاً", "EMAIL_EXISTS");

        if (await _unitOfWork.Users.IsPhoneExistsAsync(dto.Phone))
            return Result.Failure<int>("رقم الهاتف مسجل مسبقاً", "PHONE_EXISTS");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 12);
        var user = User.Create(dto.FullName, dto.Email, dto.Phone, passwordHash, dto.Role);
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        if (dto.Role == UserRole.Customer)
        {
            var customer = Customer.Create(user.Id);
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
        }

        return Result.Success(user.Id);
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user == null)
            return Result.Failure("المستخدم غير موجود", "USER_NOT_FOUND");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return Result.Failure("كلمة المرور الحالية غير صحيحة", "INVALID_PASSWORD");

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, 12);
        user.ChangePassword(newPasswordHash);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ValidateTokenAsync(string token)
    {
        var userId = _jwtService.ValidateToken(token);

        if (!userId.HasValue)
            return Result.Failure("التوكن غير صالح", "INVALID_TOKEN");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);

        if (user == null || !user.IsActive)
            return Result.Failure("المستخدم غير موجود أو معطل", "USER_INVALID");

        return Result.Success();
    }
}