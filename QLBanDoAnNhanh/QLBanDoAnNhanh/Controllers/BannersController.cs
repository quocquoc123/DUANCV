using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Common;
using QLBanDoAnNhanh.Models;
using QLBanDoAnNhanh.Services;

namespace QLBanDoAnNhanh.Controllers
{
    /// <summary>
    /// Quản lý banner hero (trái/phải) cho trang chủ và từng danh mục.
    /// </summary>
    public class BannersController : Controller
    {
        private readonly QlbanDoAnNhanh3Context _context;
        private readonly CloudinaryService _cloudinaryService;

        public BannersController(QlbanDoAnNhanh3Context context, CloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        // GET: /Banners/AdminIndex
        [AdminAuthFilter]
        public async Task<IActionResult> AdminIndex(string phamVi, string viTri)
        {
            var query = _context.Banners
                .AsNoTracking()
                .Include(b => b.MaDmNavigation)
                .AsQueryable();

            if (phamVi == "trangchu")
                query = query.Where(b => b.MaDm == null);
            else if (phamVi == "danhmuc")
                query = query.Where(b => b.MaDm != null);

            if (!string.IsNullOrWhiteSpace(viTri) && (viTri == "Left" || viTri == "Right"))
                query = query.Where(b => b.ViTri == viTri);

            ViewBag.PhamVi = phamVi;
            ViewBag.ViTri = viTri;
            ViewBag.TongBanner = await _context.Banners.CountAsync();

            var list = await query
                .OrderBy(b => b.MaDm == null ? 0 : 1)
                .ThenBy(b => b.MaDm)
                .ThenBy(b => b.ViTri)
                .ThenBy(b => b.ThuTu)
                .ToListAsync();

            return View("Admin/Index", list);
        }

        // GET: /Banners/Create
        [AdminAuthFilter]
        public async Task<IActionResult> Create()
        {
            await LoadDanhMucDropdownAsync();
            return View("Admin/Create", new Banner { TrangThai = true, ViTri = "Left", ThuTu = 0 });
        }

        // POST: /Banners/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthFilter]
        public async Task<IActionResult> Create(
            [Bind("TieuDe,ViTri,MaDm,ThuTu,TrangThai")] Banner banner,
            IFormFile hinhAnhFile)
        {
            NormalizeViTri(banner);

            if (hinhAnhFile == null || hinhAnhFile.Length == 0)
                ModelState.AddModelError("HinhAnh", "Vui lòng chọn ảnh banner.");

            if (ModelState.IsValid)
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(hinhAnhFile, "banner");
                if (imageUrl == null)
                {
                    ModelState.AddModelError("HinhAnh", "Upload ảnh thất bại. Vui lòng thử lại.");
                }
                else
                {
                    banner.HinhAnh = imageUrl;
                    banner.NgayCapNhat = DateTime.Now;
                    _context.Add(banner);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã thêm banner \"{banner.TieuDe}\" thành công!";
                    return RedirectToAction(nameof(AdminIndex));
                }
            }

            await LoadDanhMucDropdownAsync(banner.MaDm);
            return View("Admin/Create", banner);
        }

        // GET: /Banners/Edit/5
        [AdminAuthFilter]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            await LoadDanhMucDropdownAsync(banner.MaDm);
            return View("Admin/Edit", banner);
        }

        // POST: /Banners/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthFilter]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("MaBanner,TieuDe,ViTri,MaDm,ThuTu,TrangThai,HinhAnh")] Banner banner,
            IFormFile hinhAnhFile)
        {
            if (id != banner.MaBanner) return NotFound();

            NormalizeViTri(banner);

            if (string.IsNullOrWhiteSpace(banner.HinhAnh) && (hinhAnhFile == null || hinhAnhFile.Length == 0))
                ModelState.AddModelError("HinhAnh", "Banner cần có ảnh. Vui lòng chọn ảnh mới.");

            if (ModelState.IsValid)
            {
                try
                {
                    if (hinhAnhFile != null && hinhAnhFile.Length > 0)
                    {
                        var oldPublicId = CloudinaryService.ExtractPublicId(banner.HinhAnh);
                        if (!string.IsNullOrWhiteSpace(oldPublicId))
                            await _cloudinaryService.DeleteImageAsync(oldPublicId);
                        _cloudinaryService.DeleteLocalIfExists(banner.HinhAnh);

                        var imageUrl = await _cloudinaryService.UploadImageAsync(hinhAnhFile, "banner");
                        if (imageUrl != null)
                            banner.HinhAnh = imageUrl;
                        else
                            TempData["Warning"] = "Upload ảnh thất bại. Ảnh cũ vẫn được giữ nguyên.";
                    }

                    banner.NgayCapNhat = DateTime.Now;
                    _context.Update(banner);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã cập nhật banner \"{banner.TieuDe}\" thành công!";
                    return RedirectToAction(nameof(AdminIndex));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Banners.AnyAsync(b => b.MaBanner == banner.MaBanner))
                        return NotFound();
                    throw;
                }
            }

            await LoadDanhMucDropdownAsync(banner.MaDm);
            return View("Admin/Edit", banner);
        }

        // POST: /Banners/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthFilter]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                TempData["Error"] = "Không tìm thấy banner.";
                return RedirectToAction(nameof(AdminIndex));
            }

            var publicId = CloudinaryService.ExtractPublicId(banner.HinhAnh);
            if (!string.IsNullOrWhiteSpace(publicId))
                await _cloudinaryService.DeleteImageAsync(publicId);
            _cloudinaryService.DeleteLocalIfExists(banner.HinhAnh);

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa banner \"{banner.TieuDe}\".";
            return RedirectToAction(nameof(AdminIndex));
        }

        private async Task LoadDanhMucDropdownAsync(int? selectedMaDm = null)
        {
            var danhMucs = await _context.DanhMucs
                .AsNoTracking()
                .OrderBy(d => d.TenDm)
                .Select(d => new { d.MaDm, d.TenDm })
                .ToListAsync();

            ViewBag.DanhMucList = new SelectList(danhMucs, "MaDm", "TenDm", selectedMaDm);
        }

        private static void NormalizeViTri(Banner banner)
        {
            banner.ViTri = string.Equals(banner.ViTri, "Right", StringComparison.OrdinalIgnoreCase)
                ? "Right"
                : "Left";

            // Form gửi empty string → null (trang chủ)
            if (banner.MaDm.HasValue && banner.MaDm.Value <= 0)
                banner.MaDm = null;
        }
    }
}
