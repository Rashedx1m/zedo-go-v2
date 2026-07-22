using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _dashboardService.GetStatsAsync();
        return HandleResult(result);
    }
}