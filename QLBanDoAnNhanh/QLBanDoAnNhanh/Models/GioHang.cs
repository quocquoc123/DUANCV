using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Models;

public partial class GioHang
{
    public int MaGh { get; set; }

    public int? MaNguoiDung { get; set; }

    public int? SoLuong { get; set; }

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual NguoiDung MaNguoiDungNavigation { get; set; }
}
