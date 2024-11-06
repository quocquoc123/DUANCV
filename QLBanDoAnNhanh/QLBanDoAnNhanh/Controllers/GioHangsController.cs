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

    namespace QLBanDoAnNhanh.Controllers
    {
    public class GioHangsController : Controller
    {
        private readonly QlbanDoAnNhanh3Context _context;
        private GioHang GetGioHangFromSession()
        {
            var gioHang = HttpContext.Session.GetObjectFromJson<GioHang>("GioHang");
            if (gioHang == null)
            {
                gioHang = new GioHang();
            }
            return gioHang;
        }

        // Tính tổng số sản phẩm trong giỏ hàng và cập nhật ViewBag
        private void UpdateCartItemCount(GioHang gioHang)
        {
            ViewBag.CartItemCount = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuongSp);

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
                var sanPham = context.SanPhams.FirstOrDefault(sp => sp.MaSp == MaSp);
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
                    chiTietGioHang = NewMethod(MaSp, quantity, sanPham);
                    gioHang.ChiTietGioHangs.Add(chiTietGioHang);
                }
                else
                {
                    // Nếu sản phẩm đã có trong giỏ, cập nhật số lượng và tổng tiền
                    chiTietGioHang.SoLuongSp += quantity;
                    chiTietGioHang.TongTien = (int)(chiTietGioHang.SoLuongSp * sanPham.GiaTien);
                }

                // Lưu lại giỏ hàng vào session
                SaveGioHangToSession(gioHang);
                UpdateCartItemCount(gioHang);
            }

            // Điều hướng về trang giỏ hàng
            return RedirectToAction("Index");
        }

        private static ChiTietGioHang NewMethod(int MaSp, int quantity, SanPham sanPham)
        {
            return new ChiTietGioHang
            {
                MaSp = MaSp,
                MaSpNavigation = sanPham,
                SoLuongSp = quantity,
           
                TongTien = (int)(quantity * sanPham.GiaTien)
            };
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var gioHang = GetGioHangFromSession();
            UpdateCartItemCount(gioHang);
            return View(gioHang); // Trả về View để hiển thị giỏ hàng
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
                chiTietGioHang.TongTien = (int)(quantity * chiTietGioHang.MaSpNavigation.GiaTien);
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
        public IActionResult Checkout(string DiaChi)
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
                // Tạo đối tượng DonHang
                var donHang = new DonHang
                {
                    MaDh = maDonHang,
                    Username = username,
                    MaKhuyenMai = "1", // Giả sử có mã khuyến mãi mặc định
                    Diachi = DiaChi,
                    TongTien = gioHang.ChiTietGioHangs.Sum(x => (double)(x.TongTien ?? 0)),
                    SoLuong = (int)gioHang.ChiTietGioHangs.Sum(x => x.SoLuongSp),
                    TrangThai = trangThai,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    MaNguoiDung = int.Parse(HttpContext.Session.GetString("UserID"))
                };

                // Thêm đơn hàng vào cơ sở dữ liệu
                context.DonHangs.Add(donHang);

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
            return hasPreviousOrders ? "Đang xử lý" : "Mới";
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
                // Tìm đơn hàng theo mã đơn hàng
                var donHang = context.DonHangs.FirstOrDefault(dh => dh.MaDh == maDh);
                
                if (donHang == null)
                {
                    TempData["Message"] = "Không tìm thấy đơn hàng để hủy!";
                    return RedirectToAction("OrderHistory"); // Quay lại lịch sử đơn hàng
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThai = "Đã hủy"; // Hoặc cập nhật thêm thuộc tính IsCanceled nếu có
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

                context.SaveChanges();
                TempData["Message"] = "Đơn hàng đã được hủy thành công.";
                return RedirectToAction("OrderHistory"); // Quay lại lịch sử đơn hàng
            }
        }

       
    }
}
