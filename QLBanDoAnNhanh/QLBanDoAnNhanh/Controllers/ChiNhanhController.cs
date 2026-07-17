using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Common;
using QLBanDoAnNhanh.Models;
using QLBanDoAnNhanh.Services;

namespace QLBanDoAnNhanh.Controllers
{
    /// <summary>
    /// Controller quản lý chi nhánh cửa hàng.
    /// - Phần Admin (CRUD): yêu cầu đăng nhập Admin qua AdminAuthFilter.
    /// - Phần User (Index): không yêu cầu đăng nhập, hiển thị danh sách chi nhánh.
    /// </summary>
    public class ChiNhanhController : Controller
    {
        private readonly QlbanDoAnNhanh3Context _context;
        private readonly CloudinaryService _cloudinaryService;

        public ChiNhanhController(QlbanDoAnNhanh3Context context, CloudinaryService cloudinaryService)
        {
            _context          = context;
            _cloudinaryService = cloudinaryService;
        }

        // ============================================================
        //  PHẦN NGƯỜI DÙNG – Không cần đăng nhập
        // ============================================================

        /// <summary>
        /// Hiển thị danh sách chi nhánh cho người dùng (không cần đăng nhập).
        /// </summary>
        // GET: /ChiNhanh
        public async Task<IActionResult> Index(string search, string trangThai)
        {
            var query = _context.ChiNhanhs.AsNoTracking().AsQueryable();

            // Chỉ hiển thị chi nhánh đang hoạt động với người dùng
            query = query.Where(cn => cn.TrangThai);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(cn =>
                    cn.TenChiNhanh.ToLower().Contains(kw) ||
                    cn.DiaChi.ToLower().Contains(kw));
            }

            var entities = await query.ToListAsync();

            // Map sang ChiNhanhViewModel (view yêu cầu kiểu này)
            var danhSach = entities.Select(cn => new ChiNhanhViewModel
            {
                Id          = cn.MaChiNhanh,
                TenChiNhanh = cn.TenChiNhanh,
                DiaChi      = cn.DiaChi,
                SoDienThoai = cn.SoDienThoai,
                GioMoCua    = cn.GioMoCua,
                GioDongCua  = cn.GioDongCua,
                Latitude    = cn.Latitude ?? 0,
                Longitude   = cn.Longitude ?? 0,
                HinhAnh     = cn.HinhAnh ?? string.Empty,
                // Lấy phần cuối địa chỉ làm Quận (vd: "123 Lê Lợi, Quận 1" → "Quận 1")
                Quan        = cn.DiaChi.Contains(',')
                                ? cn.DiaChi.Split(',').Last().Trim()
                                : string.Empty
            }).ToList();

            ViewBag.Search       = search;
            ViewBag.TrangThai    = trangThai;
            ViewBag.TongChiNhanh = await _context.ChiNhanhs.CountAsync(cn => cn.TrangThai);

            return View(danhSach);
        }

        // ============================================================
        //  PHẦN ADMIN – Yêu cầu đăng nhập Admin
        // ============================================================

        /// <summary>
        /// Danh sách chi nhánh phía Admin với tìm kiếm và lọc trạng thái.
        /// </summary>
        // GET: /ChiNhanh/AdminIndex
        [AdminAuthFilter]
        public async Task<IActionResult> AdminIndex(string search, string trangThai)
        {
            var query = _context.ChiNhanhs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(cn =>
                    cn.TenChiNhanh.ToLower().Contains(kw) ||
                    cn.DiaChi.ToLower().Contains(kw) ||
                    (cn.SoDienThoai != null && cn.SoDienThoai.Contains(kw)) ||
                    (cn.Email != null && cn.Email.ToLower().Contains(kw)));
            }

            if (trangThai == "hoatdong")
                query = query.Where(cn => cn.TrangThai);
            else if (trangThai == "ngunghoatdong")
                query = query.Where(cn => !cn.TrangThai);

            ViewBag.Search       = search;
            ViewBag.TrangThai    = trangThai;
            ViewBag.TongChiNhanh = await _context.ChiNhanhs.CountAsync();

