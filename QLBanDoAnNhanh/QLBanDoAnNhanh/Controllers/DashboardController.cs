using Microsoft.AspNetCore.Mvc;
using QLBanDoAnNhanh.Services;

namespace QLBanDoAnNhanh.Controllers;

public class DashboardController : Controller
{
    private readonly IProductDiscountService _discountService;

    public DashboardController(IProductDiscountService discountService)
    {
        _discountService = discountService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var discountDashboard = await _discountService.GetManagementPageAsync(cancellationToken);
        return View(discountDashboard.Dashboard);
    }
}
