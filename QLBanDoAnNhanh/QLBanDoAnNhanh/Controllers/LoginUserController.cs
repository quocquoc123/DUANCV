using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Controllers
{
    public class LoginUserController : Controller
    {
        private QlbanDoAnNhanh3Context db = new QlbanDoAnNhanh3Context();

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> LoginAccount(NguoiDung _user)
        {
            var check = await db.NguoiDungs
                .Where(s => s.Email == _user.Email && s.Matkhau == _user.Matkhau)
                .FirstOrDefaultAsync();

            if (check == null)
            {
                ViewBag.ErrorInfo = "Invalid Login Info";
                return View("Index");
            }
            else
            {
                // Assuming session uses a service in ASP.NET Core
                HttpContext.Session.SetString("Email", _user.Email);
                HttpContext.Session.SetString("MatKhau", _user.Matkhau);

                if (check.RoleId == 1) // Admin role
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return RedirectToAction("TrangChu", "SanPhams");
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> RegisterUser(NguoiDung _user)
        {
            if (ModelState.IsValid)
            {
                var check_ID = await db.NguoiDungs
                    .Where(s => s.Email == _user.Email)
                    .FirstOrDefaultAsync();

                if (check_ID == null)
                {
                    _user.RoleId = 2; // Regular user by default
                    db.NguoiDungs.Add(_user);
                    await db.SaveChangesAsync();

                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.ErrorRegister = "This Email already exists";
                    return View();
                }
            }
            return View();
        }

        public IActionResult LogOutUser()
        {
            HttpContext.Session.Clear(); // Clear the session
            return RedirectToAction("TrangChu", "SanPhams");
        }

    }
}
