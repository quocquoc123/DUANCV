using Microsoft.AspNetCore.Mvc;

namespace QLBanDoAnNhanh.Controllers
{
    public class ChatController : Controller
    {
        public IActionResult Chat()
        {
            var username = HttpContext.Session.GetString("userLogin");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Message"] = "Vui lòng đăng nhập trước khi thanh toán!";
                return RedirectToAction("Login", "User"); // Điều hướng đến trang đăng nhập
            }
            // Lấy userLogin và quyền admin từ session và truyền vào ViewBag
            ViewBag.UserLogin = HttpContext.Session.GetString("userLogin");
            ViewBag.IsAdmin = HttpContext.Session.GetString("adminLogin") != null; // Kiểm tra nếu là admin
            return View();
        }

        public IActionResult ChatAdmin()
        {
            var username = HttpContext.Session.GetString("userLogin");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Message"] = "Vui lòng đăng nhập trước khi thanh toán!";
                return RedirectToAction("Login", "User"); // Điều hướng đến trang đăng nhập
            }
            // Truyền userLogin và quyền admin cho giao diện admin
            ViewBag.UserLogin = HttpContext.Session.GetString("userLogin");
            ViewBag.IsAdmin = HttpContext.Session.GetString("adminLogin") != null;
            return View();
        }
    }
}
