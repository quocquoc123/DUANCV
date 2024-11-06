using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Models;

public partial class DanhMuc
{
    public int MaDm { get; set; }

    public string TenDm { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
