using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Models;

public partial class KhuyenMai
{
    public string MaKhuyenMai { get; set; }

    public int GiaTri { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public bool TrangThai { get; set; }

    public DateTime NgayTao { get; set; }

    public int? DieuKienApDung { get; set; }

    public int SoLuong { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
