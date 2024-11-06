using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Data;

public partial class DanhGium
{
    public int MaDanhGia { get; set; }

    public int MaSanPham { get; set; }

    public int MaNguoiDung { get; set; }

    public decimal? SoSao { get; set; }

    public DateTime NgayBinhLuan { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; }

    public virtual SanPham MaSanPhamNavigation { get; set; }
}
