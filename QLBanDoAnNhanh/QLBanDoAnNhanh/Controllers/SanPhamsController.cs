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
using System.Net.Http;
using System.Text.Json;
using QLBanDoAnNhanh.DTOs;

namespace QLBanDoAnNhanh.Controllers
{
    public class SanPhamsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly QlbanDoAnNhanh3Context _context;
        private readonly IProductDiscountService _discountService;
        private readonly IHttpClientFactory _httpClientFactory;

        public SanPhamsController(
            QlbanDoAnNhanh3Context context,
            IConfiguration configuration,
            IProductDiscountService discountService,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _context = context;
            _discountService = discountService;
            _httpClientFactory = httpClientFactory;
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
                .Where(p => p.TenSp.ToLower().Contains(searchTermLower) && p.TrangThai)
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
                .Where(p => p.TrangThai)
                .ToList();

            // Lấy sản phẩm gợi ý dựa trên số lượng đã được mua
            var suggestedProducts = GetMostPurchasedProducts(4); // Lấy 2 sản phẩm bán chạy nhất

            ViewBag.SuggestedProducts = suggestedProducts;
            ViewBag.DiscountProducts = await _discountService.GetFeaturedDiscountProductsAsync(8);

            await ApplyManagedHeroBannersAsync(
                maDm: null,
                fallbackLeft: "/images/Burger-Zinger.jpg",
                fallbackLeftAlt: "Burger Zinger",
                fallbackRight: "/images/Burger-Flava.jpg",
                fallbackRightAlt: "Burger Flava");

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
                    .Where(p => mostPurchasedProductIds.Contains(p.MaSp) && p.TrangThai)
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
        public async Task<IActionResult> SanPhamTheoTenDanhMuc(string TenHang)
        {
            var preferredProducts = _context.SanPhams
                .AsNoTracking()
                .Include(sp => sp.MaDmNavigation)
                .Include(sp => sp.MaGiamGiaNavigation)
                .Where(sp => sp.MaDmNavigation.TenDm == TenHang && sp.TrangThai)
                .ToList();

            var displayedProducts = preferredProducts.Any()
                ? preferredProducts
                : _context.SanPhams
                    .AsNoTracking()
                    .Include(sp => sp.MaDmNavigation)
                    .Include(sp => sp.MaGiamGiaNavigation)
                    .Where(sp => sp.TrangThai && sp.MaDmNavigation.TenDm != TenHang)
                    .OrderByDescending(sp => sp.SlbanTrongNgay ?? 0)
                    .ThenBy(sp => sp.TenSp)
                    .ToList();

            var heroProducts = BuildCategoryHeroProducts(preferredProducts, displayedProducts);

            ViewBag.TenDanhMuc = TenHang;
            ViewBag.IsFallbackCategory = !preferredProducts.Any() && displayedProducts.Any();
            ViewBag.PreferredCategoryCount = preferredProducts.Count;
            ViewBag.HeroLeftImage = heroProducts.LeftImage;
            ViewBag.HeroLeftAlt = heroProducts.LeftAlt;
            ViewBag.HeroRightImage = heroProducts.RightImage;
            ViewBag.HeroRightAlt = heroProducts.RightAlt;

            var maDm = await _context.DanhMucs
                .AsNoTracking()
                .Where(d => d.TenDm == TenHang)
                .Select(d => (int?)d.MaDm)
                .FirstOrDefaultAsync();

            if (maDm.HasValue)
            {
                await ApplyManagedHeroBannersAsync(
                    maDm: maDm,
                    fallbackLeft: heroProducts.LeftImage,
                    fallbackLeftAlt: heroProducts.LeftAlt,
                    fallbackRight: heroProducts.RightImage,
                    fallbackRightAlt: heroProducts.RightAlt);
            }

            return View(displayedProducts);
        }

        /// <summary>
        /// Áp dụng banner admin (nếu có). Chỉ ghi đè vị trí nào đã cấu hình ảnh.
        /// </summary>
        private async Task ApplyManagedHeroBannersAsync(
            int? maDm,
            string fallbackLeft,
            string fallbackLeftAlt,
            string fallbackRight,
            string fallbackRightAlt)
        {
            var query = _context.Banners.AsNoTracking().Where(b => b.TrangThai && !string.IsNullOrWhiteSpace(b.HinhAnh));
            query = maDm == null
                ? query.Where(b => b.MaDm == null)
                : query.Where(b => b.MaDm == maDm);

            var banners = await query
                .OrderBy(b => b.ThuTu)
                .ThenBy(b => b.MaBanner)
                .ToListAsync();

            var left = banners.FirstOrDefault(b => b.ViTri == "Left");
            var right = banners.FirstOrDefault(b => b.ViTri == "Right");

            ViewBag.HeroLeftImage = !string.IsNullOrWhiteSpace(left?.HinhAnh) ? left.HinhAnh : fallbackLeft;
            ViewBag.HeroLeftAlt = !string.IsNullOrWhiteSpace(left?.TieuDe) ? left.TieuDe : fallbackLeftAlt;
            ViewBag.HeroRightImage = !string.IsNullOrWhiteSpace(right?.HinhAnh) ? right.HinhAnh : fallbackRight;
            ViewBag.HeroRightAlt = !string.IsNullOrWhiteSpace(right?.TieuDe) ? right.TieuDe : fallbackRightAlt;
        }

        private (string LeftImage, string LeftAlt, string RightImage, string RightAlt) BuildCategoryHeroProducts(
            List<SanPham> preferredProducts,
            List<SanPham> displayedProducts)
        {
            var heroCandidates = preferredProducts
                .Concat(displayedProducts)
                .Where(sp => !string.IsNullOrWhiteSpace(sp.HinhAnh1))
                .GroupBy(sp => sp.MaSp)
                .Select(g => g.First())
                .Take(2)
                .ToList();

            var leftProduct = heroCandidates.FirstOrDefault();
            var rightProduct = heroCandidates.Skip(1).FirstOrDefault() ?? heroCandidates.FirstOrDefault();

            var leftImage = leftProduct?.HinhAnh1 ?? "/images/Burger-Zinger.jpg";
            var leftAlt = leftProduct?.TenSp ?? "Sản phẩm nổi bật";
            var rightImage = rightProduct?.HinhAnh1 ?? "/images/Burger-Flava.jpg";
            var rightAlt = rightProduct?.TenSp ?? "Sản phẩm nổi bật";

            return (leftImage, leftAlt, rightImage, rightAlt);
        }
        public async Task<List<SanPham>> GetPopularProducts(int topN = 5)
        {
            return await _context.ChiTietDonHangs
                .AsNoTracking()
                .Where(ct => ct.MaSpNavigation.TrangThai)
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

        // ============================================================
        // POST /SanPhams/ToggleStatus/5 – Ẩn/Hiện sản phẩm
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            sanPham.TrangThai = !sanPham.TrangThai;
            _context.Update(sanPham);
            await _context.SaveChangesAsync();

            return Json(new { success = true, trangThai = sanPham.TrangThai });
        }

        // ============================================================
        // POST /SanPhams/ToggleHetHang/5 – Bật/Tắt trạng thái hết hàng
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> ToggleHetHang(int id)
        {
            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            sanPham.HetHang = !sanPham.HetHang;
            _context.Update(sanPham);
            await _context.SaveChangesAsync();

            return Json(new { success = true, hetHang = sanPham.HetHang });
        }

        // ============================================================
        // POST /SanPhams/TinhPhiGiaoHang
        // Tính khoảng cách giữa khách hàng và các chi nhánh có tồn kho
        // bằng Google Maps API & trả về chi nhánh gần nhất + phí giao hàng
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> TinhPhiGiaoHang([FromBody] DTOs.TinhPhiGiaoHangRequest? req)
        {
            Console.WriteLine($"[DEBUG] TinhPhiGiaoHang: req is {(req == null ? "null" : "not null")}, DiaChi={req?.DiaChiKhachHang}, Lat={req?.LatKhachHang}, Lng={req?.LngKhachHang}");
            string? diaChi = req?.DiaChiKhachHang;
            int spId = req?.SanPhamId ?? 0;
            double? latCust = req?.LatKhachHang;
            double? lngCust = req?.LngKhachHang;
            int maChiNhanh = req?.MaChiNhanh ?? 0;
            string criteria = req?.TieuChi ?? "distance";

            try
            {
                if (string.IsNullOrWhiteSpace(diaChi) && Request.Body.CanSeek)
                {
                    Request.EnableBuffering();
                    Request.Body.Position = 0;
                    using var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                    var bodyText = await reader.ReadToEndAsync();

                    if (!string.IsNullOrWhiteSpace(bodyText))
                    {
                        using var doc = JsonDocument.Parse(bodyText);
                        var root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in root.EnumerateObject())
                            {
                                var name = prop.Name.ToLowerInvariant();
                                if (name == "diachikhachhang" || name == "diachi")
                                {
                                    diaChi = prop.Value.GetString()?.Trim();
                                }
                                else if (name == "sanphamid")
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.Number) spId = prop.Value.GetInt32();
                                    else if (prop.Value.ValueKind == JsonValueKind.String && int.TryParse(prop.Value.GetString(), out var parsedId)) spId = parsedId;
                                }
                                else if (name == "latkhachhang" || name == "lat")
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.Number) latCust = prop.Value.GetDouble();
                                    else if (prop.Value.ValueKind == JsonValueKind.String && double.TryParse(prop.Value.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var parsedLat)) latCust = parsedLat;
                                }
                                else if (name == "lngkhachhang" || name == "lng" || name == "lon")
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.Number) lngCust = prop.Value.GetDouble();
                                    else if (prop.Value.ValueKind == JsonValueKind.String && double.TryParse(prop.Value.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var parsedLng)) lngCust = parsedLng;
                                }
                                else if (name == "tieuchi")
                                {
                                    criteria = prop.Value.GetString() ?? "distance";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("JSON Parse error: " + ex.Message);
            }

            if (string.IsNullOrWhiteSpace(diaChi) && Request.HasFormContentType)
            {
                diaChi = Request.Form["diaChiKhachHang"].FirstOrDefault() ?? Request.Form["DiaChiKhachHang"].FirstOrDefault();
                if (int.TryParse(Request.Form["sanPhamId"].FirstOrDefault(), out var id)) spId = id;
                if (double.TryParse(Request.Form["latKhachHang"].FirstOrDefault(), System.Globalization.CultureInfo.InvariantCulture, out var lat)) latCust = lat;
                if (double.TryParse(Request.Form["lngKhachHang"].FirstOrDefault(), System.Globalization.CultureInfo.InvariantCulture, out var lng)) lngCust = lng;
                var tc = Request.Form["tieuChi"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(tc)) criteria = tc;
            }

            if (string.IsNullOrWhiteSpace(diaChi))
            {
                diaChi = Request.Query["diaChiKhachHang"].FirstOrDefault() ?? Request.Query["DiaChiKhachHang"].FirstOrDefault();
            }

            if (!latCust.HasValue || !lngCust.HasValue)
            {
                if (string.IsNullOrWhiteSpace(diaChi))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ địa chỉ giao hàng." });
                }
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // 1. Thử Geocoding qua Google Maps API trước (nếu có API Key)
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            if ((!latCust.HasValue || !lngCust.HasValue) && !string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var geocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(diaChi!)}&key={apiKey}";
                    var response = await httpClient.GetAsync(geocodeUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(jsonString);
                        var root = doc.RootElement;
                        var status = root.GetProperty("status").GetString();
                        if (status == "OK")
                        {
                            var location = root.GetProperty("results")[0].GetProperty("geometry").GetProperty("location");
                            latCust = location.GetProperty("lat").GetDouble();
                            lngCust = location.GetProperty("lng").GetDouble();
                        }
                    }
                }
                catch { }
            }

            // 2. Thử Geocoding qua Photon OpenStreetMap API (Động 100%, Miễn phí, Tốc độ cao)
            if ((!latCust.HasValue || !lngCust.HasValue) && !string.IsNullOrWhiteSpace(diaChi))
            {
                var candidateAddresses = BuildCandidateAddresses(diaChi);
                foreach (var candidateAddress in candidateAddresses)
                {
                    try
                    {
                        var photonUrl = $"https://photon.komoot.io/api/?q={Uri.EscapeDataString(candidateAddress)}&limit=1";
                        var response = await httpClient.GetAsync(photonUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonString = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(jsonString);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("features", out var features) && features.ValueKind == JsonValueKind.Array && features.GetArrayLength() > 0)
                            {
                                var coords = features[0].GetProperty("geometry").GetProperty("coordinates");
                                double lngVal = coords[0].GetDouble();
                                double latVal = coords[1].GetDouble();
                                if (latVal != 0 && lngVal != 0)
                                {
                                    latCust = latVal;
                                    lngCust = lngVal;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            // 3. Fallback Geocoding qua OpenStreetMap Nominatim
            if ((!latCust.HasValue || !lngCust.HasValue) && !string.IsNullOrWhiteSpace(diaChi))
            {
                var candidateAddresses = BuildCandidateAddresses(diaChi);
                foreach (var candidateAddress in candidateAddresses)
                {
                    try
                    {
                        var osmUrl = $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={Uri.EscapeDataString(candidateAddress)}";
                        var response = await httpClient.GetAsync(osmUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonString = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(jsonString);
                            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                            {
                                var item = doc.RootElement[0];
                                if (double.TryParse(item.GetProperty("lat").GetString(), System.Globalization.CultureInfo.InvariantCulture, out var latVal) &&
                                    double.TryParse(item.GetProperty("lon").GetString(), System.Globalization.CultureInfo.InvariantCulture, out var lngVal))
                                {
                                    latCust = latVal;
                                    lngCust = lngVal;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            if (!latCust.HasValue || !lngCust.HasValue)
            {
                return Json(new { success = false, message = "Dịch vụ bản đồ không định vị được tọa độ cho địa chỉ: " + diaChi });
            }

            // 4. Lấy danh sách các chi nhánh THỰC TẾ đang hoạt động từ cơ sở dữ liệu
            var query = _context.ChiNhanhs.AsNoTracking().Where(c => c.TrangThai);
            var danhSachChiNhanh = await query.ToListAsync();

            if (!danhSachChiNhanh.Any())
            {
                return Json(new { success = false, message = "Không có chi nhánh nào đang hoạt động trong hệ thống." });
            }

            if (spId > 0)
            {
                var filtered = danhSachChiNhanh.Where(c => c.SanPhamChiNhanhs.Any(spn => spn.MaSp == spId)).ToList();
                if (filtered.Any())
                {
                    danhSachChiNhanh = filtered;
                }
            }

            var validBranches = danhSachChiNhanh
                .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
                .ToList();

            if (!validBranches.Any())
            {
                return Json(new { success = false, message = "Các chi nhánh trong cơ sở dữ liệu chưa được cập nhật tọa độ GPS (Latitude/Longitude)." });
            }

            // Nếu client gửi maChiNhanh (chi nhánh đang chọn trong giỏ),
            // thì chỉ tính khoảng cách/phí theo đúng chi nhánh đó để tránh lệch km.
            if (maChiNhanh > 0)
            {
                var forced = validBranches.FirstOrDefault(b => b.MaChiNhanh == maChiNhanh);
                if (forced != null)
                {
                    validBranches = new List<ChiNhanh> { forced };
                }
            }

            var results = new List<(ChiNhanh Branch, double DistanceKm, int DurationSec, string DurationText)>();

            // 4. Thử tính khoảng cách qua Google Distance Matrix API trước
            bool googleSuccess = false;
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var originsStr = string.Join("|", validBranches.Select(b => $"{b.Latitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{b.Longitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
                    var destStr = $"{latCust.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lngCust.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                    var distanceMatrixUrl = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={originsStr}&destinations={destStr}&key={apiKey}&mode=walking";

                    var response = await httpClient.GetAsync(distanceMatrixUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(jsonString);
                        var root = doc.RootElement;
                        var status = root.GetProperty("status").GetString();

                        if (status == "OK")
                        {
                            var rows = root.GetProperty("rows");
                            for (int i = 0; i < validBranches.Count; i++)
                            {
                                if (rows.GetArrayLength() > i)
                                {
                                    var elements = rows[i].GetProperty("elements");
                                    if (elements.GetArrayLength() > 0)
                                    {
                                        var element = elements[0];
                                        if (element.GetProperty("status").GetString() == "OK")
                                        {
                                            double distMeters = element.GetProperty("distance").GetProperty("value").GetDouble();
                                            double distKm = Math.Round(distMeters / 1000.0, 2);
                                            int durSec = element.GetProperty("duration").GetProperty("value").GetInt32();
                                            string durText = element.GetProperty("duration").GetProperty("text").GetString() ?? $"{durSec / 60} phút";
                                            results.Add((validBranches[i], distKm, durSec, durText));
                                        }
                                    }
                                }
                            }
                            if (results.Any()) googleSuccess = true;
                        }
                    }
                }
                catch { }
            }

            // 5. Thử tính khoảng cách đường đi bộ thực tế qua OSRM Walking API (Miễn phí, chính xác theo tuyến đường đi bộ)
            if (!googleSuccess || !results.Any())
            {
                results.Clear();
                foreach (var branch in validBranches)
                {
                    try
                    {
                        var branchLonStr = branch.Longitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var branchLatStr = branch.Latitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var custLonStr   = lngCust.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var custLatStr   = latCust.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        var osrmUrl = $"https://router.project-osrm.org/route/v1/foot/{branchLonStr},{branchLatStr};{custLonStr},{custLatStr}?overview=false";
                        var response = await httpClient.GetAsync(osrmUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonString = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(jsonString);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("code", out var code) && code.GetString() == "Ok" &&
                                root.TryGetProperty("routes", out var routes) && routes.ValueKind == JsonValueKind.Array && routes.GetArrayLength() > 0)
                            {
                                var route0 = routes[0];
                                double distMeters = route0.GetProperty("distance").GetDouble();
                                double durSeconds = route0.GetProperty("duration").GetDouble();

                                double distKm = Math.Round(distMeters / 1000.0, 2);
                                int durMin = Math.Max(1, (int)Math.Round(durSeconds / 60.0));
                                int durSec = (int)Math.Round(durSeconds);
                                string durText = $"{durMin} phút đi bộ";

                                results.Add((branch, distKm, durSec, durText));
                            }
                        }
                    }
                    catch { }
                }
            }

            if (!results.Any())
            {
                return Json(new { success = false, message = "Không thể tính toán khoảng cách giao hàng bằng lộ trình đường đi bộ thực tế." });
            }

            // 6. Chọn chi nhánh tối ưu nhất
            var bestOption = criteria.ToLower() == "duration"
                ? results.OrderBy(r => r.DurationSec).ThenBy(r => r.DistanceKm).First()
                : results.OrderBy(r => r.DistanceKm).ThenBy(r => r.DurationSec).First();

            double distanceKm = bestOption.DistanceKm;
            string durationText = bestOption.DurationText;
            int durationSec = bestOption.DurationSec;

            // Nếu khoảng cách tính ra <= 0 km (trùng tọa độ phường/vị trí chi nhánh do fallback geocoding)
            // thì gán khoảng cách tối thiểu là 0.5 km và thời gian là 5 phút để tránh hiển thị 0 km và phí giao hàng 0đ
            if (distanceKm <= 0)
            {
                distanceKm = 0.5;
                durationText = "5 phút";
                durationSec = 300;
            }

            decimal phiGiaoHang = TinhPhiGiaoHangTheoKhoangCach(distanceKm);

            return Json(new
            {
                success = true,
                chiNhanhId = bestOption.Branch.MaChiNhanh,
                tenChiNhanh = bestOption.Branch.TenChiNhanh,
                diaChiChiNhanh = bestOption.Branch.DiaChi,
                soDienThoaiChiNhanh = bestOption.Branch.SoDienThoai,
                latKhachHang = latCust,
                lngKhachHang = lngCust,
                khoangCachKm = distanceKm,
                thoiGianText = durationText,
                thoiGianGiay = durationSec,
                phiGiaoHang = phiGiaoHang,
                phiGiaoHangFormatted = phiGiaoHang.ToString("N0") + "đ",
                message = "Tính phí giao hàng thành công."
            });
        }

        /// <summary>
        /// Tính khoảng cách Haversine (km) giữa 2 tọa độ lat/lng và nhân hệ số đường bộ thực tế (1.25x)
        /// </summary>
        private static double CalculateHaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Bán kính Trái Đất (km)
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double directKm = R * c;
            return Math.Round(directKm * 1.25, 2); // Hệ số uớc tính tuyến đường bộ Việt Nam
        }

        /// <summary>
        /// Hàm tính phí giao hàng theo bậc thang khoảng cách (km).
        /// Dễ dàng điều chỉnh tỷ lệ và các mức phí.
        /// </summary>
        private decimal TinhPhiGiaoHangTheoKhoangCach(double distanceKm)
        {
            if (distanceKm < 0) return 0;
            if (distanceKm <= 3) return 15000;
            if (distanceKm <= 7) return 25000;
            if (distanceKm <= 15) return 40000;

            // > 15km: 40,000đ + 3,000đ/km vượt (làm tròn lên km vượt)
            double kmVuot = Math.Ceiling(distanceKm - 15);
            return 40000 + (decimal)(kmVuot * 3000);
        }

        private static List<string> BuildCandidateAddresses(string diaChi)
        {
            var candidates = new List<string>();
            if (string.IsNullOrWhiteSpace(diaChi)) return candidates;

            var parts = diaChi.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
            if (!parts.Any()) return candidates;

            // 1. Nguyên văn địa chỉ đầy đủ bao gồm số nhà
            string fullRaw = string.Join(", ", parts);
            if (!fullRaw.Contains("Việt Nam", StringComparison.OrdinalIgnoreCase)) fullRaw += ", Việt Nam";
            candidates.Add(fullRaw);

            // 2. Tách tên đường (bỏ số nhà/hẻm/lô ở phần đầu) để nếu số nhà không có trên bản đồ thì vẫn định vị đúng tên đường
            string firstPart = parts[0];
            string streetOnly = System.Text.RegularExpressions.Regex.Replace(firstPart, @"^(?:Số|Hẻm|Ngõ|Ngách|Lô|Căn|Phòng)?\s*[\d\w\/\.\-]+\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            if (!string.IsNullOrWhiteSpace(streetOnly) && !streetOnly.Equals(firstPart, StringComparison.OrdinalIgnoreCase))
            {
                var streetParts = new List<string> { streetOnly };
                streetParts.AddRange(parts.Skip(1));
                string streetCandidate = string.Join(", ", streetParts);
                if (!streetCandidate.Contains("Việt Nam", StringComparison.OrdinalIgnoreCase)) streetCandidate += ", Việt Nam";
                if (!candidates.Contains(streetCandidate)) candidates.Add(streetCandidate);
            }

            // 2b. Thêm địa chỉ tối giản để OpenStreetMap tìm kiếm dễ hơn (bỏ các tiền tố Phường, Quận, Thành phố...)
            if (!string.IsNullOrWhiteSpace(streetOnly))
            {
                var cleanParts = new List<string> { streetOnly };
                foreach (var part in parts.Skip(1))
                {
                    string cleaned = part
                        .Replace("Thành phố", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Tỉnh", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Quận", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Huyện", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Thị xã", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Phường", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Xã", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Thị trấn", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();
                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        cleanParts.Add(cleaned);
                    }
                }
                string cleanCandidate = string.Join(", ", cleanParts);
                if (!cleanCandidate.Contains("Việt Nam", StringComparison.OrdinalIgnoreCase)) cleanCandidate += ", Việt Nam";
                if (!candidates.Contains(cleanCandidate)) candidates.Add(cleanCandidate);

                // Thêm phiên bản cực giản chỉ gồm Tên đường + Quận + Thành phố
                if (parts.Count >= 3)
                {
                    string cleanDistrict = parts[parts.Count - 2]
                        .Replace("Quận", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Huyện", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Thị xã", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();
                    string cleanCity = parts[parts.Count - 1]
                        .Replace("Thành phố", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("Tỉnh", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();
                    string minimalCandidate = $"{streetOnly}, {cleanDistrict}, {cleanCity}, Việt Nam";
                    if (!candidates.Contains(minimalCandidate)) candidates.Add(minimalCandidate);
                }
            }

            // 3. Giảm dần các cấp địa bàn (Phường/Xã -> Quận/Huyện -> Tỉnh/Thành)
            for (int i = 1; i < parts.Count; i++)
            {
                string subCandidate = string.Join(", ", parts.Skip(i));
                if (!subCandidate.Contains("Việt Nam", StringComparison.OrdinalIgnoreCase)) subCandidate += ", Việt Nam";
                if (!candidates.Contains(subCandidate)) candidates.Add(subCandidate);
            }

            return candidates;
        }
    }
}
