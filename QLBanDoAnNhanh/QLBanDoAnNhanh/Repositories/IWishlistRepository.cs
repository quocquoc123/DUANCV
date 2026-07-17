using QLBanDoAnNhanh.DTOs;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Repositories;

public interface IWishlistRepository
{
    // User operations
    Task<List<WishlistItemDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> IsWishlistedAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<SanPhamYeuThich> AddAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(int userId, int productId, CancellationToken cancellationToken = default);

    // Admin operations
    Task<(List<AdminWishlistItemDto> Items, int Total)> GetAllForAdminAsync(
        string searchKeyword,
        int? filterUserId,
        int? filterProductId,
        DateTime? fromDate,
        DateTime? toDate,
        string sortBy,
        string sortDir,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<WishlistStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<List<(int Id, string HoTen)>> GetUserListAsync(CancellationToken cancellationToken = default);
    Task<List<(int Id, string TenSp)>> GetProductListAsync(CancellationToken cancellationToken = default);
}
