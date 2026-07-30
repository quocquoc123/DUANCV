    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using QLBanDoAnNhanh.Models;
    using DinkToPdf;
    using DinkToPdf.Contracts;
    using PayPalCheckoutSdk.Core;
    using PayPalCheckoutSdk.Orders;
    using QLBanDoAnNhanh.Services;
    using QLBanDoAnNhanh.DTOs;
namespace QLBanDoAnNhanh.Controllers
    {
    public class GioHangsController : Controller

    {
        private readonly PayPalService _payPalService;
        private readonly VoucherService _voucherService;
        private readonly QlbanDoAnNhanh3Context _context;
        private readonly IProductDiscountService _discountService;

       

        public GioHangsController(QlbanDoAnNhanh3Context context, PayPalService payPalService, VoucherService voucherService, IProductDiscountService discountService)
        {
            _context = context;
            _payPalService = payPalService;
            _voucherService = voucherService;
            _discountService = discountService;
        }

        // Tính tổng số sản phẩm trong giỏ hàng và cập nhật ViewBag
        private void UpdateCartItemCount(GioHang gioHang)
        {
            int count = gioHang?.ChiTietGioHangs?.Sum(ct => ct.SoLuongSp ?? 0) ?? 0;
            HttpContext.Session.SetInt32("CartItemCount", count); // Lưu vào session
        }

        private GioHang GetGioHangFromSession()
        {
            var gioHang = HttpContext.Session.GetObjectFromJson<GioHang>("GioHang");
            if (gioHang == null)
            {
                gioHang = new GioHang();
            }
            return gioHang;
        }
        // Lưu giỏ hàng vào session
        private void SaveGioHangToSession(GioHang gioHang)
        {
            HttpContext.Session.SetObjectAsJson("GioHang", gioHang);
        }
        //private void UpdateCartItemCount(GioHang gioHang)
        //{
        //    ViewBag.CartItemCount = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuongSp);
        //}
        // Phương thức thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public IActionResult AddToCart(int MaSp, int quantity)
        {
            var gioHang = GetGioHangFromSession(); // Lấy giỏ hàng hiện tại từ session

            // Tìm sản phẩm theo mã sản phẩm
            using (var context = new QlbanDoAnNhanh3Context())
            {
                var sanPham = context.SanPhams
                    .Include(sp => sp.MaGiamGiaNavigation)
                    .FirstOrDefault(sp => sp.MaSp == MaSp);
                if (sanPham == null)
                {
                    return NotFound(); // Nếu không tìm thấy sản phẩm, trả về lỗi 404
                }

                // Kiểm tra số lượng tồn kho
                var chiTietGioHang = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.MaSp == MaSp);
                int currentQuantityInCart = chiTietGioHang?.SoLuongSp ?? 0;
                int totalQuantityAfterAdding = currentQuantityInCart + quantity;

                if (totalQuantityAfterAdding > sanPham.SlbanTrongNgay)
                {
                    TempData["ErrorMessage"] = "Không đủ số lượng sản phẩm trong kho!"; // Thông báo lỗi
                    return RedirectToAction("TrangChu", "SanPhams"); // Quay lại giỏ hàng và hiển thị thông báo lỗi
                }

                // Nếu sản phẩm chưa có trong giỏ, thêm sản phẩm mới vào giỏ
                if (chiTietGioHang == null)
                {
                    chiTietGioHang = NewMethod(MaSp, quantity, sanPham, _discountService.GetEffectivePrice(sanPham));
                    gioHang.ChiTietGioHangs.Add(chiTietGioHang);
                }
                else
                {
                    // Nếu sản phẩm đã có trong giỏ, cập nhật số lượng và tổng tiền
                    chiTietGioHang.SoLuongSp += quantity;
                    chiTietGioHang.TongTien = (int)(chiTietGioHang.SoLuongSp * _discountService.GetEffectivePrice(sanPham));
                }

                // Lưu lại giỏ hàng vào session
                SaveGioHangToSession(gioHang);
                UpdateCartItemCount(gioHang);
            }

            // Điều hướng về trang giỏ hàng
            return RedirectToAction("Index");
        }

        // ============================================================
        // AJAX endpoint – Thêm vào giỏ hàng (trả JSON, không redirect)
        // POST /GioHangs/AddToCartAjax
        // ============================================================
        [HttpPost]
        public IActionResult AddToCartAjax(int MaSp, int quantity = 1)
        {
            var gioHang = GetGioHangFromSession();

            using (var context = new QlbanDoAnNhanh3Context())
            {
                var sanPham = context.SanPhams
                    .Include(sp => sp.MaGiamGiaNavigation)
                    .FirstOrDefault(sp => sp.MaSp == MaSp);

                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                var chiTietGioHang = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.MaSp == MaSp);
                int currentQty = chiTietGioHang?.SoLuongSp ?? 0;
                int totalQty   = currentQty + quantity;

                if (totalQty > sanPham.SlbanTrongNgay)
                {
                    return Json(new { success = false, message = "Không đủ số lượng sản phẩm trong kho!" });
                }

                if (chiTietGioHang == null)
                {
                    chiTietGioHang = NewMethod(MaSp, quantity, sanPham, _discountService.GetEffectivePrice(sanPham));
                    gioHang.ChiTietGioHangs.Add(chiTietGioHang);
                }
                else
                {
                    chiTietGioHang.SoLuongSp += quantity;
                    chiTietGioHang.TongTien   = (int)(chiTietGioHang.SoLuongSp * _discountService.GetEffectivePrice(sanPham));
                }

                SaveGioHangToSession(gioHang);
                UpdateCartItemCount(gioHang);

                int cartCount = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuongSp ?? 0);
                return Json(new { success = true, cartCount });
            }
        }

        // ============================================================
        // GET /GioHangs/GetCartItemCount  – Trả về số lượng hiện tại
        // ============================================================
        [HttpGet]
        public IActionResult GetCartItemCount()
        {
            int count = HttpContext.Session.GetInt32("CartItemCount") ?? 0;
            return Json(count);
        }

        // ============================================================
        // GET /GioHangs/GetChiNhanhs – Trả về danh sách chi nhánh đang hoạt động
        //   (bao gồm lat/lon để client tính khoảng cách)
        // ============================================================
        [HttpGet]
        public IActionResult GetChiNhanhs()
        {
            var chiNhanhs = _context.ChiNhanhs
                .AsNoTracking()
                .Where(cn => cn.TrangThai)
                .OrderBy(cn => cn.TenChiNhanh)
                .Select(cn => new
                {
                    maChiNhanh  = cn.MaChiNhanh,
                    tenChiNhanh = cn.TenChiNhanh,
                    diaChi      = cn.DiaChi,
                    latitude    = cn.Latitude,
                    longitude   = cn.Longitude
                })
                .ToList();

            return Json(chiNhanhs);
        }

        // ============================================================
        // POST /GioHangs/CheckStockByBranch – Kiểm tra tồn kho theo chi nhánh
        // ============================================================
        [HttpPost]
        public IActionResult CheckStockByBranch([FromBody] int maChiNhanh)
        {
            var gioHang = GetGioHangFromSession();
            if (!gioHang.ChiTietGioHangs.Any())
                return Json(new { outOfStock = false, items = new object[0] });

            var cartMaSpList = gioHang.ChiTietGioHangs.Select(ct => (int)ct.MaSp).ToList();

            // Lấy các sản phẩm trong chi nhánh đã chọn (dựa theo bảng SanPhamChiNhanh)
            var spTrongChiNhanh = _context.SanPhamChiNhanhs
                .AsNoTracking()
                .Where(spcn => spcn.MaChiNhanh == maChiNhanh && cartMaSpList.Contains(spcn.MaSp))
                .Select(spcn => spcn.MaSp)
                .ToHashSet();

            var outOfStockItems = _context.SanPhams
                .AsNoTracking()
                .Where(sp => !spTrongChiNhanh.Contains(sp.MaSp) && cartMaSpList.Contains(sp.MaSp))
                .Select(sp => new { maSp = sp.MaSp, tenSp = sp.TenSp })
                .ToList();

            return Json(new
            {
                outOfStock = outOfStockItems.Any(),
                items      = outOfStockItems
            });
        }

        // ============================================================
        // POST /GioHangs/GetSuggestedBranch
        //   Body: { "latitude": double, "longitude": double }
        //   Logic:
        //     1. Lấy tất cả sản phẩm trong giỏ hàng.
        //     2. Tìm chi nhánh có ĐỦ TẤT CẢ sản phẩm (theo SanPhamChiNhanh).
        //        Nếu không có chi nhánh nào có đủ → trả về chi nhánh có nhiều sản phẩm nhất.
        //     3. Trong số những chi nhánh hợp lệ, chọn chi nhánh gần người dùng nhất
        //        (dùng tọa độ lat/lon gửi lên). Nếu người dùng không có tọa độ → chọn đầu tiên.
        // ============================================================
        [HttpPost]
        public IActionResult GetSuggestedBranch([FromBody] UserLocationDto location)
        {
            var gioHang = GetGioHangFromSession();
            if (!gioHang.ChiTietGioHangs.Any())
                return Json(new { success = false, message = "Giỏ hàng trống." });

            var cartMaSpList = gioHang.ChiTietGioHangs
                .Select(ct => (int)ct.MaSp)
                .Distinct()
                .ToList();
            int cartItemCount = cartMaSpList.Count;

            // Lấy tất cả chi nhánh đang hoạt động kèm thông tin tọa độ
            var chiNhanhs = _context.ChiNhanhs
                .AsNoTracking()
                .Where(cn => cn.TrangThai)
                .Select(cn => new
                {
                    cn.MaChiNhanh,
                    cn.TenChiNhanh,
                    cn.DiaChi,
                    cn.Latitude,
                    cn.Longitude
                })
                .ToList();

            if (!chiNhanhs.Any())
                return Json(new { success = false, message = "Không có chi nhánh hoạt động." });

            // Lấy bảng ánh xạ sản phẩm ↔ chi nhánh cho giỏ hàng hiện tại
            var mappings = _context.SanPhamChiNhanhs
                .AsNoTracking()
                .Where(spcn => cartMaSpList.Contains(spcn.MaSp))
                .Select(spcn => new { spcn.MaSp, spcn.MaChiNhanh })
                .ToList();

            // Đếm số sản phẩm trong giỏ có mặt tại từng chi nhánh
            var coverageByBranch = mappings
                .GroupBy(m => m.MaChiNhanh)
                .ToDictionary(g => g.Key, g => g.Select(m => m.MaSp).Distinct().Count());

            // Tìm chi nhánh có ĐỦ tất cả sản phẩm
            var fullCoverageBranches = chiNhanhs
                .Where(cn => coverageByBranch.TryGetValue(cn.MaChiNhanh, out var cnt) && cnt >= cartItemCount)
                .ToList();

            // Nếu không có chi nhánh nào đủ → dùng nhóm chi nhánh có độ phủ cao nhất (fallback)
            int bestCoverage = coverageByBranch.Any() ? coverageByBranch.Max(x => x.Value) : 0;
            var candidateBranches = fullCoverageBranches.Any()
                ? fullCoverageBranches
                : chiNhanhs
                    .Where(cn => coverageByBranch.TryGetValue(cn.MaChiNhanh, out var cnt) && cnt == bestCoverage)
                    .ToList();

            // Nếu vẫn không có → trả về chi nhánh đầu tiên
            if (!candidateBranches.Any())
                candidateBranches = chiNhanhs.Take(1).ToList();

            // Chọn chi nhánh gần nhất (nếu có tọa độ người dùng và chi nhánh)
            object bestBranch;
            bool hasUserLocation = location != null
                && location.Latitude.HasValue
                && location.Longitude.HasValue;

            if (hasUserLocation && candidateBranches.Any(cn => cn.Latitude.HasValue && cn.Longitude.HasValue))
            {
                double userLat = location.Latitude!.Value;
                double userLon = location.Longitude!.Value;

                bestBranch = candidateBranches
                    .OrderBy(cn =>
                    {
                        if (!cn.Latitude.HasValue || !cn.Longitude.HasValue) return double.MaxValue;
                        return HaversineDistance(userLat, userLon, cn.Latitude.Value, cn.Longitude.Value);
                    })
                    .Select(cn => new
                    {
                        maChiNhanh       = cn.MaChiNhanh,
                        tenChiNhanh      = cn.TenChiNhanh,
                        diaChi           = cn.DiaChi,
                        latitude         = cn.Latitude,
                        longitude        = cn.Longitude,
                        hasFullCoverage  = fullCoverageBranches.Any(f => f.MaChiNhanh == cn.MaChiNhanh),
                        coverageCount    = coverageByBranch.TryGetValue(cn.MaChiNhanh, out var cnt) ? cnt : 0,
                        missingCount     = Math.Max(0, cartItemCount - (coverageByBranch.TryGetValue(cn.MaChiNhanh, out var cnt2) ? cnt2 : 0)),
                        distanceKm       = cn.Latitude.HasValue && cn.Longitude.HasValue
                            ? Math.Round(HaversineDistance(userLat, userLon, cn.Latitude.Value, cn.Longitude.Value), 2)
                            : (double?)null
                    })
                    .First();
            }
            else
            {
                // Không có tọa độ → lấy chi nhánh đầu tiên trong danh sách ứng viên
                var cn0 = candidateBranches.First();
                bestBranch = new
                {
                    maChiNhanh      = cn0.MaChiNhanh,
                    tenChiNhanh     = cn0.TenChiNhanh,
                    diaChi          = cn0.DiaChi,
                    latitude        = cn0.Latitude,
                    longitude       = cn0.Longitude,
                    hasFullCoverage = fullCoverageBranches.Any(f => f.MaChiNhanh == cn0.MaChiNhanh),
                    coverageCount   = coverageByBranch.TryGetValue(cn0.MaChiNhanh, out var cnt) ? cnt : 0,
                    missingCount    = Math.Max(0, cartItemCount - (coverageByBranch.TryGetValue(cn0.MaChiNhanh, out var cnt2) ? cnt2 : 0)),
                    distanceKm      = (double?)null
                };
            }

            return Json(new { success = true, branch = bestBranch });
        }

        /// <summary>
        /// Tính khoảng cách (km) giữa hai điểm theo công thức Haversine.
        /// </summary>
        private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Bán kính Trái Đất (km)
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;

        private static ChiTietGioHang NewMethod(int MaSp, int quantity, SanPham sanPham, decimal effectivePrice)
        {
            return new ChiTietGioHang
            {
                MaSp = MaSp,
                MaSpNavigation = sanPham,
                SoLuongSp = quantity,
           
                TongTien = (int)(quantity * effectivePrice)
            };
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var gioHang = GetGioHangFromSession() ?? new GioHang();
            UpdateCartItemCount(gioHang); // Cập nhật số lượng giỏ hàng

            // Prefill thông tin checkout từ hồ sơ + đơn gần nhất (UI convenience)
            var username = HttpContext.Session.GetString("userLogin");
            if (!string.IsNullOrWhiteSpace(username))
            {
                var nguoiDung = _context.NguoiDungs.AsNoTracking().FirstOrDefault(nd => nd.Username == username);
                var lastOrder = _context.DonHangs
                    .AsNoTracking()
                    .Where(dh => dh.Username == username && !string.IsNullOrWhiteSpace(dh.Diachi))
                    .OrderByDescending(dh => dh.CreatedAt)
                    .FirstOrDefault();

                ViewBag.PrefillUsername = nguoiDung?.HoTen ?? username;
                ViewBag.PrefillPhone = nguoiDung?.Sdt ?? "";
                ViewBag.PrefillAddress = lastOrder?.Diachi ?? "";
            }

            return View(gioHang);
        }

        // Xóa sản phẩm khỏi giỏ hàng
        public IActionResult RemoveFromCart(int MaSp)
        {
            var gioHang = GetGioHangFromSession(); // Lấy giỏ hàng từ session

            // Tìm sản phẩm trong giỏ hàng
            var chiTietGioHang = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.MaSp == MaSp);
            if (chiTietGioHang != null)
            {
                gioHang.ChiTietGioHangs.Remove(chiTietGioHang); // Xóa sản phẩm khỏi giỏ hàng
            }

            // Cập nhật lại session
            SaveGioHangToSession(gioHang);
            UpdateCartItemCount(gioHang);

            // Điều hướng về trang giỏ hàng
            return RedirectToAction("Index");
        }

        // Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        public IActionResult UpdateCart(int MaSp, int quantity)
        {
            var gioHang = GetGioHangFromSession(); // Lấy giỏ hàng từ session

            // Tìm sản phẩm trong giỏ hàng
            var chiTietGioHang = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.MaSp == MaSp);
            if (chiTietGioHang != null)
            {
                chiTietGioHang.SoLuongSp = quantity;
                chiTietGioHang.TongTien = (int)(quantity * _discountService.GetEffectivePrice(chiTietGioHang.MaSpNavigation));
            }

            // Cập nhật lại session
            SaveGioHangToSession(gioHang);
            UpdateCartItemCount(gioHang);

            // Điều hướng về trang giỏ hàng
            return RedirectToAction("Index");
        }

        // Xóa toàn bộ giỏ hàng
        public IActionResult ClearCart()
        {
            var gioHang = new GioHang(); // Tạo mới giỏ hàng rỗng
            SaveGioHangToSession(gioHang); // Lưu lại giỏ hàng trống vào session

            // Điều hướng về trang giỏ hàng
            return RedirectToAction("Index");
        }
        public IActionResult Checkout(string DiaChi, string Phone, string voucherCode = null, double shippingFee = 0)
        {
            // Kiểm tra xem người dùng đã đăng nhập hay chưa
            var username = HttpContext.Session.GetString("userLogin");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Message"] = "Vui lòng đăng nhập trước khi thanh toán!";
                return RedirectToAction("Login", "User"); // Điều hướng đến trang đăng nhập
            }

            var gioHang = GetGioHangFromSession();

            if (!gioHang.ChiTietGioHangs.Any())
            {
                TempData["Message"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index");
            }

            string maDonHang = Guid.NewGuid().ToString();
            using (var context = new QlbanDoAnNhanh3Context())
            {
                foreach (var item in gioHang.ChiTietGioHangs)
                {
                    var sanPham = context.SanPhams.FirstOrDefault(sp => sp.MaSp == item.MaSp);
                    if (sanPham == null || item.SoLuongSp > sanPham.SlbanTrongNgay)
                    {
                        return RedirectToAction("Index"); // Quay lại trang giỏ hàng và hiển thị thông báo
                    }
                }

                string trangThai = GetOrderStatusFromDatabase(context, username);
                var maNguoiDung = int.Parse(HttpContext.Session.GetString("UserID"));

                if (!string.IsNullOrWhiteSpace(Phone))
                {
                    var nguoiDung = context.NguoiDungs.Find(maNguoiDung);
                    if (nguoiDung != null)
                    {
                        nguoiDung.Sdt = Phone.Trim();
                    }
                }

                // Tính tổng tiền
                double tongTien = gioHang.ChiTietGioHangs.Sum(x => (double)(x.TongTien ?? 0));
                double tongTienGoc = tongTien;
                string maKhuyenMaiSuDung = null;

                // Kiểm tra và áp dụng voucher nếu có
                if (!string.IsNullOrWhiteSpace(voucherCode))
                {
                    var khuyenMai = context.KhuyenMais.FirstOrDefault(km => km.MaKhuyenMai == voucherCode);
                    if (khuyenMai != null && khuyenMai.TrangThai && khuyenMai.SoLuong > 0)
                    {
                        // Kiểm tra thời gian và điều kiện
                        var now = DateTime.Now;
                        if (now >= khuyenMai.ThoiGianBatDau && 
                            now <= khuyenMai.ThoiGianKetThuc &&
                            tongTien >= (khuyenMai.DieuKienApDung ?? 0))
                        {
                            maKhuyenMaiSuDung = voucherCode;
                            // Tính tiền giảm và trừ vào tổng tiền
                            double tienGiam = tongTien * (khuyenMai.GiaTri / 100.0);
                            tongTien = Math.Max(0, tongTien - tienGiam);
                            // Giảm số lượng voucher
                            khuyenMai.SoLuong -= 1;
                        }
                    }
                }

                // Cộng phí giao hàng vào tổng tiền
                if (shippingFee > 0)
                {
                    tongTien += shippingFee;
                }

                // Tạo đối tượng DonHang
                var donHang = new DonHang
                {
                    MaDh = maDonHang,
                    Username = username,
                    MaKhuyenMai = maKhuyenMaiSuDung,
                    Diachi = DiaChi,
                    TongTien = tongTien,
                    SoLuong = (int)gioHang.ChiTietGioHangs.Sum(x => x.SoLuongSp),
                    TrangThai = trangThai,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    MaNguoiDung = maNguoiDung
                };

                // Thêm đơn hàng vào cơ sở dữ liệu
                context.DonHangs.Add(donHang);
                context.ThanhToans.Add(new ThanhToan
                {
                    MaDh = maDonHang,
                    PhuongThucThanhToan = "Thanh toán khi nhận hàng",
                    NgayThanhToan = DateTime.Now,
                    TongTien = tongTien,
                    TrangThaiThanhToan = true
                });

                // Lưu từng sản phẩm trong giỏ hàng vào chi tiết đơn hàng
                foreach (var item in gioHang.ChiTietGioHangs)
                {
                    var chiTiet = new ChiTietDonHang
                    {
                        MaDh = maDonHang,
                        MaSp = (int)item.MaSp,
                        SoLuong = (int)item.SoLuongSp,
                        TongTien = (int)item.TongTien
                    };
                    var sanPham = context.SanPhams.FirstOrDefault(sp => sp.MaSp == item.MaSp);
                    if (sanPham != null)
                    {
                        sanPham.SlbanTrongNgay -= item.SoLuongSp;
                        if (sanPham.SlbanTrongNgay < 0)
                        {
                            sanPham.SlbanTrongNgay = 0;
                        }
                    }
                    context.ChiTietDonHangs.Add(chiTiet);
                }

                // Lưu tất cả thay đổi
                context.SaveChanges();
            }

            // Xóa giỏ hàng sau khi thanh toán thành công
            var firstPurchasedItem = gioHang.ChiTietGioHangs.FirstOrDefault();
            if (firstPurchasedItem?.MaSpNavigation != null)
            {
                // UI trigger data for review overlay (one-time per order on homepage)
                TempData["ReviewOrderId"] = maDonHang;
                TempData["ReviewProductId"] = firstPurchasedItem.MaSpNavigation.MaSp;
                TempData["ReviewProductName"] = firstPurchasedItem.MaSpNavigation.TenSp;
                TempData["ReviewProductImage"] = firstPurchasedItem.MaSpNavigation.HinhAnh1 ?? "";
            }

            ClearCart();
            TempData["Message"] = "Thanh toán thành công! Cảm ơn bạn đã mua hàng.";

            return RedirectToAction("TrangChu", "SanPhams");
        }





        // Phương thức lấy trạng thái đơn hàng từ database
        private string GetOrderStatusFromDatabase(QlbanDoAnNhanh3Context context, string username)
        {
            // Kiểm tra xem người dùng đã có đơn hàng trước đó chưa
            var hasPreviousOrders = context.DonHangs.Any(d => d.Username == username);

            // Nếu có đơn hàng trước đó thì đặt là "Đang xử lý", ngược lại là "Mới"
            return hasPreviousOrders ? "Chưa Giao" : "Chưa Giao";
        }
        // Phương thức hiển thị chi tiết đơn hàng

        public IActionResult OrderHistory()
        {
            // Kiểm tra xem người dùng đã đăng nhập hay chưa
            var username = HttpContext.Session.GetString("userLogin");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Message"] = "Vui lòng đăng nhập để xem lịch sử đơn hàng!";
                return RedirectToAction("Login", "User"); // Điều hướng đến trang đăng nhập
            }

            using (var context = new QlbanDoAnNhanh3Context())
            {
                // Lấy danh sách đơn hàng của người dùng
                var donHangs = context.DonHangs
                                      .Include(dh => dh.ChiTietDonHangs) // Include để lấy cả chi tiết đơn hàng
                                      .ThenInclude(ct => ct.MaSpNavigation) // Include để lấy thông tin sản phẩm
                                      .Where(dh => dh.Username == username)
                                      .OrderByDescending(dh => dh.CreatedAt) // Sắp xếp theo ngày tạo mới nhất
                                      .ToList();

                return View(donHangs);
            }
        }
        public IActionResult OrderDetails(string maDh)
        {
            using (var context = new QlbanDoAnNhanh3Context())
            {
                var donHang = context.DonHangs
                                    .Include(dh => dh.ChiTietDonHangs)
                                    .ThenInclude(ct => ct.MaSpNavigation)
                                    .Include(dh => dh.MaNguoiDungNavigation)
                                    .Include(dh => dh.MaKhuyenMaiNavigation)
                                    .Include(dh => dh.ThanhToans)
                                    .FirstOrDefault(dh => dh.MaDh == maDh);

                if (donHang == null)
                {
                    return NotFound(); // Trả về lỗi 404 nếu không tìm thấy đơn hàng
                }

                return View(donHang);
            }

        }
        // Hủy đơn hàng
        public IActionResult CancelOrder(string maDh)
        {
            using (var context = new QlbanDoAnNhanh3Context())
            {
                // Tìm đơn hàng theo mã đơn hàng và bao gồm ChiTietDonHangs
                var donHang = context.DonHangs
                    .Include(dh => dh.ChiTietDonHangs)
                    .FirstOrDefault(dh => dh.MaDh == maDh);

                if (donHang == null)
                {
                    TempData["Message"] = "Không tìm thấy đơn hàng để hủy!";
                    return RedirectToAction("OrderHistory"); // Quay lại lịch sử đơn hàng
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThai = "Đã Hủy";
                context.SaveChanges();

                // Cập nhật lại số lượng sản phẩm trong kho
                foreach (var chiTiet in donHang.ChiTietDonHangs)
                {
                    var sanPham = context.SanPhams.FirstOrDefault(sp => sp.MaSp == chiTiet.MaSp);
                    if (sanPham != null)
                    {
                        sanPham.SlbanTrongNgay += chiTiet.SoLuong; // Trả lại số lượng sản phẩm vào kho
                    }
                }

                context.SaveChanges(); // Lưu thay đổi vào cơ sở dữ liệu

                TempData["Message"] = "Đơn hàng đã được hủy thành công.";
                return RedirectToAction("OrderHistory"); // Quay lại lịch sử đơn hàng
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(decimal amount, [FromServices] PayPalService payPalService, string DiaChi, string Phone, string trangThai, string voucherCode = null, decimal shippingFee = 0)
        {
            // Kiểm tra xem người dùng đã đăng nhập hay chưa
            var username = HttpContext.Session.GetString("userLogin");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Message"] = "Vui lòng đăng nhập trước khi thanh toán!";
                return RedirectToAction("Login", "User"); // Điều hướng đến trang đăng nhập
            }

            var gioHang = GetGioHangFromSession(); // Lấy giỏ hàng từ session
            if (!gioHang.ChiTietGioHangs.Any())
            {
                TempData["Message"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index");
            }

            // Tính toán tổng số tiền và số lượng từ giỏ hàng
            decimal tongTien = (decimal)gioHang.ChiTietGioHangs.Sum(x => (double)(x.TongTien ?? 0));
            decimal tongTienThanhToan = tongTien;
            int soLuong = (int)gioHang.ChiTietGioHangs.Sum(x => x.SoLuongSp);
            var maDonHang = _context.DonHangs.Max(d => d.MaDh) + 1;
            var maNguoiDung = int.Parse(HttpContext.Session.GetString("UserID"));
            string maKhuyenMaiSuDung = null;

            // Kiểm tra và áp dụng voucher nếu có
            if (!string.IsNullOrWhiteSpace(voucherCode))
            {
                var khuyenMai = _context.KhuyenMais.FirstOrDefault(km => km.MaKhuyenMai == voucherCode);
                if (khuyenMai != null && khuyenMai.TrangThai && khuyenMai.SoLuong > 0)
                {
                    // Kiểm tra thời gian và điều kiện
                    var now = DateTime.Now;
                    if (now >= khuyenMai.ThoiGianBatDau && 
                        now <= khuyenMai.ThoiGianKetThuc &&
                        (decimal)tongTien >= (decimal)(khuyenMai.DieuKienApDung ?? 0))
                    {
                        maKhuyenMaiSuDung = voucherCode;
                        // Tính tiền giảm và trừ vào tổng tiền
                        decimal tienGiam = tongTien * (khuyenMai.GiaTri / 100m);
                        tongTienThanhToan = Math.Max(0, tongTien - tienGiam);
                        // Giảm số lượng voucher
                        khuyenMai.SoLuong -= 1;
                    }
                }
            }

            // Cộng phí giao hàng vào tổng tiền
            if (shippingFee > 0)
            {
                tongTienThanhToan += shippingFee;
            }

            if (!string.IsNullOrWhiteSpace(Phone))
            {
                var nguoiDung = await _context.NguoiDungs.FindAsync(maNguoiDung);
                if (nguoiDung != null)
                {
                    nguoiDung.Sdt = Phone.Trim();
                }
            }

            // Tạo đơn hàng mới
            var donHang = new DonHang
            {

                MaDh = maDonHang,  // Set MaDh manually
                Username = username,
                MaKhuyenMai = maKhuyenMaiSuDung,
                Diachi = DiaChi,
                TongTien = (double)tongTienThanhToan,
                SoLuong = soLuong,
                TrangThai = "Chưa Giao",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                MaNguoiDung = maNguoiDung
            };

            // Thêm đơn hàng vào cơ sở dữ liệu
            _context.DonHangs.Add(donHang);
            _context.ThanhToans.Add(new ThanhToan
            {
                MaDh = donHang.MaDh,
                PhuongThucThanhToan = "PayPal",
                NgayThanhToan = DateTime.Now,
                TongTien = (double)tongTienThanhToan,
                TrangThaiThanhToan = false
            });
            await _context.SaveChangesAsync();

            // Lưu chi tiết đơn hàng
            foreach (var item in gioHang.ChiTietGioHangs)
            {
                var chiTiet = new ChiTietDonHang
                {
                    MaDh = donHang.MaDh, // Lấy MaDh từ đối tượng DonHang vừa tạo
                    MaSp = (int)item.MaSp,
                    SoLuong = (int)item.SoLuongSp,
                    TongTien = (double)item.TongTien
                };

                _context.ChiTietDonHangs.Add(chiTiet);
            }

            // Lưu các chi tiết đơn hàng vào cơ sở dữ liệu
            await _context.SaveChangesAsync();

            try
            {
                // Tạo đơn hàng trên PayPal với số tiền đã trừ discount
                var approvalLink = await payPalService.CreateOrderAsync(tongTienThanhToan, "USD");

                if (!string.IsNullOrEmpty(approvalLink))
                {
                    return Redirect(approvalLink); // Chuyển hướng đến PayPal để duyệt thanh toán
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Thanh toán thất bại!";
                return BadRequest($"Error creating payment: {ex.Message}");
            }
           
            return BadRequest("Unable to create PayPal payment.");
        }




        public async Task<IActionResult> PaymentFailure()
        {
            // Kiểm tra thông tin từ PayPal
            var token = HttpContext.Request.Query["token"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                TempData["Message"] = "Giao dịch đã bị huỷ. Vui lòng thử lại.";
                return RedirectToAction("TrangChu", "SanPhams");
            }

            // Thực hiện kiểm tra với PayPal để xác nhận trạng thái
            var capturedOrder = await _payPalService.CaptureOrderAsync(token);

            if (capturedOrder.Status != "COMPLETED")
            {
                TempData["Message"] = "Giao dịch tạm hoãn. Bạn có thể thử lại.";
            }
            else
            {
                TempData["Message1"] = "Giao dịch đã hoàn tất!";
            }

            return RedirectToAction("TrangChu", "SanPhams");
        }

    }
}
