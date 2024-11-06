using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Data;

public partial class ThanhToan
{
    public int MaThanhToan { get; set; }

    public string MaDh { get; set; }

    public string PhuongThucThanhToan { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public double TongTien { get; set; }

    public bool TrangThaiThanhToan { get; set; }

    public virtual DonHang MaDhNavigation { get; set; }
}
