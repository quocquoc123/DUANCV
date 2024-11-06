using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Data;

public partial class BinhLuan
{
    public int MaBinhLuan { get; set; }

    public int MaSp { get; set; }

    public int MaNguoiDung { get; set; }

    public string NoiDung { get; set; }

    public DateTime NgayBinhLuan { get; set; }

    public virtual NguoiDung MaNguoiDungNavigation { get; set; }

    public virtual SanPham MaSpNavigation { get; set; }
}
