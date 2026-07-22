using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class CustomersController : BaseController
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService) => _customerService = customerService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _customerService.GetAllAsync();
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _customerService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var result = await _customerService.GetByUserIdAsync(userId);
        return HandleResult(result);
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> Create(int userId)
    {
        var result = await _customerService.CreateAsync(userId);
        return HandleResult(result);
    }
}