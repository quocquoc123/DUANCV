using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Models;

public partial class SanPhamYeuThich
{
    public int WishlistId { get; set; }

    public int MaNguoiDung { get; set; }

    public int MaSp { get; set; }

    public DateTime NgayThem { get; set; }

    // Navigation properties
    public virtual NguoiDung NguoiDung { get; set; }

    public virtual SanPham SanPham { get; set; }
}
