using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;  

public class DonHangsController : Controller
{
    private readonly QlbanDoAnNhanh3Context _context; // Thay đổi theo DbContext của bạn

    public DonHangsController(QlbanDoAnNhanh3Context context)
    {
        _context = context;
    }

    // GET: DonHangs
    public async Task<IActionResult> Index(string trangThai = null, string search = null)
    {
        var query = _context.DonHangs.AsNoTracking().AsQueryable();

        // Lọc theo trạng thái nếu có
        if (!string.IsNullOrEmpty(trangThai))
        {
            query = query.Where(d => d.TrangThai == trangThai);
        }

        // Tìm kiếm theo mã đơn hoặc tên khách hàng
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(d =>
                d.MaDh.ToLower().Contains(keyword) ||
                d.Username.ToLower().Contains(keyword));
        }

        var donHangs = await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        ViewBag.TrangThai = trangThai;
        ViewBag.Search = search;
        return View(donHangs);
    }


    // GET: DonHangs/Details/5
    public async Task<IActionResult> Details(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var donHang = await _context.DonHangs
            .AsNoTracking()
            .Include(dh => dh.ThanhToans)
            .Include(dh => dh.ChiTietDonHangs)
                .ThenInclude(ct => ct.MaSpNavigation)
            .Include(dh => dh.MaKhuyenMaiNavigation)
            .Include(dh => dh.MaNguoiDungNavigation)
            .FirstOrDefaultAsync(m => m.MaDh == id);
        if (donHang == null)
        {
            return NotFound();
        }

        return View(donHang);
    }

    // GET: DonHangs/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var donHang = await _context.DonHangs.FindAsync(id);
        if (donHang == null)
        {
            return NotFound();
        }
        return View(donHang);
    }

    // POST: DonHangs/Edit/5
    // POST: DonHangs/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("MaDh,Username,TrangThai,Diachi,MaKhuyenMai,TongTien,SoLuong,CreatedAt,UpdatedAt")] DonHang donHang)
    {
        if (id != donHang.MaDh)
        {
            return NotFound();
        }


        // Kiểm tra nếu Username bị bỏ trống
        if (string.IsNullOrEmpty(donHang.Username))
        {
            ModelState.AddModelError("Username", "Tên người dùng không được bỏ trống.");
            return View(donHang);
        }

        // Kiểm tra MaKhuyenMai có hợp lệ hay không
        var khuyenMai = await _context.KhuyenMais.FindAsync(donHang.MaKhuyenMai);
        if (khuyenMai == null)
        {
            ModelState.AddModelError("MaKhuyenMai", "Mã khuyến mãi không hợp lệ.");
            return View(donHang);
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Không thay đổi MaKhuyenMai nếu không cần
                _context.Entry(donHang).Property(d => d.MaKhuyenMai).IsModified = false;

                // Cập nhật các thuộc tính khác
                _context.Update(donHang);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DonHangExists(donHang.MaDh))
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
        return View(donHang);
    }


    // Cập nhật trạng thái đơn hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTrangThai_ChuaGiao(string id)
    {
        return await UpdateTrangThai(id, "Chưa Giao");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTrangThai_DaGiao(string id)
    {
        return await UpdateTrangThai(id, "Đã Giao");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTrangThai_DangGiao(string id)
    {
        return await UpdateTrangThai(id, "Đang Giao");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTrangThai_DaHuy(string id)
    {
        return await UpdateTrangThai(id, "Đã Hủy");
    }

    private async Task<IActionResult> UpdateTrangThai(string id, string trangThai)
    {
        if (id == null)
        {
            return NotFound();
        }

        var donHang = await _context.DonHangs.FindAsync(id);
        if (donHang == null)
        {
            return NotFound();
        }

        donHang.TrangThai = trangThai; // Cập nhật trạng thái
        donHang.UpdatedAt = DateTime.Now; // Cập nhật thời gian chỉnh sửa
        _context.Entry(donHang).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã chuyển đơn hàng {id} sang trạng thái \u201c{trangThai}\u201d.";
        return RedirectToAction(nameof(Index));
    }

    // GET: DonHangs/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var donHang = await _context.DonHangs
            .FirstOrDefaultAsync(m => m.MaDh == id);
        if (donHang == null)
        {
            return NotFound();
        }

        return View(donHang);
    }

    // POST: DonHangs/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var donHang = await _context.DonHangs.FindAsync(id);
        if (donHang != null)
        {
            var chiTietDonHang = _context.ChiTietDonHangs.Where(ct => ct.MaDh == id);

            // Xóa chi tiết đơn hàng trước
            _context.ChiTietDonHangs.RemoveRange(chiTietDonHang);

            // Xóa đơn hàng
            _context.DonHangs.Remove(donHang);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa đơn hàng {id} thành công.";
        }
        return RedirectToAction(nameof(Index));

    }

    // Kiểm tra tồn tại đơn hàng
    private bool DonHangExists(string id)
    {
        return _context.DonHangs.Any(e => e.MaDh == id);
    }
    public ActionResult Search(string searchTerm)
    {

        if (string.IsNullOrWhiteSpace(searchTerm))
        {

            return RedirectToAction("Index");
        }


        var searchTermLower = searchTerm.ToLower();

        var searchResults = _context.DonHangs
            .AsNoTracking()
            .Where(p => p.TrangThai.ToLower().Contains(searchTermLower))
            .ToList();
        ViewBag.SearchTerm = searchTerm;
        return View("Index", searchResults);
    }
    public IActionResult DoanhThu() 
    {
        var doanhThu = _context.ChiTietDonHangs
            .AsNoTracking()
            .Join(_context.DonHangs, cdh => cdh.MaDh, dh => dh.MaDh, (cdh, dh) => new { cdh, dh })
            .Where(x => x.dh.TrangThai == "Đã Giao") // Chỉ lấy đơn hàng có trạng thái "Đã Giao"
            .Join(_context.SanPhams, combined => combined.cdh.MaSp, sp => sp.MaSp, (combined, sp) => new { combined.cdh, combined.dh, sp })
            .Join(_context.DanhMucs, combined => combined.sp.MaDm, dm => dm.MaDm, (combined, dm) => new

            {

                DanhMuc = dm.TenDm,
                DoanhThu = combined.cdh.TongTien
            })
            .GroupBy(x => x.DanhMuc)
            .Select(g => new
            {
                DanhMuc = g.Key,
                DoanhThu = g.Sum(x => x.DoanhThu)
            })
            .ToList();

        return View(doanhThu);
    }
    public IActionResult SoLuongDaBan()
    {
        var soLuongBan = _context.ChiTietDonHangs
            .AsNoTracking()
            .Join(_context.DonHangs, cdh => cdh.MaDh, dh => dh.MaDh, (cdh, dh) => new { cdh, dh })
            .Where(x => x.dh.TrangThai == "Đã Giao")
            .Join(_context.SanPhams, x => x.cdh.MaSp, sp => sp.MaSp, (x, sp) => new { x.cdh, sp })
            .GroupBy(x => new { x.sp.MaSp, x.sp.TenSp })
            .Select(g => new
            {
                MaSanPham = g.Key.MaSp,
                TenSanPham = g.Key.TenSp,
                SoLuongDaBan = g.Sum(x => x.cdh.SoLuong)
            })
            .OrderByDescending(x => x.SoLuongDaBan)
            .ToList();

        // Xác định số lượng bán cao nhất và thấp nhất
        var maxSoLuongDaBan = soLuongBan.Max(x => x.SoLuongDaBan);
        var minSoLuongDaBan = soLuongBan.Min(x => x.SoLuongDaBan);

        ViewBag.TenSanPham = soLuongBan.Select(x => x.TenSanPham).ToArray();
        ViewBag.SoLuongDaBan = soLuongBan.Select(x => x.SoLuongDaBan).ToArray();

        var danhGiaSanPham = soLuongBan.Select(x => new
        {
            MaSanPham = x.MaSanPham,
            TenSanPham = x.TenSanPham,
            SoLuongDaBan = x.SoLuongDaBan,
            NhanXet = GetProductComment(x.SoLuongDaBan, maxSoLuongDaBan, minSoLuongDaBan), // Truyền maxSoLuongDaBan và minSoLuongDaBan vào
            Url = Url.Action("Details", "SanPhams", new { id = x.MaSanPham })
        }).ToList();

        ViewBag.DanhGiaSanPham = danhGiaSanPham;

        return View();
    }

    // Hàm nhận xét sản phẩm, với maxSoLuongDaBan và minSoLuongDaBan để xác định sản phẩm bán chạy nhất và ít được mua nhất
    private string GetProductComment(int soLuongDaBan, int maxSoLuongDaBan, int minSoLuongDaBan)
    {
        if (soLuongDaBan == maxSoLuongDaBan)
            return "Sản phẩm bán chạy nhất!";
        else if (soLuongDaBan == minSoLuongDaBan)
            return "Sản phẩm ít được mua.";
        else if (soLuongDaBan >= 50)
            return "Sản phẩm khá phổ biến.";
        else if (soLuongDaBan >= 20)
            return "Sản phẩm đang được ưa chuộng.";
        else
            return "Sản phẩm ít được mua.";
    }

  

    public IActionResult ExportInvoiceToPdf(string maDh)
    {

        // Lấy thông tin đơn hàng và chi tiết đơn hàng
        {
            var order = _context.DonHangs
                .AsNoTracking()
                .Include(o => o.ChiTietDonHangs)
                .ThenInclude(od => od.MaSpNavigation)
                .FirstOrDefault(o => o.MaDh == maDh);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng");
            }

            using (var memoryStream = new MemoryStream())
            {
                // Tạo một tài liệu PDF mới
                // Đảm bảo rằng font được lưu trong thư mục wwwroot/Roboto
                string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Roboto", "Roboto-Regular.ttf"); 
                PdfWriter writer = new PdfWriter(memoryStream);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf);

                // Thêm tiêu đề
                document.Add(new Paragraph("Hoa Don").SetFontSize(20).SetBold().SetTextAlignment(TextAlignment.CENTER));
                document.Add(new Paragraph($"Ma Don Hang: {order.MaDh}").SetTextAlignment(TextAlignment.LEFT));
                document.Add(new Paragraph($"Khach Hang: {order.Username}").SetTextAlignment(TextAlignment.LEFT));
                document.Add(new Paragraph($"Ngay Dat Hang: {order.CreatedAt}").SetTextAlignment(TextAlignment.LEFT));
                document.Add(new Paragraph(" ")); // Thêm khoảng trắng

                // Thêm tiêu đề bảng
                Table table = new Table(UnitValue.CreatePercentArray(4)).UseAllAvailableWidth();
                table.AddHeaderCell("San Pham");
                table.AddHeaderCell("So Luong");
                table.AddHeaderCell("Don Gia");
                table.AddHeaderCell("Tong Cong");

                // Điền dữ liệu vào bảng với thông tin sản phẩm
                foreach (var detail in order.ChiTietDonHangs)
                {
                    table.AddCell(detail.MaSpNavigation.TenSp); // Tên sản phẩm
                    table.AddCell(detail.SoLuong.ToString()); // Số lượng
                    table.AddCell(detail.MaSpNavigation.GiaTien.ToString("C")); // Đơn giá
                    table.AddCell(detail.TongTien.ToString("C")); // Tổng tiền cho sản phẩm
                }

                // Tổng tiền
                table.AddCell(new Cell(1, 3).Add(new Paragraph("Tong Tien").SetBold()));
                table.AddCell(order.TongTien.ToString("C"));

                document.Add(table); // Thêm bảng vào tài liệu

                // Đóng tài liệu
                document.Close();

                // Trả về file PDF
                return File(memoryStream.ToArray(), "application/pdf", $"HoaDon_{order.MaDh}.pdf");
            }
        }
    }
}
