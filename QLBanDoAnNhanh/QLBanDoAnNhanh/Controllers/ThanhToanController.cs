using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;
using QLBanDoAnNhanh.Services;

namespace QLBanDoAnNhanh.Controllers;

public class ThanhToanController : Controller
{
    private readonly QlbanDoAnNhanh3Context _context;
    private readonly MomoService _momoService;
    private readonly MomoSettings _momoSettings;

    public ThanhToanController(
        QlbanDoAnNhanh3Context context,
        MomoService momoService,
        Microsoft.Extensions.Options.IOptions<MomoSettings> momoSettings)
    {
        _context = context;
        _momoService = momoService;
        _momoSettings = momoSettings.Value;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMomoPayment(string DiaChi, string Phone, string voucherCode = null)
    {
        var username = HttpContext.Session.GetString("userLogin");
        if (string.IsNullOrEmpty(username))
        {
            TempData["Message"] = "Vui lòng đăng nhập trước khi thanh toán!";
            return RedirectToAction("Login", "User");
        }

        var gioHang = GetGioHangFromSession();
        if (!gioHang.ChiTietGioHangs.Any())
        {
            TempData["Message"] = "Giỏ hàng của bạn đang trống!";
            return RedirectToAction("Index", "GioHangs");
        }

        foreach (var item in gioHang.ChiTietGioHangs)
        {
            var sanPham = await _context.SanPhams.FirstOrDefaultAsync(sp => sp.MaSp == item.MaSp);
            if (sanPham == null || item.SoLuongSp > sanPham.SlbanTrongNgay)
            {
                TempData["ErrorMessage"] = "Không đủ số lượng sản phẩm trong kho!";
                return RedirectToAction("Index", "GioHangs");
            }
        }

        var maNguoiDung = int.Parse(HttpContext.Session.GetString("UserID") ?? "0");
        var maDonHang = GenerateOrderCode();

        if (!string.IsNullOrWhiteSpace(Phone))
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(maNguoiDung);
            if (nguoiDung != null)
            {
                nguoiDung.Sdt = Phone.Trim();
            }
        }

        double tongTien = gioHang.ChiTietGioHangs.Sum(x => (double)(x.TongTien ?? 0));
        string maKhuyenMaiSuDung = null;

        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            var khuyenMai = await _context.KhuyenMais.FirstOrDefaultAsync(km => km.MaKhuyenMai == voucherCode);
            if (khuyenMai != null && khuyenMai.TrangThai && khuyenMai.SoLuong > 0)
            {
                var now = DateTime.Now;
                if (now >= khuyenMai.ThoiGianBatDau &&
                    now <= khuyenMai.ThoiGianKetThuc &&
                    tongTien >= (khuyenMai.DieuKienApDung ?? 0))
                {
                    maKhuyenMaiSuDung = voucherCode;
                    double tienGiam = tongTien * (khuyenMai.GiaTri / 100.0);
                    tongTien -= tienGiam;
                    khuyenMai.SoLuong -= 1;
                }
            }
        }

        var expiresAt = DateTime.Now.AddMinutes(_momoSettings.PaymentTimeoutMinutes);
        var donHang = new DonHang
        {
            MaDh = maDonHang,
            Username = username,
            MaKhuyenMai = maKhuyenMaiSuDung,
            Diachi = DiaChi,
            TongTien = tongTien,
            SoLuong = (int)gioHang.ChiTietGioHangs.Sum(x => x.SoLuongSp),
            TrangThai = "Chờ thanh toán",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            MaNguoiDung = maNguoiDung
        };

        _context.DonHangs.Add(donHang);

        foreach (var item in gioHang.ChiTietGioHangs)
        {
            _context.ChiTietDonHangs.Add(new ChiTietDonHang
            {
                MaDh = maDonHang,
                MaSp = (int)item.MaSp,
                SoLuong = (int)item.SoLuongSp,
                TongTien = (int)item.TongTien
            });
        }

        await _context.SaveChangesAsync();

        var amount = (long)Math.Round(tongTien, 0);
        var orderInfo = $"Thanh toan {maDonHang}";
        var momoResult = await _momoService.CreatePaymentAsync(maDonHang, amount, orderInfo);

