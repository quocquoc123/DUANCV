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
    private QlbanDoAnNhanh3Context db = new QlbanDoAnNhanh3Context();

    private readonly QlbanDoAnNhanh3Context _context; // Thay đổi theo DbContext của bạn

    public DonHangsController(QlbanDoAnNhanh3Context context)
    {
        _context = context;
    }

    // GET: DonHangs
    public async Task<IActionResult> Index(string trangThai = null)
    {
        var query = _context.DonHangs.AsQueryable();

        // Lọc theo trạng thái nếu có
        if (!string.IsNullOrEmpty(trangThai))
        {
            query = query.Where(d => d.TrangThai == trangThai);
        }

        var donHangs = await query.ToListAsync();
        ViewBag.TrangThai = trangThai; // Truyền trạng thái hiện tại về view
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
    public async Task<IActionResult> UpdateTrangThai_ChuaGiao(string id)
    {
        return await UpdateTrangThai(id, "Chưa Giao");
    }

    public async Task<IActionResult> UpdateTrangThai_DaGiao(string id)
    {
        return await UpdateTrangThai(id, "Đã Giao");
    }

    public async Task<IActionResult> UpdateTrangThai_DangGiao(string id)
    {
        return await UpdateTrangThai(id, "Đang Giao");
    }

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
        // Tìm đơn hàng và các chi tiết đơn hàng liên quan
        var donHang = await _context.DonHangs.FindAsync(id);
        if (donHang != null)
        {
            var chiTietDonHang = _context.ChiTietDonHangs.Where(ct => ct.MaDh == id);

            // Xóa các chi tiết đơn hàng trước
            _context.ChiTietDonHangs.RemoveRange(chiTietDonHang);

            // Xóa đơn hàng
            _context.DonHangs.Remove(donHang);

            await _context.SaveChangesAsync();
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

        var searchResults = db.DonHangs
            .Where(p => p.TrangThai.ToLower().Contains(searchTermLower))
            .ToList();
        ViewBag.SearchTerm = searchTerm;
        return View("Index", searchResults);
    }
    public IActionResult DoanhThu() 
    {
        var doanhThu = _context.ChiTietDonHangs
            .Join(_context.DonHangs, cdh => cdh.MaDh, dh => dh.MaDh, (cdh, dh) => new { cdh, dh })
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
    public IActionResult ExportInvoiceToPdf(string maDh)
    {

        // Lấy thông tin đơn hàng và chi tiết đơn hàng
        using (var context = new QlbanDoAnNhanh3Context())
        {
            var order = context.DonHangs
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
                PdfWriter writer = new PdfWriter(memoryStream);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf);

                // Thêm tiêu đề
                document.Add(new Paragraph("Hóa Đơn").SetFontSize(20).SetBold().SetTextAlignment(TextAlignment.CENTER));
                document.Add(new Paragraph($"Mã đơn hàng: {order.MaDh}").SetTextAlignment(TextAlignment.LEFT));
                document.Add(new Paragraph($"Khách hàng: {order.Username}").SetTextAlignment(TextAlignment.LEFT));
                document.Add(new Paragraph($"Ngày đặt hàng: {order.CreatedAt}").SetTextAlignment(TextAlignment.LEFT));
                document.Add(new Paragraph(" ")); // Thêm khoảng trắng

                // Thêm tiêu đề bảng
                Table table = new Table(UnitValue.CreatePercentArray(4)).UseAllAvailableWidth();
                table.AddHeaderCell("Sản Phẩm");
                table.AddHeaderCell("Số Lượng");
                table.AddHeaderCell("Đơn Giá");
                table.AddHeaderCell("Tổng Cộng");

                // Điền dữ liệu vào bảng với thông tin sản phẩm
                foreach (var detail in order.ChiTietDonHangs)
                {
                    table.AddCell(detail.MaSpNavigation.TenSp); // Tên sản phẩm
                    table.AddCell(detail.SoLuong.ToString()); // Số lượng
                    table.AddCell(detail.MaSpNavigation.GiaTien.ToString("C")); // Đơn giá
                    table.AddCell(detail.TongTien.ToString("C")); // Tổng tiền cho sản phẩm
                }

                // Tổng tiền
                table.AddCell(new Cell(1, 3).Add(new Paragraph("Tổng Tiền").SetBold()));
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


