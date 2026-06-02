using Microsoft.AspNetCore.Mvc;
using QLBanDoAnNhanh.Services;
using QLBanDoAnNhanh.ViewModels;

namespace QLBanDoAnNhanh.Controllers;

public class ProductDiscountsController : Controller
{
    private readonly IProductDiscountService _discountService;

    public ProductDiscountsController(IProductDiscountService discountService)
    {
        _discountService = discountService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _discountService.GetManagementPageAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ProductDiscountFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["DiscountError"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _discountService.ApplyDiscountAsync(model, cancellationToken);
            TempData["DiscountSuccess"] = "Đã cập nhật giảm giá sản phẩm.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["DiscountError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _discountService.CancelDiscountAsync(id, cancellationToken);
            TempData["DiscountSuccess"] = "Đã hủy giảm giá sản phẩm.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["DiscountError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