        if (!momoResult.Success || momoResult.UseFallbackQr || !MomoService.IsRealMomoQrPayload(momoResult.QrCodeUrl))
        {
            TempData["ErrorMessage"] = "Không tạo được mã QR MoMo: " + momoResult.Message;
            return RedirectToAction("Index", "GioHangs");
        }

        _context.ThanhToans.Add(new ThanhToan
        {
            MaDh = maDonHang,
            PhuongThucThanhToan = "MoMo QR",
            PaymentMethod = "MoMo",
            PaymentStatus = "Pending",
            NgayThanhToan = DateTime.Now,
            TongTien = tongTien,
            TrangThaiThanhToan = false,
            QrCodeUrl = momoResult.QrCodeUrl,
            MomoPayUrl = momoResult.PayUrl,
            PaymentExpiresAt = expiresAt,
            MomoRequestId = momoResult.RequestId
        });
        await _context.SaveChangesAsync();

        HttpContext.Session.SetString("PendingMomoOrderId", maDonHang);
        return RedirectToAction(nameof(MomoQr), new { maDh = maDonHang });
    }

    [HttpGet]
    public async Task<IActionResult> MomoQr(string maDh)
    {
        var donHang = await _context.DonHangs
            .Include(d => d.ThanhToans)
            .FirstOrDefaultAsync(d => d.MaDh == maDh);

        if (donHang == null)
        {
            return NotFound();
        }

        var thanhToan = donHang.ThanhToans.FirstOrDefault(t => t.PaymentMethod == "MoMo");
        if (thanhToan == null)
        {
            return NotFound();
        }

        if (thanhToan.PaymentStatus == "Paid")
        {
            return RedirectToAction(nameof(MomoSuccess), new { maDh });
        }

        if (IsPaymentExpired(thanhToan))
        {
            await MarkPaymentExpiredAsync(donHang, thanhToan);
            return View("MomoExpired", donHang);
        }

        var qrPayload = thanhToan.QrCodeUrl;
        if (!MomoService.IsRealMomoQrPayload(qrPayload))
        {
            TempData["ErrorMessage"] = "Mã QR không hợp lệ hoặc đã hết hạn. Vui lòng đặt hàng lại.";
            return RedirectToAction("Index", "GioHangs");
        }

        ViewBag.QrPayload = qrPayload;
        ViewBag.MomoPayUrl = thanhToan.MomoPayUrl;
        ViewBag.IsSandbox = _momoSettings.Endpoint.Contains("test-payment", StringComparison.OrdinalIgnoreCase);
        ViewBag.ExpiresAt = thanhToan.PaymentExpiresAt?.ToString("o");
        ViewBag.Amount = donHang.TongTien;
        ViewBag.OrderInfo = donHang.MaDh;

        return View(donHang);
    }

    [HttpGet]
    public async Task<IActionResult> MomoReturn(string orderId, int? resultCode)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng thanh toán.";
            return RedirectToAction("Index", "GioHangs");
        }

        var donHang = await _context.DonHangs
            .Include(d => d.ThanhToans)
            .FirstOrDefaultAsync(d => d.MaDh == orderId);

        if (donHang == null)
        {
            return NotFound();
        }

        var thanhToan = donHang.ThanhToans.FirstOrDefault(t => t.PaymentMethod == "MoMo");
        if (thanhToan == null)
        {
            return NotFound();
        }

        if (thanhToan.PaymentStatus == "Paid")
        {
            ClearCartSession();
            return RedirectToAction(nameof(MomoSuccess), new { maDh = orderId });
        }

        if (resultCode.HasValue && resultCode.Value != 0)
        {
            TempData["MomoWarning"] = "Giao dịch chưa hoàn tất. Vui lòng quét mã QR để thanh toán.";
            return RedirectToAction(nameof(MomoQr), new { maDh = orderId });
        }

        return RedirectToAction(nameof(MomoQr), new { maDh = orderId });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MomoNotify([FromBody] MomoIpnRequest request)
    {
        var result = _momoService.ProcessIpnAsync(request);
        if (!result.IsValid)
        {
            return BadRequest(new { message = result.Message });
        }

        var donHang = await _context.DonHangs
            .Include(d => d.ThanhToans)
            .Include(d => d.ChiTietDonHangs)
            .FirstOrDefaultAsync(d => d.MaDh == result.OrderId);

        if (donHang == null)
        {
            return NotFound(new { message = "Order not found" });
        }

        var thanhToan = donHang.ThanhToans.FirstOrDefault(t => t.PaymentMethod == "MoMo");
        if (thanhToan == null)
        {
            return NotFound(new { message = "Payment not found" });
        }

        if (result.IsPaid)
        {
            await CompleteMomoPaymentAsync(donHang, thanhToan, result.TransactionId);
            return Ok(new { message = "Success" });
        }

        thanhToan.PaymentStatus = "Failed";
        donHang.TrangThai = "Thanh toán thất bại";
        donHang.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Payment failed" });
    }

    [HttpGet]
    public async Task<IActionResult> CheckMomoStatus(string maDh)
    {
        var donHang = await _context.DonHangs
            .Include(d => d.ThanhToans)
            .FirstOrDefaultAsync(d => d.MaDh == maDh);

        if (donHang == null)
        {
            return NotFound(new { status = "NotFound" });
        }

        var thanhToan = donHang.ThanhToans.FirstOrDefault(t => t.PaymentMethod == "MoMo");
        if (thanhToan == null)
        {
            return NotFound(new { status = "NotFound" });
        }

        if (thanhToan.PaymentStatus == "Pending" && IsPaymentExpired(thanhToan))
        {
            await MarkPaymentExpiredAsync(donHang, thanhToan);
        }

        return Json(new
        {
            status = thanhToan.PaymentStatus,
            orderStatus = donHang.TrangThai,
            expiresAt = thanhToan.PaymentExpiresAt?.ToString("o")
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExpireMomoPayment(string maDh)
    {
        var donHang = await _context.DonHangs
            .Include(d => d.ThanhToans)
            .FirstOrDefaultAsync(d => d.MaDh == maDh);

        if (donHang == null)
        {
            return NotFound();
        }

        var thanhToan = donHang.ThanhToans.FirstOrDefault(t => t.PaymentMethod == "MoMo");
        if (thanhToan == null)
        {
            return NotFound();
        }

        if (thanhToan.PaymentStatus == "Pending" && IsPaymentExpired(thanhToan))
        {
            await MarkPaymentExpiredAsync(donHang, thanhToan);
        }

        return Json(new { status = thanhToan.PaymentStatus });
    }

    [HttpGet]
    public IActionResult MomoSuccess(string maDh)
    {
        ClearCartSession();
        ViewBag.MaDh = maDh;
        TempData["Message"] = "Thanh toán thành công";
        return View();
    }

    private async Task CompleteMomoPaymentAsync(DonHang donHang, ThanhToan thanhToan, string transactionId)
    {
        if (thanhToan.PaymentStatus == "Paid")
        {
            return;
        }

        thanhToan.PaymentStatus = "Paid";
        thanhToan.TrangThaiThanhToan = true;
        thanhToan.TransactionId = transactionId;
        thanhToan.PaidAt = DateTime.Now;
        donHang.TrangThai = "Đã thanh toán";
        donHang.UpdatedAt = DateTime.Now;

        foreach (var chiTiet in donHang.ChiTietDonHangs)
        {
            var sanPham = await _context.SanPhams.FirstOrDefaultAsync(sp => sp.MaSp == chiTiet.MaSp);
            if (sanPham != null)
            {
                sanPham.SlbanTrongNgay -= chiTiet.SoLuong;
                if (sanPham.SlbanTrongNgay < 0)
                {
                    sanPham.SlbanTrongNgay = 0;
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task MarkPaymentExpiredAsync(DonHang donHang, ThanhToan thanhToan)
    {
        if (thanhToan.PaymentStatus != "Pending")
        {
            return;
        }

        thanhToan.PaymentStatus = "Expired";
        donHang.TrangThai = "Hết hạn thanh toán";
        donHang.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    private static bool IsPaymentExpired(ThanhToan thanhToan)
    {
        return thanhToan.PaymentExpiresAt.HasValue && DateTime.Now >= thanhToan.PaymentExpiresAt.Value;
    }

    private GioHang GetGioHangFromSession()
    {
        return HttpContext.Session.GetObjectFromJson<GioHang>("GioHang") ?? new GioHang();
    }

    private void ClearCartSession()
    {
        HttpContext.Session.SetObjectAsJson("GioHang", new GioHang());
        HttpContext.Session.SetInt32("CartItemCount", 0);
    }

    private static string GenerateOrderCode()
    {
        return "DH" + DateTime.Now.ToString("yyMMddHHmmss");
    }
}
