using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class PaymentsController : BaseController
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService) => _paymentService = paymentService;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _paymentService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpGet("request/{requestId}")]
    public async Task<IActionResult> GetByRequestId(int requestId)
    {
        var result = await _paymentService.GetByRequestIdAsync(requestId);
        return HandleResult(result);
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] ProcessPaymentDto dto)
    {
        var result = await _paymentService.ProcessAsync(dto);
        return HandleResult(result);
    }

    [HttpGet("driver/{driverId}/earnings")]
    public async Task<IActionResult> GetDriverEarnings(int driverId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _paymentService.GetDriverEarningsAsync(driverId, from, to);
        return HandleResult(result);
    }

    [HttpGet("company/revenue")]
    public async Task<IActionResult> GetCompanyRevenue([FromQuery] DateTime? from,[FromQuery] DateTime? to)
    {
        var result = await _paymentService.GetCompanyRevenueAsync(from, to);
        return HandleResult(result);
    }
}