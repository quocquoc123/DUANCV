using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Models;

public partial class DonHang
{
    public string MaDh { get; set; }

    public string Username { get; set; }

    public string Diachi { get; set; }

    public string MaKhuyenMai { get; set; }

    public double TongTien { get; set; }

    public int SoLuong { get; set; }

    public string TrangThai { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? MaNguoiDung { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual KhuyenMai MaKhuyenMaiNavigation { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; }

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}
