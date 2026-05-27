using Microsoft.AspNetCore.Mvc;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Controllers
{
    public class BaoCaoUserController : Controller
    {
        private readonly QlbanDoAnNhanh3Context _context;

        public BaoCaoUserController(QlbanDoAnNhanh3Context context)
        {
            _context = context;
        }


        // GET: BaoCao/SubmitFeedback
        public IActionResult SubmitFeedback()
        {

            var username = HttpContext.Session.GetString("userLogin");
            if (username == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Lấy người dùng dựa trên Username
            var user = _context.NguoiDungs.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Lấy toàn bộ phản hồi và trả lời liên quan đến người dùng
            var feedbackHistory = _context.PhanHoiBaoCaos
                                        .Where(ph => ph.MaNguoiDung == user.MaNguoiDung ||
                                                     ph.MaBaoCaoNavigation.MaNguoiDung == user.MaNguoiDung)
                                        .OrderByDescending(ph => ph.NgayPhanHoi)
                                        .ToList();

            return View(feedbackHistory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitFeedback(string feedbackContent)
        {
            // Kiểm tra nếu người dùng đã đăng nhập và nội dung phản hồi không rỗng
            var username = HttpContext.Session.GetString("userLogin");
            if (username != null && !string.IsNullOrEmpty(feedbackContent))
            {
                // Tìm người dùng dựa trên Username từ session
                var user = _context.NguoiDungs.FirstOrDefault(u => u.Username == username);
                if (user == null)
                {
                    return RedirectToAction("Login", "User");
                }

                // Tạo mới báo cáo người dùng
                var report = new BaoCaoNguoiDung
                {
                    MaNguoiDung = user.MaNguoiDung, // Sử dụng MaNguoiDung từ đối tượng người dùng
                    NgayTao = DateTime.Now,
                    TrangThai = 0 // Đặt trạng thái là chưa xử lý
                };
                _context.BaoCaoNguoiDungs.Add(report);
                _context.SaveChanges();

                // Lưu phản hồi của người dùng vào bảng PhanHoiBaoCao
                var feedback = new PhanHoiBaoCao
                {
                    MaBaoCao = report.MaBaoCao,
                    MaNguoiDung = report.MaNguoiDung,
                    NoiDung = feedbackContent,
                    NguoiTraLoi = false, // false biểu thị phản hồi từ người dùng
                    NgayPhanHoi = DateTime.Now
                };
                _context.PhanHoiBaoCaos.Add(feedback);
                _context.SaveChanges();

                // Điều hướng đến trang thông báo thành công
                return RedirectToAction("SubmitFeedback");
            }
            return RedirectToAction("Login", "User");
        }


        public IActionResult FeedbackSuccess()
        {
            return View();
        }

        public IActionResult FeedbackHistory()
        {
            var username = HttpContext.Session.GetString("userLogin");
            if (username == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Lấy người dùng dựa trên Username
            var user = _context.NguoiDungs.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Lấy toàn bộ phản hồi và trả lời liên quan đến người dùng
            var feedbackHistory = _context.PhanHoiBaoCaos
                                        .Where(ph => ph.MaNguoiDung == user.MaNguoiDung ||
                                                     ph.MaBaoCaoNavigation.MaNguoiDung == user.MaNguoiDung)
                                        .OrderByDescending(ph => ph.NgayPhanHoi)
                                        .ToList();

            return View(feedbackHistory);
        }
    }
}
