using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Repositories;

public interface IProductDiscountRepository
{
    Task<IReadOnlyList<SanPham>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SanPham>> GetActiveDiscountProductsAsync(int take, CancellationToken cancellationToken = default);
    Task<SanPham> GetByIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<int> ExpireDiscountsAsync(DateTime currentDate, CancellationToken cancellationToken = default);
    Task<GiamGium> CreateDiscountAsync(decimal percent, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<GiamGium> GetOrCreateDefaultDiscountAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
