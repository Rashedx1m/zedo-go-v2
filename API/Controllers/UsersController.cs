// API/Controllers/UsersController.cs
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _userService.GetAllAsync();
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetByEmail(string email)
    {
        var result = await _userService.GetByEmailAsync(email);
        return HandleResult(result);
    }

    [HttpGet("role/{role}")]
    public async Task<IActionResult> GetByRole(UserRole role)
    {
        var result = await _userService.GetByRoleAsync(role);
        return HandleResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        return HandleResult(result);
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _userService.ActivateAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _userService.DeactivateAsync(id);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);
        return HandleResult(result);
    }
}