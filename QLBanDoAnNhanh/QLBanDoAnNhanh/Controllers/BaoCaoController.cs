using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Controllers
{
    public class BaoCaoController : Controller
    {
        private readonly QlbanDoAnNhanh3Context _context;

        public BaoCaoController(QlbanDoAnNhanh3Context context)
        {
            _context = context;
        }

        // List all feedback reports
        public IActionResult ManageReports()
        {
            if (HttpContext.Session.GetString("userLogin") != null)
            {
                // Sử dụng Include với đối tượng điều hướng NguoiDung
                var reports = _context.BaoCaoNguoiDungs
                    .Include(bc => bc.NguoiDung) // Bao gồm NguoiDung liên kết
                    .ToList();
                return View(reports);
            }
            return RedirectToAction("Login", "User", new { area = "" });
        }


        // Display feedback details for responding
        public IActionResult RespondReport(int id)
        {
            if (HttpContext.Session.GetString("userLogin") == null)
            {
                return RedirectToAction("Login", "User", new { area = "" });
            }

            // Load báo cáo cùng với phản hồi (PhanHoiBaoCaos)
            var report = _context.BaoCaoNguoiDungs
                                 .Include(b => b.PhanHoiBaoCaos) // Include phản hồi báo cáo
                                 .ThenInclude(p => p.MaNguoiDungNavigation) // Nếu cần thông tin người dùng phản hồi
                                 .FirstOrDefault(b => b.MaBaoCao == id);

            if (report == null)
            {
                return NotFound(); // Trả về lỗi nếu không tìm thấy báo cáo
            }

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RespondReport(int id, string responseMessage)
        {
            var username = HttpContext.Session.GetString("userLogin");
            if (username != null && !string.IsNullOrEmpty(responseMessage))
            {
                try
                {
                    // Lấy MaNguoiDung từ username
                    var user = _context.NguoiDungs.FirstOrDefault(u => u.Username == username);
                    if (user == null)
                    {
                        return RedirectToAction("Login", "User", new { area = "" });
                    }

                    var feedback = new PhanHoiBaoCao
                    {
                        MaBaoCao = id,
                        MaNguoiDung = user.MaNguoiDung,
                        NoiDung = responseMessage,
                        NguoiTraLoi = true,
                        NgayPhanHoi = DateTime.Now
                    };

                    // Thêm phản hồi vào bảng PhanHoiBaoCao
                    _context.PhanHoiBaoCaos.Add(feedback);

                    // Lấy báo cáo và cập nhật trạng thái
                    var report = _context.BaoCaoNguoiDungs.Find(id);
                    if (report != null)
                    {
                        report.TrangThai = 1; // Đặt trạng thái thành đã xử lý
                        _context.SaveChanges(); // Lưu thay đổi vào cơ sở dữ liệu
                    }

                    return RedirectToAction("ManageReports");
                }
                catch (Exception)
                {
                    ViewBag.Error = "Đã có lỗi xảy ra khi trả lời phản hồi.";
                    return View();
                }
            }
            return RedirectToAction("Login", "User", new { area = "" });
        }
    }
}
