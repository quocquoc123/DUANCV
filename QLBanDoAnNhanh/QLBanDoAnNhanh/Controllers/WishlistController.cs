using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;
using QLBanDoAnNhanh.Services;

namespace QLBanDoAnNhanh.Controllers;

public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;
    private readonly QlbanDoAnNhanh3Context _context;

    public WishlistController(IWishlistService wishlistService, QlbanDoAnNhanh3Context context)
    {
        _wishlistService = wishlistService;
        _context = context;
    }

    // ==========================================
    // USER ACTIONS
    // ==========================================

    /// <summary>
    /// Trang danh sách yêu thích của người dùng: GET /Wishlist
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Session.GetString("UserID");
        if (string.IsNullOrEmpty(userIdStr))
        {
            TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem danh sách yêu thích.";
            return RedirectToAction("Login", "User");
        }

        int userId = int.Parse(userIdStr);
        var viewModel = await _wishlistService.GetUserWishlistAsync(userId, cancellationToken);
        return View(viewModel);
    }

    /// <summary>
    /// Lấy số lượng sản phẩm yêu thích: GET /Wishlist/Count
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Count(CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Session.GetString("UserID");
        if (string.IsNullOrEmpty(userIdStr))
            return Json(new { count = 0 });

        int userId = int.Parse(userIdStr);
        var count = await _wishlistService.GetWishlistCountAsync(userId, cancellationToken);
        return Json(new { count });
    }

    /// <summary>
    /// Toggle thêm/bỏ yêu thích: POST /Wishlist/Toggle/5
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Session.GetString("UserID");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return Json(new
            {
                success = false,
                requireLogin = true,
                message = "Vui lòng đăng nhập để thêm sản phẩm vào danh sách yêu thích."
            });
        }

        int userId = int.Parse(userIdStr);
        var result = await _wishlistService.ToggleWishlistAsync(userId, id, cancellationToken);

        return Json(new
        {
            success = result.Success,
            isWishlisted = result.IsWishlisted,
            count = result.Count,
            message = result.Message,
            requireLogin = false
        });
    }

    /// <summary>
    /// Xóa khỏi yêu thích (từ trang Wishlist): POST /Wishlist/Remove/5
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Session.GetString("UserID");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return Json(new { success = false, message = "Chưa đăng nhập" });
        }

        int userId = int.Parse(userIdStr);
        var removed = await _wishlistService.RemoveFromWishlistAsync(userId, id, cancellationToken);

        var count = removed ? await _wishlistService.GetWishlistCountAsync(userId, cancellationToken) : 0;

        return Json(new
        {
            success = removed,
            count,
            message = removed ? "Đã xóa khỏi danh sách yêu thích" : "Không tìm thấy sản phẩm"
        });
    }

    /// <summary>
    /// Kiểm tra trạng thái yêu thích: GET /Wishlist/Check/5
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Check(int id, CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Session.GetString("UserID");
        if (string.IsNullOrEmpty(userIdStr))
            return Json(new { isWishlisted = false });

        int userId = int.Parse(userIdStr);
        var isWishlisted = await _wishlistService.IsWishlistedAsync(userId, id, cancellationToken);
        return Json(new { isWishlisted });
    }

    // ==========================================
    // ADMIN ACTIONS
    // ==========================================

    /// <summary>
    /// Trang quản lý Wishlist (Admin): GET /Wishlist/AdminIndex
    /// </summary>
    public async Task<IActionResult> AdminIndex(
        string searchKeyword = null,
        int? filterUserId = null,
        int? filterProductId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortBy = "NgayThem",
        string sortDir = "desc",
        int page = 1,
        int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        // Kiểm tra quyền Admin
        var adminLogin = HttpContext.Session.GetString("adminLogin");
        if (string.IsNullOrEmpty(adminLogin))
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToAction("Login", "User");
        }

        var viewModel = await _wishlistService.GetAdminWishlistAsync(
            searchKeyword, filterUserId, filterProductId,
            fromDate, toDate, sortBy, sortDir, page, pageSize, cancellationToken);

        return View(viewModel);
    }

    /// <summary>
    /// Lấy thống kê Wishlist cho Dashboard: GET /Wishlist/Stats
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken)
    {
        var adminLogin = HttpContext.Session.GetString("adminLogin");
        if (string.IsNullOrEmpty(adminLogin))
            return Json(new { success = false });

        var stats = await _wishlistService.GetDashboardStatsAsync(cancellationToken);
        return Json(new
        {
            success = true,
            tongLuotYeuThich = stats.TongLuotYeuThich,
            tongSanPhamDuocYeuThich = stats.TongSanPhamDuocYeuThich,
            top10 = stats.Top10SanPham.Select(p => new
            {
                maSp = p.MaSp,
                tenSp = p.TenSp,
                hinhAnh1 = p.HinhAnh1,
                soLuotYeuThich = p.SoLuotYeuThich,
                giaTien = p.GiaTien
            })
        });
    }

    /// <summary>
    /// Admin xóa một wishlist record: POST /Wishlist/AdminDelete/5
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AdminDelete(int id, CancellationToken cancellationToken)
    {
        var adminLogin = HttpContext.Session.GetString("adminLogin");
        if (string.IsNullOrEmpty(adminLogin))
            return Json(new { success = false, message = "Không có quyền" });

        var item = await _context.SanPhamYeuThichs
            .FirstOrDefaultAsync(w => w.WishlistId == id, cancellationToken);

        if (item == null)
            return Json(new { success = false, message = "Không tìm thấy bản ghi" });

        _context.SanPhamYeuThichs.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return Json(new { success = true, message = "Đã xóa bản ghi yêu thích" });
    }
}
