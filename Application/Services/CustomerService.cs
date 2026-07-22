using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer == null)
            return Result.Failure<CustomerDto>("العميل غير موجود", "NOT_FOUND");

        var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
        return Result.Success(customer.ToDto(user!));
    }

    public async Task<Result<CustomerDto>> GetByUserIdAsync(int userId)
    {
        var customer = await _unitOfWork.Customers.GetByUserIdAsync(userId);
        if (customer == null)
            return Result.Failure<CustomerDto>("العميل غير موجود", "NOT_FOUND");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return Result.Success(customer.ToDto(user!));
    }

    public async Task<Result<IEnumerable<CustomerDto>>> GetAllAsync()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();
        var customerDtos = new List<CustomerDto>();

        foreach (var customer in customers)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
            customerDtos.Add(customer.ToDto(user!));
        }

        return Result.Success<IEnumerable<CustomerDto>>(customerDtos);
    }

    public async Task<Result<int>> CreateAsync(int userId)
    {
        // التحقق من وجود المستخدم
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return Result.Failure<int>("المستخدم غير موجود", "USER_NOT_FOUND");

        // التحقق من أن المستخدم ليس عميلاً بالفعل
        var existingCustomer = await _unitOfWork.Customers.GetByUserIdAsync(userId);
        if (existingCustomer != null)
            return Result.Failure<int>("المستخدم مسجل كعميل بالفعل", "ALREADY_CUSTOMER");

        var customer = Customer.Create(userId);
        await _unitOfWork.Customers.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(customer.Id);
    }
}