            var danhSach = await query.OrderBy(cn => cn.MaChiNhanh).ToListAsync();
            return View("Admin/Index", danhSach);
        }

        /// <summary>
        /// Xem chi tiết một chi nhánh (Admin).
        /// </summary>
        // GET: /ChiNhanh/Details/5
        [AdminAuthFilter]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var chiNhanh = await _context.ChiNhanhs
                .AsNoTracking()
                .FirstOrDefaultAsync(cn => cn.MaChiNhanh == id);

            if (chiNhanh == null) return NotFound();

            return View("Admin/Details", chiNhanh);
        }

        /// <summary>
        /// Hiển thị form tạo mới chi nhánh.
        /// </summary>
        // GET: /ChiNhanh/Create
        [AdminAuthFilter]
        public IActionResult Create()
        {
            return View("Admin/Create");
        }

        /// <summary>
        /// Xử lý tạo mới chi nhánh, upload ảnh lên Cloudinary.
        /// </summary>
        // POST: /ChiNhanh/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthFilter]
        public async Task<IActionResult> Create(
            [Bind("TenChiNhanh,DiaChi,SoDienThoai,Email,GioMoCua,GioDongCua,Latitude,Longitude,TrangThai")]
            ChiNhanh chiNhanh,
            IFormFile? hinhAnhFile)
        {
            if (ModelState.IsValid)
            {
                // Upload ảnh lên Cloudinary nếu có chọn file
                if (hinhAnhFile != null && hinhAnhFile.Length > 0)
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(hinhAnhFile, "chinhanh");
                    if (imageUrl != null)
                        chiNhanh.HinhAnh = imageUrl;
                    else
                        TempData["Warning"] = "Upload ảnh thất bại. Chi nhánh được lưu mà không có ảnh.";
                }

                _context.Add(chiNhanh);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã thêm chi nhánh \"{chiNhanh.TenChiNhanh}\" thành công!";
                return RedirectToAction(nameof(AdminIndex));
            }

            return View("Admin/Create", chiNhanh);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa chi nhánh.
        /// </summary>
        // GET: /ChiNhanh/Edit/5
        [AdminAuthFilter]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var chiNhanh = await _context.ChiNhanhs.FindAsync(id);
            if (chiNhanh == null) return NotFound();

            return View("Admin/Edit", chiNhanh);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin chi nhánh, thay ảnh nếu có upload mới.
        /// </summary>
        // POST: /ChiNhanh/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthFilter]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("MaChiNhanh,TenChiNhanh,DiaChi,SoDienThoai,Email,GioMoCua,GioDongCua,Latitude,Longitude,HinhAnh,TrangThai")]
            ChiNhanh chiNhanh,
            IFormFile? hinhAnhFile)
        {
            if (id != chiNhanh.MaChiNhanh) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Upload ảnh mới nếu người dùng chọn file
                    if (hinhAnhFile != null && hinhAnhFile.Length > 0)
                    {
                        // Xóa ảnh cũ trên Cloudinary nếu có
                        var oldPublicId = CloudinaryService.ExtractPublicId(chiNhanh.HinhAnh);
                        if (!string.IsNullOrWhiteSpace(oldPublicId))
                            await _cloudinaryService.DeleteImageAsync(oldPublicId);

                        // Upload ảnh mới
                        var imageUrl = await _cloudinaryService.UploadImageAsync(hinhAnhFile, "chinhanh");
                        if (imageUrl != null)
                            chiNhanh.HinhAnh = imageUrl;
                        else
                            TempData["Warning"] = "Upload ảnh thất bại. Ảnh cũ vẫn được giữ nguyên.";
                    }

                    _context.Update(chiNhanh);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã cập nhật chi nhánh \"{chiNhanh.TenChiNhanh}\" thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChiNhanhExists(chiNhanh.MaChiNhanh))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(AdminIndex));
            }

            return View("Admin/Edit", chiNhanh);
        }

        /// <summary>
        /// Xóa chi nhánh (POST), đồng thời xóa ảnh trên Cloudinary.
        /// </summary>
        // POST: /ChiNhanh/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthFilter]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chiNhanh = await _context.ChiNhanhs.FindAsync(id);
            if (chiNhanh == null)
            {
                TempData["Error"] = "Không tìm thấy chi nhánh.";
                return RedirectToAction(nameof(AdminIndex));
            }

            // Xóa ảnh trên Cloudinary nếu có
            var publicId = CloudinaryService.ExtractPublicId(chiNhanh.HinhAnh);
            if (!string.IsNullOrWhiteSpace(publicId))
                await _cloudinaryService.DeleteImageAsync(publicId);

            _context.ChiNhanhs.Remove(chiNhanh);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa chi nhánh \"{chiNhanh.TenChiNhanh}\" thành công!";
            return RedirectToAction(nameof(AdminIndex));
        }

        // ============================================================
        //  API ENDPOINT – Lấy thông tin chi tiết (dùng bởi User View)
        // ============================================================

        // GET: /ChiNhanh/GetChiNhanh?id=5
        [HttpGet]
        public async Task<IActionResult> GetChiNhanh(int id)
        {
            var cn = await _context.ChiNhanhs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.MaChiNhanh == id);

            if (cn == null) return NotFound();

            return Json(new
            {
                id          = cn.MaChiNhanh,
                tenChiNhanh = cn.TenChiNhanh,
                diaChi      = cn.DiaChi,
                soDienThoai = cn.SoDienThoai,
                email       = cn.Email,
                gioMoCua    = cn.GioMoCua,
                gioDongCua  = cn.GioDongCua,
                latitude    = cn.Latitude,
                longitude   = cn.Longitude,
                isOpen      = cn.IsOpen,
                trangThai   = cn.TrangThai
            });
        }

        // ============================================================
        //  API ENDPOINTS – Branch Picker (dùng bởi Header dropdown)
        // ============================================================

        /// <summary>
        /// Trả về danh sách tất cả chi nhánh đang hoạt động dưới dạng JSON.
        /// Dùng bởi header branch-picker dropdown.
        /// </summary>
        // GET: /ChiNhanh/GetDanhSachChiNhanh
        [HttpGet]
        public async Task<IActionResult> GetDanhSachChiNhanh()
        {
            var danhSach = await _context.ChiNhanhs
                .AsNoTracking()
                .Where(cn => cn.TrangThai)
                .OrderBy(cn => cn.TenChiNhanh)
                .Select(cn => new
                {
                    id          = cn.MaChiNhanh,
                    tenChiNhanh = cn.TenChiNhanh,
                    diaChi      = cn.DiaChi,
                    gioMoCua    = cn.GioMoCua,
                    gioDongCua  = cn.GioDongCua,
                    isOpen      = cn.IsOpen
                })
                .ToListAsync();

            return Json(danhSach);
        }

        /// <summary>
        /// Lưu chi nhánh đang chọn vào Session.
        /// </summary>
        // POST: /ChiNhanh/SetChiNhanh
        [HttpPost]
        public IActionResult SetChiNhanh([FromBody] SetChiNhanhRequest request)
        {
            if (request == null || request.MaChiNhanh <= 0)
            {
                // Xóa session khi reset về "Tất cả"
                HttpContext.Session.Remove("SelectedBranchId");
                HttpContext.Session.Remove("SelectedBranchName");
                return Json(new { success = true, cleared = true });
            }

            var cn = _context.ChiNhanhs
                .AsNoTracking()
                .FirstOrDefault(c => c.MaChiNhanh == request.MaChiNhanh && c.TrangThai);

            if (cn == null)
                return Json(new { success = false, message = "Chi nhánh không tồn tại." });

            HttpContext.Session.SetInt32("SelectedBranchId", cn.MaChiNhanh);
            HttpContext.Session.SetString("SelectedBranchName", cn.TenChiNhanh);

            return Json(new { success = true, tenChiNhanh = cn.TenChiNhanh, maChiNhanh = cn.MaChiNhanh });
        }

        /// <summary>
        /// Lấy danh sách sản phẩm thuộc chi nhánh được chỉ định qua bảng SanPhamChiNhanh.
        /// </summary>
        // GET: /ChiNhanh/GetSanPhamTheoChiNhanh?maChiNhanh=1
        [HttpGet]
        public async Task<IActionResult> GetSanPhamTheoChiNhanh(int maChiNhanh)
        {
            if (maChiNhanh <= 0)
                return Json(new { success = false, message = "Mã chi nhánh không hợp lệ." });

            var branchExists = await _context.ChiNhanhs
                .AsNoTracking()
                .AnyAsync(cn => cn.MaChiNhanh == maChiNhanh && cn.TrangThai);

            if (!branchExists)
                return Json(new { success = false, message = "Chi nhánh không tồn tại." });

            var sanPhams = await _context.SanPhamChiNhanhs
                .AsNoTracking()
                .Where(spcn => spcn.MaChiNhanh == maChiNhanh)
                .Include(spcn => spcn.MaSpNavigation)
                    .ThenInclude(sp => sp.MaDmNavigation)
                .Include(spcn => spcn.MaSpNavigation)
                    .ThenInclude(sp => sp.MaGiamGiaNavigation)
                .Select(spcn => spcn.MaSpNavigation)
                .ToListAsync();

            var now = DateTime.Now;
            var result = sanPhams.Select(sp =>
            {
                var giamGia = sp.MaGiamGiaNavigation;
                bool hasDiscount = giamGia != null
                    && giamGia.GiaTri > 0
                    && giamGia.ThoiGianBatDau <= now
                    && giamGia.ThoiGianKetThuc >= now;

                decimal giaSau = hasDiscount
                    ? Math.Round((decimal)sp.GiaTien - ((decimal)sp.GiaTien * giamGia!.GiaTri / 100), 0)
                    : (decimal)sp.GiaTien;

                return new
                {
                    maSp            = sp.MaSp,
                    tenSp           = sp.TenSp,
                    giaTien         = sp.GiaTien,
                    giaSauGiamGia   = giaSau,
                    discountPercent = hasDiscount ? giamGia!.GiaTri : 0,
                    hinhAnh1        = sp.HinhAnh1 ?? "",
                    thanhPhan       = sp.ThanhPhan ?? "",
                    slbanTrongNgay  = sp.SlbanTrongNgay ?? 0,
                    tenDanhMuc      = sp.MaDmNavigation?.TenDm ?? "",
                    detailUrl       = Url.Action("ChiTietSanPham", "SanPhams", new { id = sp.MaSp })
                };
            }).ToList();

            return Json(new { success = true, sanPhams = result, total = result.Count });
        }

        // ============================================================
        //  HELPERS
        // ============================================================

        private bool ChiNhanhExists(int id)
        {
            return _context.ChiNhanhs.Any(e => e.MaChiNhanh == id);
        }
    }

    /// <summary>
    /// DTO cho request body của POST /ChiNhanh/SetChiNhanh.
    /// </summary>
    public class SetChiNhanhRequest
    {
        public int MaChiNhanh { get; set; }
    }
}
