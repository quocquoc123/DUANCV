using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.DTOs;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly QlbanDoAnNhanh3Context _context;

    public WishlistRepository(QlbanDoAnNhanh3Context context)
    {
        _context = context;
    }

    public async Task<List<WishlistItemDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;

        var items = await _context.SanPhamYeuThichs
            .AsNoTracking()
            .Where(w => w.MaNguoiDung == userId)
            .Include(w => w.SanPham)
                .ThenInclude(sp => sp.MaDmNavigation)
            .Include(w => w.SanPham)
                .ThenInclude(sp => sp.MaGiamGiaNavigation)
            .Include(w => w.SanPham)
                .ThenInclude(sp => sp.DanhGia)
            .OrderByDescending(w => w.NgayThem)
            .Select(w => new WishlistItemDto
            {
                WishlistId = w.WishlistId,
                MaSp = w.MaSp,
                TenSp = w.SanPham.TenSp,
                GiaTien = w.SanPham.GiaTien,
                HinhAnh1 = w.SanPham.HinhAnh1,
                TenDanhMuc = w.SanPham.MaDmNavigation != null ? w.SanPham.MaDmNavigation.TenDm : "",
                IsDiscount = w.SanPham.MaGiamGiaNavigation != null
                             && w.SanPham.MaGiamGiaNavigation.GiaTri > 0
                             && w.SanPham.MaGiamGiaNavigation.ThoiGianBatDau <= now
                             && w.SanPham.MaGiamGiaNavigation.ThoiGianKetThuc >= now,
                DiscountPercent = w.SanPham.MaGiamGiaNavigation != null
                                  && w.SanPham.MaGiamGiaNavigation.GiaTri > 0
                                  && w.SanPham.MaGiamGiaNavigation.ThoiGianBatDau <= now
                                  && w.SanPham.MaGiamGiaNavigation.ThoiGianKetThuc >= now
                                  ? w.SanPham.MaGiamGiaNavigation.GiaTri : 0,
                GiaSauGiam = w.SanPham.MaGiamGiaNavigation != null
                             && w.SanPham.MaGiamGiaNavigation.GiaTri > 0
                             && w.SanPham.MaGiamGiaNavigation.ThoiGianBatDau <= now
                             && w.SanPham.MaGiamGiaNavigation.ThoiGianKetThuc >= now
                             ? (decimal?)(Math.Round((decimal)w.SanPham.GiaTien
                                - (decimal)w.SanPham.GiaTien * w.SanPham.MaGiamGiaNavigation.GiaTri / 100, 0))
                             : null,
                DiemDanhGia = w.SanPham.DanhGia.Any()
                              ? (double)w.SanPham.DanhGia.Average(d => (double)d.SoSao)
                              : 0,
                SoLuotDanhGia = w.SanPham.DanhGia.Count(),
                NgayThem = w.NgayThem
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<int> GetCountByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.SanPhamYeuThichs
            .AsNoTracking()
            .CountAsync(w => w.MaNguoiDung == userId, cancellationToken);
    }

    public async Task<bool> IsWishlistedAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        return await _context.SanPhamYeuThichs
            .AsNoTracking()
            .AnyAsync(w => w.MaNguoiDung == userId && w.MaSp == productId, cancellationToken);
    }

    public async Task<SanPhamYeuThich> AddAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        var item = new SanPhamYeuThich
        {
            MaNguoiDung = userId,
            MaSp = productId,
            NgayThem = DateTime.Now
        };
        _context.SanPhamYeuThichs.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<bool> RemoveAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        var item = await _context.SanPhamYeuThichs
            .FirstOrDefaultAsync(w => w.MaNguoiDung == userId && w.MaSp == productId, cancellationToken);

        if (item == null) return false;

        _context.SanPhamYeuThichs.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(List<AdminWishlistItemDto> Items, int Total)> GetAllForAdminAsync(
        string searchKeyword,
        int? filterUserId,
        int? filterProductId,
        DateTime? fromDate,
        DateTime? toDate,
        string sortBy,
        string sortDir,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SanPhamYeuThichs
            .AsNoTracking()
            .Include(w => w.NguoiDung)
            .Include(w => w.SanPham)
                .ThenInclude(sp => sp.MaDmNavigation)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(w =>
                w.NguoiDung.HoTen.ToLower().Contains(kw) ||
                w.NguoiDung.Email.ToLower().Contains(kw) ||
                w.SanPham.TenSp.ToLower().Contains(kw));
        }

        if (filterUserId.HasValue)
            query = query.Where(w => w.MaNguoiDung == filterUserId.Value);

        if (filterProductId.HasValue)
            query = query.Where(w => w.MaSp == filterProductId.Value);

        if (fromDate.HasValue)
            query = query.Where(w => w.NgayThem >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(w => w.NgayThem <= toDate.Value.AddDays(1));

        // Count
        var total = await query.CountAsync(cancellationToken);

        // Sort
        query = (sortBy?.ToLower(), sortDir?.ToLower()) switch
        {
            ("hoten", "asc")   => query.OrderBy(w => w.NguoiDung.HoTen),
            ("hoten", _)       => query.OrderByDescending(w => w.NguoiDung.HoTen),
            ("tensp", "asc")   => query.OrderBy(w => w.SanPham.TenSp),
            ("tensp", _)       => query.OrderByDescending(w => w.SanPham.TenSp),
            ("ngaythem", "asc")=> query.OrderBy(w => w.NgayThem),
            _                  => query.OrderByDescending(w => w.NgayThem),
        };

        // Paginate
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new AdminWishlistItemDto
            {
                WishlistId = w.WishlistId,
                MaNguoiDung = w.MaNguoiDung,
                HoTen = w.NguoiDung.HoTen,
                Email = w.NguoiDung.Email,
                MaSp = w.MaSp,
                TenSp = w.SanPham.TenSp,
                HinhAnh1 = w.SanPham.HinhAnh1,
                GiaTien = w.SanPham.GiaTien,
                TenDanhMuc = w.SanPham.MaDmNavigation != null ? w.SanPham.MaDmNavigation.TenDm : "",
                NgayThem = w.NgayThem
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<WishlistStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var tongLuot = await _context.SanPhamYeuThichs.CountAsync(cancellationToken);
        var tongSanPham = await _context.SanPhamYeuThichs
            .Select(w => w.MaSp)
            .Distinct()
            .CountAsync(cancellationToken);

        var top10 = await _context.SanPhamYeuThichs
            .AsNoTracking()
            .Include(w => w.SanPham)
            .GroupBy(w => new { w.MaSp, w.SanPham.TenSp, w.SanPham.HinhAnh1, w.SanPham.GiaTien })
            .Select(g => new TopWishlistProductDto
            {
                MaSp = g.Key.MaSp,
                TenSp = g.Key.TenSp,
                HinhAnh1 = g.Key.HinhAnh1,
                GiaTien = g.Key.GiaTien,
                SoLuotYeuThich = g.Count()
            })
            .OrderByDescending(x => x.SoLuotYeuThich)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new WishlistStatsDto
        {
            TongLuotYeuThich = tongLuot,
            TongSanPhamDuocYeuThich = tongSanPham,
            Top10SanPham = top10
        };
    }

    public async Task<List<(int Id, string HoTen)>> GetUserListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SanPhamYeuThichs
            .AsNoTracking()
            .Include(w => w.NguoiDung)
            .Select(w => new { w.MaNguoiDung, w.NguoiDung.HoTen })
            .Distinct()
            .OrderBy(x => x.HoTen)
            .Select(x => ValueTuple.Create(x.MaNguoiDung, x.HoTen))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(int Id, string TenSp)>> GetProductListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SanPhamYeuThichs
            .AsNoTracking()
            .Include(w => w.SanPham)
            .Select(w => new { w.MaSp, w.SanPham.TenSp })
            .Distinct()
            .OrderBy(x => x.TenSp)
            .Select(x => ValueTuple.Create(x.MaSp, x.TenSp))
            .ToListAsync(cancellationToken);
    }
}
