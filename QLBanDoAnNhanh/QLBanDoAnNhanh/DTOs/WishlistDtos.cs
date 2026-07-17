namespace QLBanDoAnNhanh.DTOs;

public class WishlistItemDto
{
    public int WishlistId { get; set; }
    public int MaSp { get; set; }
    public string TenSp { get; set; }
    public double GiaTien { get; set; }
    public decimal? GiaSauGiam { get; set; }
    public bool IsDiscount { get; set; }
    public decimal DiscountPercent { get; set; }
    public string HinhAnh1 { get; set; }
    public string TenDanhMuc { get; set; }
    public double DiemDanhGia { get; set; }
    public int SoLuotDanhGia { get; set; }
    public DateTime NgayThem { get; set; }
}

public class WishlistToggleResultDto
{
    public bool Success { get; set; }
    public bool IsWishlisted { get; set; }
    public int Count { get; set; }
    public string Message { get; set; }
    public bool RequireLogin { get; set; }
}

public class AdminWishlistItemDto
{
    public int WishlistId { get; set; }
    public int MaNguoiDung { get; set; }
    public string HoTen { get; set; }
    public string Email { get; set; }
    public int MaSp { get; set; }
    public string TenSp { get; set; }
    public string HinhAnh1 { get; set; }
    public double GiaTien { get; set; }
    public string TenDanhMuc { get; set; }
    public DateTime NgayThem { get; set; }
}

public class WishlistStatsDto
{
    public int TongLuotYeuThich { get; set; }
    public int TongSanPhamDuocYeuThich { get; set; }
    public List<TopWishlistProductDto> Top10SanPham { get; set; } = new();
}

public class TopWishlistProductDto
{
    public int MaSp { get; set; }
    public string TenSp { get; set; }
    public string HinhAnh1 { get; set; }
    public int SoLuotYeuThich { get; set; }
    public double GiaTien { get; set; }
}
