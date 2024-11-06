using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Data;

public partial class ChiTietGioHang
{
    public int Id { get; set; }

    public int? MaGh { get; set; }

    public int? SoLuongSp { get; set; }

    public int? MaSp { get; set; }

    public double? TongTien { get; set; }

    public virtual GioHang MaGhNavigation { get; set; }

    public virtual SanPham MaSpNavigation { get; set; }
}
