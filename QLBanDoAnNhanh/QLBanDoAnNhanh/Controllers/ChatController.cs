using Microsoft.AspNetCore.Mvc;

namespace QLBanDoAnNhanh.Controllers
{
    public class ChatController : Controller
    {
        public IActionResult Chat()
        {
            // Lấy userLogin và quyền admin từ session và truyền vào ViewBag
            ViewBag.UserLogin = HttpContext.Session.GetString("userLogin");
            ViewBag.IsAdmin = HttpContext.Session.GetString("adminLogin") != null; // Kiểm tra nếu là admin
            return View();
        }

        public IActionResult ChatAdmin()
        {
            // Truyền userLogin và quyền admin cho giao diện admin
            ViewBag.UserLogin = HttpContext.Session.GetString("userLogin");
            ViewBag.IsAdmin = HttpContext.Session.GetString("adminLogin") != null;
            return View();
        }
    }
}
