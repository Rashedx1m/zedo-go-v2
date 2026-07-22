using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserDto>> GetByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return Result.Failure<UserDto>("المستخدم غير موجود", "NOT_FOUND");

        return Result.Success(user.ToDto());
    }

    public async Task<Result<UserDto>> GetByEmailAsync(string email)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email);
        if (user == null)
            return Result.Failure<UserDto>("المستخدم غير موجود", "NOT_FOUND");

        return Result.Success(user.ToDto());
    }

    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return Result.Success(users.Select(u => u.ToDto()));
    }

    public async Task<Result<IEnumerable<UserDto>>> GetByRoleAsync(UserRole role)
    {
        var users = await _unitOfWork.Users.GetByRoleAsync(role);
        return Result.Success(users.Select(u => u.ToDto()));
    }

    public async Task<Result> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return Result.Failure("المستخدم غير موجود", "NOT_FOUND");

        user.UpdateProfile(dto.FullName, dto.Phone);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ActivateAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return Result.Failure("المستخدم غير موجود", "NOT_FOUND");

        if (user.IsActive)
            return Result.Failure("المستخدم مفعّل بالفعل", "ALREADY_ACTIVE");

        user.Activate();
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return Result.Failure("المستخدم غير موجود", "NOT_FOUND");

        if (!user.IsActive)
            return Result.Failure("المستخدم معطّل بالفعل", "ALREADY_INACTIVE");

        user.Deactivate();
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            return Result.Failure("المستخدم غير موجود", "NOT_FOUND");

        await _unitOfWork.Users.DeleteAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}