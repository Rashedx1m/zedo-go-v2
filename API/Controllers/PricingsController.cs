using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class PricingsController : BaseController
{
    private readonly IPricingService _pricingService;

    public PricingsController(IPricingService pricingService) => _pricingService = pricingService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _pricingService.GetAllAsync();
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _pricingService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var result = await _pricingService.GetActiveAsync();
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePricingDto dto)
    {
        var result = await _pricingService.CreateAsync(dto);
        return HandleResult(result);
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _pricingService.ActivateAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _pricingService.DeactivateAsync(id);
        return HandleResult(result);
    }

    [HttpGet("estimate")]
    public async Task<IActionResult> EstimateCost(
        [FromQuery] double pickupLat,
        [FromQuery] double pickupLon,
        [FromQuery] double dropLat,
        [FromQuery] double dropLon)
    {
        var result = await _pricingService.EstimateCostAsync(pickupLat, pickupLon, dropLat, dropLon);
        return HandleResult(result);
    }
}