using System;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using QLBanDoAnNhanh.Models;

public class InvoiceService
{
    public MemoryStream GenerateInvoice(DonHang donHang)
    {
        var stream = new MemoryStream();
        var pdfWriter = new PdfWriter(stream);
        var pdf = new PdfDocument(pdfWriter);
        var document = new Document(pdf);

        // Tiêu đề hóa đơn
        document.Add(new Paragraph("HÓA ĐƠN THAM KHẢO")
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(20));

        // Thông tin đơn hàng
        document.Add(new Paragraph($"Mã Đơn Hàng: {donHang.MaDh}"));
        document.Add(new Paragraph($"Tên Người Dùng: {donHang.Username}"));
        document.Add(new Paragraph($"Địa Chỉ: {donHang.Diachi}"));
        
        document.Add(new Paragraph($"Tổng Tiền: {donHang.TongTien} VND"));
        document.Add(new Paragraph($"Trạng Thái: {donHang.TrangThai}"));
        document.Add(new Paragraph(""));

        // Thêm bảng chi tiết đơn hàng
        Table table = new Table(new float[] { 1, 3, 2, 2 }); // Số cột: STT, Tên SP, Số lượng, Thành tiền
        table.AddHeaderCell("STT");
        table.AddHeaderCell("Tên Sản Phẩm");
        table.AddHeaderCell("Số Lượng");
        table.AddHeaderCell("Thành Tiền (VNĐ)");

        int stt = 1;
        foreach (var chiTiet in donHang.ChiTietDonHangs)
        {
            table.AddCell(stt.ToString());
            table.AddCell(chiTiet.MaSpNavigation.TenSp);
            table.AddCell(chiTiet.SoLuong.ToString());
            table.AddCell(chiTiet.TongTien.ToString());
            stt++;
        }

        document.Add(table);
        document.Close();

        stream.Position = 0; // Reset vị trí của stream trước khi trả về
        return stream;
    }
}
