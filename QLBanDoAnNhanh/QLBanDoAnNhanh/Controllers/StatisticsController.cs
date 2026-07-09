using Microsoft.AspNetCore.Mvc;
using QLBanDoAnNhanh.Services;

namespace QLBanDoAnNhanh.Controllers;

[ApiController]
[Route("api/statistics")]
public sealed class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] string period = "day",
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetRevenueAsync(period, fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetProductStatisticsAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetCustomerStatisticsAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string groupBy = "day",
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetOrderStatisticsAsync(groupBy, fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetPaymentStatisticsAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("promotions")]
    public async Task<IActionResult> GetPromotions(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetPromotionStatisticsAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }
}
