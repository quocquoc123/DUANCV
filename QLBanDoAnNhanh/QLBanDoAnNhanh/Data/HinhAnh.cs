using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Data;

public partial class HinhAnh
{
    public int Id { get; set; }

    public string UrlHinh { get; set; }

    public int? MaSp { get; set; }

    public virtual SanPham MaSpNavigation { get; set; }
}
