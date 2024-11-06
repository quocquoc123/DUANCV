using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Models;

public partial class GiamGium
{
    public int MaGiamGia { get; set; }

    public int GiaTri { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
