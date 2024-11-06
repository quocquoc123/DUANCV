using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Data;

public partial class PhanQuyen
{
    public int RoleId { get; set; }

    public string RoleName { get; set; }

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}
