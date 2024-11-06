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
    public class GiamGiumsController : Controller
    {
        private readonly QlbanDoAnNhanh3Context _context;

        public GiamGiumsController(QlbanDoAnNhanh3Context context)
        {
            _context = context;
        }

        // GET: GiamGiums
        public async Task<IActionResult> Index()
        {
            return View(await _context.GiamGia.ToListAsync());
        }

        // GET: GiamGiums/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var giamGium = await _context.GiamGia
                .FirstOrDefaultAsync(m => m.MaGiamGia == id);
            if (giamGium == null)
            {
                return NotFound();
            }

            return View(giamGium);
        }

        // GET: GiamGiums/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: GiamGiums/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaGiamGia,GiaTri")] GiamGium giamGium)
        {
            if (ModelState.IsValid)
            {
                _context.Add(giamGium);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(giamGium);
        }

        // GET: GiamGiums/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var giamGium = await _context.GiamGia.FindAsync(id);
            if (giamGium == null)
            {
                return NotFound();
            }
            return View(giamGium);
        }

        // POST: GiamGiums/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaGiamGia,GiaTri")] GiamGium giamGium)
        {
            if (id != giamGium.MaGiamGia)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(giamGium);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GiamGiumExists(giamGium.MaGiamGia))
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
            return View(giamGium);
        }

        // GET: GiamGiums/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var giamGium = await _context.GiamGia
                .FirstOrDefaultAsync(m => m.MaGiamGia == id);
            if (giamGium == null)
            {
                return NotFound();
            }

            return View(giamGium);
        }

        // POST: GiamGiums/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var giamGium = await _context.GiamGia.FindAsync(id);
            if (giamGium != null)
            {
                _context.GiamGia.Remove(giamGium);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GiamGiumExists(int id)
        {
            return _context.GiamGia.Any(e => e.MaGiamGia == id);
        }
    }
}
