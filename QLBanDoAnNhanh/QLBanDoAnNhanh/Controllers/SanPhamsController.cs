using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;
using System.IO; 
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;
using QLBanDoAnNhanh.Services;
namespace QLBanDoAnNhanh.Controllers
{
    public class SanPhamsController : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly QlbanDoAnNhanh3Context _context;
        private readonly IProductDiscountService _discountService;

        public SanPhamsController(QlbanDoAnNhanh3Context context, IConfiguration configuration, IProductDiscountService discountService)
        {
            _configuration = configuration;
            _context = context;
            _discountService = discountService;
        }

        // GET: SanPhams
        public async Task<IActionResult> Index()
        {
            // Lấy tất cả sản phẩm cùng với các thông tin liên quan
            var sanPhams = _context.SanPhams
                .AsNoTracking()
                .Include(sp => sp.MaDmNavigation)
                .Include(sp => sp.MaGiamGiaNavigation)// Lấy thông tin giảm giá
                .Include(sp => sp.SanPhamChiNhanhs)
                    .ThenInclude(spn => spn.MaChiNhanhNavigation);
                

            return View(await sanPhams.ToListAsync());
        }


        // GET: SanPhams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .AsNoTracking()
                .Include(s => s.MaDmNavigation)
                .Include(s => s.MaGiamGiaNavigation)
                .FirstOrDefaultAsync(m => m.MaSp == id);
            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }
        public async Task<IActionResult> ChiTietSanPham(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            // Lấy sản phẩm và các bình luận liên quan
            var sanPham = await _context.SanPhams
                .AsNoTracking()
                .Include(sp => sp.MaGiamGiaNavigation)
                .Include(sp => sp.HinhAnhs) // Nếu cần thông tin hình ảnh
                .FirstOrDefaultAsync(sp => sp.MaSp == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            // Lấy bình luận cho sản phẩm này
            var binhLuans = await _context.BinhLuans
                .AsNoTracking()
                .Include(bl => bl.MaNguoiDungNavigation)
                .Where(bl => bl.MaSp == id)
                .OrderByDescending(bl => bl.NgayBinhLuan)
                .ToListAsync();

            // Tính điểm trung bình từ bảng đánh giá
            var diemTrungBinh = await _context.DanhGia
                .AsNoTracking()
                .Where(dg => dg.MaSanPham == id)
                .AverageAsync(dg => (double?)dg.SoSao) ?? 0; // Trả về 0 nếu không có đánh giá

            // Thống kê phân bố sao (UI-only, không thay đổi business logic)
            var ratingCountsQuery = await _context.DanhGia
                .AsNoTracking()
                .Where(dg => dg.MaSanPham == id)
                .GroupBy(dg => dg.SoSao)
                .Select(g => new { Star = (int)g.Key, Count = g.Count() })
                .ToListAsync();

            var ratingCounts = new int[6]; // index 1..5
            foreach (var row in ratingCountsQuery)
            {
                if (row.Star >= 1 && row.Star <= 5)
                {
                    ratingCounts[row.Star] = row.Count;
                }
            }

            // Map rating theo user để hiển thị trên review card (UI-only)
            var userRatings = await _context.DanhGia
                .AsNoTracking()
                .Where(dg => dg.MaSanPham == id)
                .GroupBy(dg => dg.MaNguoiDung)
                .Select(g => new { MaNguoiDung = g.Key, SoSao = (int)g.Max(x => x.SoSao) })
                .ToDictionaryAsync(x => x.MaNguoiDung, x => x.SoSao);

            var username = HttpContext.Session.GetString("userLogin");
            var nguoiDung = !string.IsNullOrEmpty(username)
                ? await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(nd => nd.Username == username)
                : null;

            var canComment = false;
            if (nguoiDung != null)
            {
                var daMuaSanPham = await _context.ChiTietDonHangs
                    .AsNoTracking()
                    .AnyAsync(ct =>
                        ct.MaSp == id &&
                        (
                            ct.MaDhNavigation.MaNguoiDung == nguoiDung.MaNguoiDung ||
                            ct.MaDhNavigation.Username == username
                        ) &&
                        ct.MaDhNavigation.TrangThai != "Đã Hủy");

                var daBinhLuan = await _context.BinhLuans
                    .AsNoTracking()
                    .AnyAsync(bl => bl.MaSp == id && bl.MaNguoiDung == nguoiDung.MaNguoiDung);

                canComment = daMuaSanPham && !daBinhLuan;
                ViewBag.HasPurchasedProduct = daMuaSanPham;
                ViewBag.HasCommentedProduct = daBinhLuan;
            }
            else
            {
                ViewBag.HasPurchasedProduct = false;
                ViewBag.HasCommentedProduct = false;
            }

            // Truyền sản phẩm, bình luận và điểm trung bình vào View
            ViewBag.BinhLuans = binhLuans;
            ViewBag.DiemTrungBinh = diemTrungBinh;
            ViewBag.IsDiscountActive = _discountService.IsDiscountActive(sanPham);
            ViewBag.EffectivePrice = _discountService.GetEffectivePrice(sanPham);
            ViewBag.CanComment = canComment;
            ViewBag.RatingCounts = ratingCounts;
            ViewBag.UserRatings = userRatings;

            return View(sanPham);
        }



        [HttpPost]
        public async Task<IActionResult> AddComment(int maSP, string noiDung)
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (HttpContext.Session.GetString("userLogin") == null)
            {
                return RedirectToAction("Login", "User");
            }

            string username = HttpContext.Session.GetString("userLogin");
            var nguoiDung = await _context.NguoiDungs.FirstOrDefaultAsync(nd => nd.Username == username);

            if (nguoiDung == null)
            {
                return RedirectToAction("Index", "LoginUser");
            }

            if (string.IsNullOrWhiteSpace(noiDung))
            {
                TempData["ErrorMessage"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction("ChiTietSanPham", new { id = maSP });
            }

            var daMuaSanPham = await _context.ChiTietDonHangs
                .AnyAsync(ct =>
                    ct.MaSp == maSP &&
                    (
                        ct.MaDhNavigation.MaNguoiDung == nguoiDung.MaNguoiDung ||
                        ct.MaDhNavigation.Username == username
                    ) &&
                    ct.MaDhNavigation.TrangThai != "Đã Hủy");

            if (!daMuaSanPham)
            {
                TempData["ErrorMessage"] = "Bạn cần mua sản phẩm trước khi bình luận.";
                return RedirectToAction("ChiTietSanPham", new { id = maSP });
            }

            var daBinhLuan = await _context.BinhLuans
                .AnyAsync(bl => bl.MaSp == maSP && bl.MaNguoiDung == nguoiDung.MaNguoiDung);

            if (daBinhLuan)
            {
                TempData["ErrorMessage"] = "Bạn chỉ được bình luận 1 lần cho sản phẩm này.";
                return RedirectToAction("ChiTietSanPham", new { id = maSP });
            }

            // Tạo một đối tượng bình luận mới
            BinhLuan binhLuan = new BinhLuan
            {
                MaSp = maSP,
                MaNguoiDung = nguoiDung.MaNguoiDung,
                NoiDung = noiDung.Trim(),
                NgayBinhLuan = DateTime.Now
            };

            // Thêm bình luận vào cơ sở dữ liệu
            _context.BinhLuans.Add(binhLuan);
            await _context.SaveChangesAsync();

            // Chuyển hướng về trang chi tiết sản phẩm
            return RedirectToAction("ChiTietSanPham", new { id = maSP });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAndUpdateRating(int maSP, decimal rating)
        {
            try
            {
                // Kiểm tra xem người dùng đã đăng nhập chưa
                if (HttpContext.Session.GetString("userLogin") == null)
                {
                    return RedirectToAction("Login", "User");
                }

                // Lấy thông tin người dùng từ session
                string username = HttpContext.Session.GetString("userLogin");
                var nguoiDung = await _context.NguoiDungs.FirstOrDefaultAsync(nd => nd.Username == username);

                if (nguoiDung == null)
                {
                    return RedirectToAction("Index", "LoginUser");
                }

                // Kiểm tra giá trị đánh giá hợp lệ
                if (rating < 1 || rating > 5)
                {
                    TempData["ErrorMessage"] = "Đánh giá không hợp lệ. Vui lòng chọn số sao từ 1 đến 5.";
                    return RedirectToAction("ChiTietSanPham", new { id = maSP });
                }

                // Kiểm tra người dùng hiện tại đã mua sản phẩm chưa
                // (Trước đây chỉ kiểm tra sản phẩm có xuất hiện trong đơn hàng của bất kỳ ai)
                var daMuaHang = await _context.ChiTietDonHangs
                    .AnyAsync(ct =>
                        ct.MaSp == maSP &&
                        (
                            ct.MaDhNavigation.MaNguoiDung == nguoiDung.MaNguoiDung ||
                            ct.MaDhNavigation.Username == username
                        ) &&
                        ct.MaDhNavigation.TrangThai != "Đã Hủy");

                if (!daMuaHang)
                {
                    TempData["ErrorMessage"] = "Bạn chưa mua sản phẩm này, không thể đánh giá.";
                    return RedirectToAction("ChiTietSanPham", new { id = maSP });
                }
                // Kiểm tra xem người dùng đã đánh giá sản phẩm này chưa
                var daDanhGia = await _context.DanhGia
            .AnyAsync(dg => dg.MaNguoiDung == nguoiDung.MaNguoiDung && dg.MaSanPham == maSP);

                if (daDanhGia)
                {
                    TempData["ErrorMessage"] = "Bạn đã đánh giá sản phẩm này rồi.";
                    return RedirectToAction("ChiTietSanPham", new { id = maSP });
                }

                // Thêm đánh giá mới vào cơ sở dữ liệu
                var danhGia = new DanhGium
                {
                    MaSanPham = maSP,
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    SoSao = rating,
                    NgayBinhLuan = DateTime.Now
                };

                await _context.DanhGia.AddAsync(danhGia);
                await _context.SaveChangesAsync();

                // Tính và cập nhật điểm trung bình
                // Cập nhật lại SoSaoTrungBinh của sản phẩm trong bảng DanhGia
                //danhGia.SoSaoTrungBinh = averageRating;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đánh giá của bạn đã được thêm thành công!";
            }
            catch (Exception ex)
            {
                // Thêm lỗi vào ModelState để hiển thị lỗi nếu có
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi thêm đánh giá: " + ex.Message);
            }

            return RedirectToAction("ChiTietSanPham", new { id = maSP });
        }




        // GET: SanPhams/Create
        public IActionResult Create()
        {
            ViewData["MaDm"] = new SelectList(_context.DanhMucs, "MaDm", "TenDm");
            ViewData["MaGiamGia"] = new SelectList(_context.GiamGia, "MaGiamGia", "MaGiamGia");
            ViewBag.ChiNhanhs = _context.ChiNhanhs.Where(cn => cn.TrangThai).OrderBy(cn => cn.TenChiNhanh).ToList();
            ViewBag.SelectedChiNhanhs = new List<int>();
            return View();
        }

        // POST: SanPhams/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaSp,TenSp,MaGiamGia,ThanhPhan,GiaTien,DonVi,ChitietSp,MaDm,SlbanTrongNgay")] SanPham sanPham, IFormFile HinhAnh1, IFormFile HinhAnh2, int[] selectedChiNhanhs)
        {
            if (sanPham.SlbanTrongNgay < 0)
            {
                ModelState.AddModelError("SlbanTrongNgay", "Số lượng bán trong ngày không thể nhỏ hơn 0.");
            }

            // Nếu có lỗi, trả lại View với dữ liệu nhập trước đó
            if (!ModelState.IsValid)
            {
                ViewData["MaDm"] = new SelectList(_context.DanhMucs, "MaDm", "TenDm", sanPham.MaDm);
                ViewData["MaGiamGia"] = new SelectList(_context.GiamGia, "MaGiamGia", "MaGiamGia", sanPham.MaGiamGia);
                ViewBag.ChiNhanhs = _context.ChiNhanhs.Where(cn => cn.TrangThai).OrderBy(cn => cn.TenChiNhanh).ToList();
                ViewBag.SelectedChiNhanhs = selectedChiNhanhs?.ToList() ?? new List<int>();
                return View(sanPham);
            }
            // Kiểm tra tệp HinhAnh1 và HinhAnh2 có được chọn không
            if (HinhAnh1 != null && HinhAnh1.Length > 0)
            {
                // Lấy đường dẫn lưu tệp
                var fileName1 = Path.GetFileName(HinhAnh1.FileName);
                var filePath1 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName1);

                // Lưu tệp vào thư mục 'wwwroot/images' 
                using (var stream = new FileStream(filePath1, FileMode.Create))
                {
                    await HinhAnh1.CopyToAsync(stream);
                }

                // Lưu đường dẫn vào cơ sở dữ liệu
                sanPham.HinhAnh1 = "/images/" + fileName1;
            }

            if (HinhAnh2 != null && HinhAnh2.Length > 0)
            {
                var fileName2 = Path.GetFileName(HinhAnh2.FileName);
                var filePath2 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName2);

                using (var stream = new FileStream(filePath2, FileMode.Create))
                {
                    await HinhAnh2.CopyToAsync(stream);
                }

                sanPham.HinhAnh2 = "/images/" + fileName2;
            }

            // Lưu sản phẩm vào database
            _context.Add(sanPham);
            await _context.SaveChangesAsync();

            // Lưu quan hệ chi nhánh
            if (selectedChiNhanhs != null && selectedChiNhanhs.Length > 0)
            {
                foreach (var maChiNhanh in selectedChiNhanhs)
                {
                    _context.SanPhamChiNhanhs.Add(new SanPhamChiNhanh
                    {
                        MaSp = sanPham.MaSp,
                        MaChiNhanh = maChiNhanh
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: SanPhams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(sp => sp.SanPhamChiNhanhs)
                .FirstOrDefaultAsync(sp => sp.MaSp == id);
            if (sanPham == null)
            {
                return NotFound();
            }
            ViewData["MaDm"] = new SelectList(_context.DanhMucs, "MaDm", "MaDm", sanPham.MaDm);
            ViewData["MaGiamGia"] = new SelectList(_context.GiamGia, "MaGiamGia", "MaGiamGia", sanPham.MaGiamGia);
            ViewBag.ChiNhanhs = _context.ChiNhanhs.Where(cn => cn.TrangThai).OrderBy(cn => cn.TenChiNhanh).ToList();
            ViewBag.SelectedChiNhanhs = sanPham.SanPhamChiNhanhs.Select(x => x.MaChiNhanh).ToList();
            return View(sanPham);
        }

        // POST: Admin/SanPhams/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaSp,TenSp,MaGiamGia,ThanhPhan,GiaTien,DonVi,ChitietSp,MaDm,SlbanTrongNgay,HinhAnh1,HinhAnh2")]
        SanPham sanPham, IFormFile file1, IFormFile file2, int[] selectedChiNhanhs)
        {
            if (id != sanPham.MaSp)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra xem có tệp hình ảnh mới không
                    if ((file1 != null && file1.Length > 0) && (file2 != null && file2.Length > 0))
                    {
                        // Tạo tên file duy nhất để tránh xung đột
                        var fileName1 = Path.GetFileName(file1.FileName);
                        var fileName2 = Path.GetFileName(file2.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName1);
                        var filePath1 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName2);

                        // Xóa hình ảnh cũ (nếu cần)

                        // Lưu file vào thư mục wwwroot/images
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file1.CopyToAsync(stream);

                        }
                        using (var stream = new FileStream(filePath1, FileMode.Create))
                        {

                            await file2.CopyToAsync(stream);
                        }

                        // Gán đường dẫn của ảnh mới cho thuộc tính AnhSp
                        sanPham.HinhAnh1 = "/images/" + fileName1; // Đảm bảo đường dẫn hợp lệ
                        sanPham.HinhAnh2 = "/images/" + fileName2; // Đảm bảo đường dẫn hợp lệ
                    }

                    // Cập nhật thông tin sản phẩm
                    _context.Update(sanPham);

                    // Cập nhật quan hệ chi nhánh: xóa cũ, thêm mới
                    var existing = _context.SanPhamChiNhanhs.Where(x => x.MaSp == id).ToList();
                    _context.SanPhamChiNhanhs.RemoveRange(existing);

                    if (selectedChiNhanhs != null && selectedChiNhanhs.Length > 0)
                    {
                        foreach (var maChiNhanh in selectedChiNhanhs)
                        {
                            _context.SanPhamChiNhanhs.Add(new SanPhamChiNhanh
                            {
                                MaSp = id,
                                MaChiNhanh = maChiNhanh
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SanPhamExists(sanPham.MaSp))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }


                }
            }
            ViewData["MaDm"] = new SelectList(_context.DanhMucs, "MaDm", "MaDm", sanPham.MaDm);
            ViewData["MaGiamGia"] = new SelectList(_context.GiamGia, "MaGiamGia", "MaGiamGia", sanPham.MaGiamGia);
            ViewBag.ChiNhanhs = _context.ChiNhanhs.Where(cn => cn.TrangThai).OrderBy(cn => cn.TenChiNhanh).ToList();
            ViewBag.SelectedChiNhanhs = selectedChiNhanhs?.ToList() ?? new List<int>();
            return View(sanPham);
        }
        // GET: SanPhams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.MaDmNavigation)
                .Include(s => s.MaGiamGiaNavigation)
                .FirstOrDefaultAsync(m => m.MaSp == id);
            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }
        public ActionResult Search(string searchTerm)
        {

            if (string.IsNullOrWhiteSpace(searchTerm))
            {

                return RedirectToAction("TrangChu");
            }


            var searchTermLower = searchTerm.ToLower();

            var searchResults = _context.SanPhams
                .AsNoTracking()
                .Include(p => p.MaDmNavigation)
                .Include(p => p.MaGiamGiaNavigation)
                .Where(p => p.TenSp.ToLower().Contains(searchTermLower))
                .ToList();
            ViewBag.SearchTerm = searchTerm;
            return View("TrangChu", searchResults);
        }

        // POST: SanPhams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Kiểm tra xem sản phẩm có trong đơn hàng không
            bool existsInOrders = await _context.ChiTietDonHangs.AnyAsync(c => c.MaSp == id);
            if (existsInOrders)
            {
                TempData["DeleteError"] = "Không thể xóa! Sản phẩm này đang có trong đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            

            // Xóa sản phẩm nếu không có ràng buộc
            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham != null)
            {
                _context.SanPhams.Remove(sanPham);
                await _context.SaveChangesAsync();
                TempData["DeleteSuccess"] = "Sản phẩm đã được xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> TrangChu()
        {
            await _discountService.ExpireOutdatedDiscountsAsync();

            var products = _context.SanPhams
                .AsNoTracking()
                .Include(p => p.MaDmNavigation)
                .Include(p => p.MaGiamGiaNavigation)
                .ToList();

            // Lấy sản phẩm gợi ý dựa trên số lượng đã được mua
            var suggestedProducts = GetMostPurchasedProducts(4); // Lấy 2 sản phẩm bán chạy nhất

            ViewBag.SuggestedProducts = suggestedProducts;
            ViewBag.DiscountProducts = await _discountService.GetFeaturedDiscountProductsAsync(8);

            return View(products); // Truyền danh sách sản phẩm vào view
        }
        private List<SanPham> GetMostPurchasedProducts(int topN = 5)
        {
                // Lấy top N sản phẩm được mua nhiều nhất
                var mostPurchasedProductIds = _context.ChiTietDonHangs
                    .AsNoTracking()
                    .GroupBy(ct => ct.MaSp) // Nhóm theo mã sản phẩm
                    .OrderByDescending(g => g.Sum(ct => ct.SoLuong)) // Sắp xếp theo số lượng giảm dần
                    .Select(g => g.Key)
                    .Take(topN) // Lấy N sản phẩm mua nhiều nhất
                    .ToList();

                var mostPurchasedProducts = _context.SanPhams
                    .AsNoTracking()
                    .Include(p => p.MaGiamGiaNavigation)
                    .Include(p => p.MaDmNavigation)
                    .Where(p => mostPurchasedProductIds.Contains(p.MaSp))
                    .ToList();

                // Sắp xếp lại danh sách sản phẩm theo thứ tự ID bán chạy nhất
                mostPurchasedProducts = mostPurchasedProducts
                    .OrderBy(p => mostPurchasedProductIds.IndexOf(p.MaSp))
                    .ToList();

                return mostPurchasedProducts;
        }
        private bool SanPhamExists(int id)
        {
            return _context.SanPhams.Any(e => e.MaSp == id);

        }
        public IActionResult SanPhamTheoTenDanhMuc(string TenHang)
        {
            var sanPhams = _context.SanPhams
                .AsNoTracking()
                .Include(sp => sp.MaDmNavigation)
                .Include(sp => sp.MaGiamGiaNavigation)
                .Where(sp => sp.MaDmNavigation.TenDm == TenHang)
                .ToList();

            ViewBag.TenDanhMuc = TenHang; // Truyền tên danh mục cho View
            return View(sanPhams);
        }
        public async Task<List<SanPham>> GetPopularProducts(int topN = 5)
{
    return await _context.ChiTietDonHangs
        .AsNoTracking()
        .GroupBy(ct => ct.MaSp)
        .OrderByDescending(g => g.Sum(ct => ct.SoLuong)) // Sắp xếp theo tổng số lượng bán
        .Take(topN) // Lấy top N sản phẩm
        .Select(g => g.First().MaSpNavigation) // Lấy thông tin sản phẩm
        .ToListAsync();
}

        public IActionResult StoreLocation()
        {
            ViewBag.ApiKey = _configuration["GoogleMaps:ApiKey"];
            return View();
        }

    }
}
