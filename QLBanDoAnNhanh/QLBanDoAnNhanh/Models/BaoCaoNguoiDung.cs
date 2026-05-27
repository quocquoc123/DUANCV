using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Models;

public partial class BaoCaoNguoiDung
{
    public int MaBaoCao { get; set; }

    public int MaNguoiDung { get; set; }

    public string PhanHoi { get; set; }

    public DateTime? NgayTao { get; set; }

    public int? TrangThai { get; set; }

    public virtual NguoiDung NguoiDung { get; set; }

    public virtual ICollection<PhanHoiBaoCao> PhanHoiBaoCaos { get; set; } = new List<PhanHoiBaoCao>();
}
