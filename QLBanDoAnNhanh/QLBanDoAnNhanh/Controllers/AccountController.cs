using Microsoft.AspNetCore.Mvc;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Controllers
{
    public class AccountController : Controller
    {
        private readonly QlbanDoAnNhanh3Context _context;
        //private readonly Common _common;

        public IActionResult Index()
        {
            return View();
        }
    }
}
