using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Models;

public partial class SanPham
{
    public int MaSp { get; set; }

    public string TenSp { get; set; }

    public int MaGiamGia { get; set; }

    public string ThanhPhan { get; set; }

    public double GiaTien { get; set; }

    public double DonVi { get; set; }

    public string ChitietSp { get; set; }

    public int MaDm { get; set; }

    public int? SlbanTrongNgay { get; set; }

    public string HinhAnh1 { get; set; }

    public string HinhAnh2 { get; set; }

    public virtual ICollection<BinhLuan> BinhLuans { get; set; } = new List<BinhLuan>();

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<HinhAnh> HinhAnhs { get; set; } = new List<HinhAnh>();

    public virtual DanhMuc MaDmNavigation { get; set; }

    public virtual GiamGium MaGiamGiaNavigation { get; set; }
}
