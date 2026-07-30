using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Repositories;

public class ProductDiscountRepository : IProductDiscountRepository
{
    private readonly QlbanDoAnNhanh3Context _context;

    public ProductDiscountRepository(QlbanDoAnNhanh3Context context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SanPham>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SanPhams
            .AsNoTracking()
            .Include(p => p.MaGiamGiaNavigation)
            .OrderBy(p => p.TenSp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SanPham>> GetActiveDiscountProductsAsync(int take, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;

        return await _context.SanPhams
            .AsNoTracking()
            .Include(p => p.MaGiamGiaNavigation)
            .Where(p =>
                p.TrangThai &&
                p.MaGiamGiaNavigation.GiaTri > 0 &&
                p.MaGiamGiaNavigation.ThoiGianBatDau <= now &&
                p.MaGiamGiaNavigation.ThoiGianKetThuc >= now)
            .OrderByDescending(p => p.MaGiamGiaNavigation.GiaTri)
            .ThenBy(p => p.TenSp)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<SanPham> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _context.SanPhams
            .Include(p => p.MaGiamGiaNavigation)
            .FirstOrDefaultAsync(p => p.MaSp == productId, cancellationToken);
    }

    public async Task<int> ExpireDiscountsAsync(DateTime currentDate, CancellationToken cancellationToken = default)
    {
        var defaultDiscount = await GetOrCreateDefaultDiscountAsync(cancellationToken);

        return await _context.SanPhams
            .Where(p =>
                p.MaGiamGiaNavigation.GiaTri > 0 &&
                p.MaGiamGiaNavigation.ThoiGianKetThuc < currentDate)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.MaGiamGia, defaultDiscount.MaGiamGia), cancellationToken);
    }

    public async Task<GiamGium> CreateDiscountAsync(decimal percent, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var discount = new GiamGium
        {
            GiaTri = Convert.ToInt32(Math.Round(percent, 0, MidpointRounding.AwayFromZero)),
            ThoiGianBatDau = startDate,
            ThoiGianKetThuc = endDate
        };

        await _context.GiamGia.AddAsync(discount, cancellationToken);
        return discount;
    }

    public async Task<GiamGium> GetOrCreateDefaultDiscountAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var defaultDiscount = await _context.GiamGia
            .OrderBy(g => g.MaGiamGia)
            .FirstOrDefaultAsync(g => g.GiaTri == 0, cancellationToken);

        if (defaultDiscount != null)
        {
            return defaultDiscount;
        }

        defaultDiscount = new GiamGium
        {
            GiaTri = 0,
            ThoiGianBatDau = now,
            ThoiGianKetThuc = now
        };

        await _context.GiamGia.AddAsync(defaultDiscount, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return defaultDiscount;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
