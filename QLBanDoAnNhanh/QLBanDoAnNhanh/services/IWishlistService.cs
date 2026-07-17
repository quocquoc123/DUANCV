using QLBanDoAnNhanh.DTOs;
using QLBanDoAnNhanh.ViewModels;

namespace QLBanDoAnNhanh.Services;

public interface IWishlistService
{
    Task<WishlistPageViewModel> GetUserWishlistAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetWishlistCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<WishlistToggleResultDto> ToggleWishlistAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromWishlistAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<bool> IsWishlistedAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<AdminWishlistViewModel> GetAdminWishlistAsync(
        string searchKeyword = null,
        int? filterUserId = null,
        int? filterProductId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortBy = "NgayThem",
        string sortDir = "desc",
        int page = 1,
        int pageSize = 15,
        CancellationToken cancellationToken = default);
    Task<WishlistStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
}
