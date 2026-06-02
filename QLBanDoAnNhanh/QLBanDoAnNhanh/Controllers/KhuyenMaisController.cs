﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Controllers
{
    public class KhuyenMaisController : Controller
    {
        private readonly QlbanDoAnNhanh3Context _context;

        public KhuyenMaisController(QlbanDoAnNhanh3Context context)
        {
            _context = context;
        }

        // GET: KhuyenMais
        public async Task<IActionResult> Index()
        {
            return View(await _context.KhuyenMais.AsNoTracking().ToListAsync());
        }

        // GET: KhuyenMais/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khuyenMai = await _context.KhuyenMais
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaKhuyenMai == id);
            if (khuyenMai == null)
            {
                return NotFound();
            }

            return View(khuyenMai);
        }

        // GET: KhuyenMais/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: KhuyenMais/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaKhuyenMai,GiaTri,ThoiGianBatDau,ThoiGianKetThuc,TrangThai,DieuKienApDung,SoLuong")] KhuyenMai khuyenMai)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mã khuyến mãi đã tồn tại
                if (await _context.KhuyenMais.AnyAsync(x => x.MaKhuyenMai == khuyenMai.MaKhuyenMai))
                {
                    ModelState.AddModelError("MaKhuyenMai", "Mã khuyến mãi này đã tồn tại!");
                    return View(khuyenMai);
                }

                // Kiểm tra thời gian
                if (khuyenMai.ThoiGianBatDau >= khuyenMai.ThoiGianKetThuc)
                {
                    ModelState.AddModelError("ThoiGianKetThuc", "Thời gian kết thúc phải sau thời gian bắt đầu!");
                    return View(khuyenMai);
                }

                // Kiểm tra giá trị giảm
                if (khuyenMai.GiaTri <= 0 || khuyenMai.GiaTri > 100)
                {
                    ModelState.AddModelError("GiaTri", "Giá trị giảm phải từ 1-100!");
                    return View(khuyenMai);
                }

                // Kiểm tra số lượng
                if (khuyenMai.SoLuong <= 0)
                {
                    ModelState.AddModelError("SoLuong", "Số lượng phải lớn hơn 0!");
                    return View(khuyenMai);
                }

                // Set ngày tạo
                khuyenMai.NgayTao = DateTime.Now;

                _context.Add(khuyenMai);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(khuyenMai);
        }

        // GET: KhuyenMais/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai == null)
            {
                return NotFound();
            }
            return View(khuyenMai);
        }

        // POST: KhuyenMais/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaKhuyenMai,GiaTri,ThoiGianBatDau,ThoiGianKetThuc,TrangThai,DieuKienApDung,SoLuong")] KhuyenMai khuyenMai)
        {
            if (id != khuyenMai.MaKhuyenMai)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra thời gian
                if (khuyenMai.ThoiGianBatDau >= khuyenMai.ThoiGianKetThuc)
                {
                    ModelState.AddModelError("ThoiGianKetThuc", "Thời gian kết thúc phải sau thời gian bắt đầu!");
                    return View(khuyenMai);
                }

                // Kiểm tra giá trị giảm
                if (khuyenMai.GiaTri <= 0 || khuyenMai.GiaTri > 100)
                {
                    ModelState.AddModelError("GiaTri", "Giá trị giảm phải từ 1-100!");
                    return View(khuyenMai);
                }

                // Kiểm tra số lượng
                if (khuyenMai.SoLuong <= 0)
                {
                    ModelState.AddModelError("SoLuong", "Số lượng phải lớn hơn 0!");
                    return View(khuyenMai);
                }

                try
                {
                    // Tối ưu: Lấy đối tượng đang được DB theo dõi để cập nhật trực tiếp
                    var existingKhuyenMai = await _context.KhuyenMais.FirstOrDefaultAsync(x => x.MaKhuyenMai == id);
                    
                    if (existingKhuyenMai == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật các trường cho phép sửa, giữ nguyên NgayTao
                    existingKhuyenMai.GiaTri = khuyenMai.GiaTri;
                    existingKhuyenMai.ThoiGianBatDau = khuyenMai.ThoiGianBatDau;
                    existingKhuyenMai.ThoiGianKetThuc = khuyenMai.ThoiGianKetThuc;
                    existingKhuyenMai.TrangThai = khuyenMai.TrangThai;
                    existingKhuyenMai.DieuKienApDung = khuyenMai.DieuKienApDung;
                    existingKhuyenMai.SoLuong = khuyenMai.SoLuong;

                    _context.Update(existingKhuyenMai);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KhuyenMaiExists(khuyenMai.MaKhuyenMai))
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
            return View(khuyenMai);
        }

        // GET: KhuyenMais/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khuyenMai = await _context.KhuyenMais
                .FirstOrDefaultAsync(m => m.MaKhuyenMai == id);
            if (khuyenMai == null)
            {
                return NotFound();
            }

            return View(khuyenMai);
        }

        // POST: KhuyenMais/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai != null)
            {
                _context.KhuyenMais.Remove(khuyenMai);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KhuyenMaiExists(string id)
        {
            return _context.KhuyenMais.Any(e => e.MaKhuyenMai == id);
        }
    }
}
