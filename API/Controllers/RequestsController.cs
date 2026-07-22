using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class RequestsController : BaseController
{
    private readonly IRequestService _requestService;

    public RequestsController(IRequestService requestService) => _requestService = requestService;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _requestService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpGet("{id}/full")]
    public async Task<IActionResult> GetFullInfo(int id)
    {
        var result = await _requestService.GetFullInfoAsync(id);
        return HandleResult(result);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var result = await _requestService.GetByCustomerAsync(customerId);
        return HandleResult(result);
    }

    // ✅ إضافة endpoint للطلب النشط للعميل
    [HttpGet("customer/{customerId}/active")]
    public async Task<IActionResult> GetActiveByCustomer(int customerId)
    {
        var result = await _requestService.GetActiveByCustomerAsync(customerId);
        return HandleResult(result);
    }

    [HttpGet("driver/{driverId}")]
    public async Task<IActionResult> GetByDriver(int driverId)
    {
        var result = await _requestService.GetByDriverAsync(driverId);
        return HandleResult(result);
    }

    // ✅ إضافة endpoint للطلب النشط للسائق
    [HttpGet("driver/{driverId}/active")]
    public async Task<IActionResult> GetActiveByDriver(int driverId)
    {
        var result = await _requestService.GetActiveByDriverAsync(driverId);
        return HandleResult(result);
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(RequestStatus status)
    {
        var result = await _requestService.GetByStatusAsync(status);
        return HandleResult(result);
    }

    // ✅ إنشاء طلب - يستخدم customerId من المستخدم الحالي
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto)
    {
        var userId = GetUserId();

        // جلب الـ customerId من الـ userId
        var result = await _requestService.CreateByUserIdAsync(userId, dto);
        return HandleResult(result);
    }

    // ✅ أو إنشاء طلب مع تمرير customerId
    [HttpPost("customer/{customerId}")]
    public async Task<IActionResult> CreateForCustomer(int customerId, [FromBody] CreateRequestDto dto)
    {
        var result = await _requestService.CreateAsync(customerId, dto);
        return HandleResult(result);
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> Accept(int id, [FromQuery] int driverId)
    {
        var result = await _requestService.AcceptAsync(id, driverId);
        return HandleResult(result);
    }

    [HttpPost("{id}/arrived")]
    public async Task<IActionResult> DriverArrived(int id)
    {
        var result = await _requestService.DriverArrivedAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartRide(int id)
    {
        var result = await _requestService.StartRideAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromQuery] PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        var result = await _requestService.CompleteAsync(id, paymentMethod);
        return HandleResult(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] string? reason = null)
    {
        var canceledBy = "Customer";
        var result = await _requestService.CancelAsync(id, canceledBy, reason);
        return HandleResult(result);
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyForDriver([FromQuery] int driverId, [FromQuery] double radiusKm = 10)
    {
        var result = await _requestService.GetNearbyForDriverAsync(driverId, radiusKm);
        return HandleResult(result);
    }
}