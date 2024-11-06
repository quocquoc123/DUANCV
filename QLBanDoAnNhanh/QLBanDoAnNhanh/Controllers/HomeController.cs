using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;
using System.Diagnostics;

namespace QLBanDoAnNhanh.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly QlbanDoAnNhanh3Context _context;
        private QlbanDoAnNhanh3Context db = new QlbanDoAnNhanh3Context();

        public HomeController(ILogger<HomeController> logger, QlbanDoAnNhanh3Context context)
        {
            _logger = logger;
            _context = context; // Kh?i t?o DbContext thông qua Dependency Injection
        }

        public IActionResult Index()
        {
            var products = _context.SanPhams.ToList(); // L?y danh sách s?n ph?m
            return View(products); // Truy?n danh sách s?n ph?m vào view
        }





        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
