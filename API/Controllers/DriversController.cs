using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class DriversController : BaseController
{
    private readonly IDriverService _driverService;

    public DriversController(IDriverService driverService) => _driverService = driverService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _driverService.GetAllAsync();
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _driverService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var result = await _driverService.GetByUserIdAsync(userId);
        return HandleResult(result);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
    {
        var result = await _driverService.GetAvailableAsync();
        return HandleResult(result);
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radiusKm = 5)
    {
        var result = await _driverService.GetNearbyAsync(lat, lon, radiusKm);
        return HandleResult(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDriverDto dto)
    {
        var result = await _driverService.RegisterAsync(dto);
        return HandleResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDriverDto dto)
    {
        var result = await _driverService.UpdateAsync(id, dto);
        return HandleResult(result);
    }

    [HttpPost("{id}/online")]
    public async Task<IActionResult> GoOnline(int id)
    {
        var result = await _driverService.GoOnlineAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id}/offline")]
    public async Task<IActionResult> GoOffline(int id)
    {
        var result = await _driverService.GoOfflineAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id}/location")]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationDto dto)
    {
        var result = await _driverService.UpdateLocationAsync(id, dto);
        return HandleResult(result);
    }

    [HttpGet("{id}/location")]
    public async Task<IActionResult> GetLocation(int id)
    {
        var result = await _driverService.GetLocationAsync(id);
        return HandleResult(result);
    }
}