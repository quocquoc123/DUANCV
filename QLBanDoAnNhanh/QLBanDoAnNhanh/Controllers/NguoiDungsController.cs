using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Controllers
{
    public class NguoiDungsController : Controller
    {
        private QlbanDoAnNhanh3Context db = new QlbanDoAnNhanh3Context();

        private readonly QlbanDoAnNhanh3Context _context;

        public NguoiDungsController(QlbanDoAnNhanh3Context context)
        {
            _context = context;
        }

        // GET: NguoiDungs
        public async Task<IActionResult> Index()
        {
            var QlbanDoAnNhanh3Context = _context.NguoiDungs.Include(n => n.Role);
            return View(await QlbanDoAnNhanh3Context.ToListAsync());
        }

        // GET: NguoiDungs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nguoiDung = await _context.NguoiDungs
                .Include(n => n.Role)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);
            if (nguoiDung == null)
            {
                return NotFound();
            }

            return View(nguoiDung);
        }

        // GET: NguoiDungs/Create
        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(_context.PhanQuyens, "RoleId", "RoleId");
            return View();
        }

        // POST: NguoiDungs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNguoiDung,Username,TrangThai,HoTen,Email,Sdt,Matkhau,RoleId")] NguoiDung nguoiDung)
        {
            //if (ModelState.IsValid)
            //{
                _context.Add(nguoiDung);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            
            ViewData["RoleId"] = new SelectList(_context.PhanQuyens, "RoleId", "RoleId", nguoiDung.RoleId);
            return View(nguoiDung);
        }

        // GET: NguoiDungs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            if (nguoiDung == null)
            {
                return NotFound();
            }
            ViewData["RoleId"] = new SelectList(_context.PhanQuyens, "RoleId", "RoleId", nguoiDung.RoleId);
            return View(nguoiDung);
        }

        // POST: NguoiDungs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaNguoiDung,Username,TrangThai,HoTen,Email,Sdt,Matkhau,RoleId")] NguoiDung nguoiDung)
        {
            if (id != nguoiDung.MaNguoiDung)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {


                try
                {
                    _context.Update(nguoiDung);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NguoiDungExists(nguoiDung.MaNguoiDung))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;

                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoleId"] = new SelectList(_context.PhanQuyens, "RoleId", "RoleId", nguoiDung.RoleId);
            return View(nguoiDung);
        }

        // GET: NguoiDungs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nguoiDung = await _context.NguoiDungs
                .Include(n => n.Role)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);
            if (nguoiDung == null)
            {
                return NotFound();
            }

            return View(nguoiDung);
        }

        // POST: NguoiDungs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            if (nguoiDung != null)
            {
                _context.NguoiDungs.Remove(nguoiDung);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NguoiDungExists(int id)
        {
            return _context.NguoiDungs.Any(e => e.MaNguoiDung == id);
        }
        public ActionResult Search(string searchTerm)
        {

            if (string.IsNullOrWhiteSpace(searchTerm))
            {

                return RedirectToAction("Index");
            }


            var searchTermLower = searchTerm.ToLower();

            var searchResults = db.NguoiDungs
                .Where(p => p.Username.ToLower().Contains(searchTermLower))
                .ToList();
            ViewBag.SearchTerm = searchTerm;
            return View("Index", searchResults);
        }
        // Phương thức GET để xác nhận việc vô hiệu hóa tài khoản
        public async Task<IActionResult> Disable(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            if (nguoiDung == null)
            {
                return NotFound();
            }

            return View(nguoiDung); // Hiển thị view để xác nhận việc vô hiệu hóa
        }

        // Phương thức POST để thực hiện việc vô hiệu hóa
        [HttpPost, ActionName("Disable")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableConfirmed(int id)
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            if (nguoiDung != null)
            {
                nguoiDung.TrangThai = "inactive"; // Đặt trạng thái thành không kích hoạt
                _context.Update(nguoiDung);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        public IActionResult EnableAccount(int id)
        {
            var nguoiDung = _context.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return NotFound();
            }

            nguoiDung.TrangThai = "active"; // Cập nhật trạng thái
            _context.Update(nguoiDung);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        public IActionResult DisableAccount(int id)
        {
            var nguoiDung = _context.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return NotFound();
            }

            // Đặt trạng thái tài khoản thành không hoạt động
            nguoiDung.TrangThai = "inactive"; // Hoặc giá trị phù hợp với hệ thống của bạn
            _context.Update(nguoiDung);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }

}
