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

    public class BinhLuansController : Controller
    {
        private QlbanDoAnNhanh3Context db = new QlbanDoAnNhanh3Context();
        private readonly QlbanDoAnNhanh3Context _context;

        public BinhLuansController(QlbanDoAnNhanh3Context context)
        {
            _context = context;
        }

        // GET: BinhLuans
        public async Task<IActionResult> Index()
        {
            var QlbanDoAnNhanh3Context = _context.BinhLuans.Include(b => b.MaNguoiDungNavigation).Include(b => b.MaSpNavigation);
            return View(await QlbanDoAnNhanh3Context.ToListAsync());
        }

        // GET: BinhLuans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var binhLuan = await _context.BinhLuans
                .Include(b => b.MaNguoiDungNavigation)
                .Include(b => b.MaSpNavigation)
                .FirstOrDefaultAsync(m => m.MaBinhLuan == id);
            if (binhLuan == null)
            {
                return NotFound();
            }

            return View(binhLuan);
        }

        // GET: BinhLuans/Create
        public IActionResult Create()
        {
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung");
            ViewData["MaSp"] = new SelectList(_context.SanPhams, "MaSp", "MaSp");
            return View();
        }

        // POST: BinhLuans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaBinhLuan,MaSp,MaNguoiDung,NoiDung,NgayBinhLuan")] BinhLuan binhLuan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(binhLuan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", binhLuan.MaNguoiDung);
            ViewData["MaSp"] = new SelectList(_context.SanPhams, "MaSp", "MaSp", binhLuan.MaSp);
            return View(binhLuan);
        }

        // GET: BinhLuans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var binhLuan = await _context.BinhLuans.FindAsync(id);
            if (binhLuan == null)
            {
                return NotFound();
            }
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", binhLuan.MaNguoiDung);
            ViewData["MaSp"] = new SelectList(_context.SanPhams, "MaSp", "MaSp", binhLuan.MaSp);
            return View(binhLuan);
        }

        // POST: BinhLuans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaBinhLuan,MaSp,MaNguoiDung,NoiDung,NgayBinhLuan")] BinhLuan binhLuan)
        {
            if (id != binhLuan.MaBinhLuan)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(binhLuan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BinhLuanExists(binhLuan.MaBinhLuan))
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
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "MaNguoiDung", binhLuan.MaNguoiDung);
            ViewData["MaSp"] = new SelectList(_context.SanPhams, "MaSp", "MaSp", binhLuan.MaSp);
            return View(binhLuan);
        }

        // GET: BinhLuans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var binhLuan = await _context.BinhLuans
                .Include(b => b.MaNguoiDungNavigation)
                .Include(b => b.MaSpNavigation)
                .FirstOrDefaultAsync(m => m.MaBinhLuan == id);
            if (binhLuan == null)
            {
                return NotFound();
            }

            return View(binhLuan);
        }

        // POST: BinhLuans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var binhLuan = await _context.BinhLuans.FindAsync(id);
            if (binhLuan != null)
            {
                _context.BinhLuans.Remove(binhLuan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BinhLuanExists(int id)
        {
            return _context.BinhLuans.Any(e => e.MaBinhLuan == id);
        }
        public ActionResult Search(string searchTerm)
        {

            if (string.IsNullOrWhiteSpace(searchTerm))
            {

                return RedirectToAction("Index");
            }


            var searchTermLower = searchTerm.ToLower();

            var searchResults = db.BinhLuans
                .Where(p => p.NoiDung.ToLower().Contains(searchTermLower))
                .ToList();
            ViewBag.SearchTerm = searchTerm;
            return View("Index", searchResults);
        }
    }
}
