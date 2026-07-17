using QLBanDoAnNhanh.DTOs;
using QLBanDoAnNhanh.Repositories;
using QLBanDoAnNhanh.ViewModels;

namespace QLBanDoAnNhanh.Services;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _repository;

    public WishlistService(IWishlistRepository repository)
    {
        _repository = repository;
    }

    public async Task<WishlistPageViewModel> GetUserWishlistAsync(int userId, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetByUserIdAsync(userId, cancellationToken);
        return new WishlistPageViewModel
        {
            Items = items,
            TotalCount = items.Count
        };
    }

    public async Task<int> GetWishlistCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetCountByUserIdAsync(userId, cancellationToken);
    }

    public async Task<WishlistToggleResultDto> ToggleWishlistAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var isWishlisted = await _repository.IsWishlistedAsync(userId, productId, cancellationToken);

            if (isWishlisted)
            {
                await _repository.RemoveAsync(userId, productId, cancellationToken);
                var count = await _repository.GetCountByUserIdAsync(userId, cancellationToken);
                return new WishlistToggleResultDto
                {
                    Success = true,
                    IsWishlisted = false,
                    Count = count,
                    Message = "Đã xóa khỏi danh sách yêu thích"
                };
            }
            else
            {
                await _repository.AddAsync(userId, productId, cancellationToken);
                var count = await _repository.GetCountByUserIdAsync(userId, cancellationToken);
                return new WishlistToggleResultDto
                {
                    Success = true,
                    IsWishlisted = true,
                    Count = count,
                    Message = "Đã thêm vào danh sách yêu thích ❤️"
                };
            }
        }
        catch (Exception ex)
        {
            return new WishlistToggleResultDto
            {
                Success = false,
                IsWishlisted = false,
                Count = 0,
                Message = "Có lỗi xảy ra: " + ex.Message
            };
        }
    }

    public async Task<bool> RemoveFromWishlistAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        return await _repository.RemoveAsync(userId, productId, cancellationToken);
    }

    public async Task<bool> IsWishlistedAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        return await _repository.IsWishlistedAsync(userId, productId, cancellationToken);
    }

    public async Task<AdminWishlistViewModel> GetAdminWishlistAsync(
        string searchKeyword = null,
        int? filterUserId = null,
        int? filterProductId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortBy = "NgayThem",
        string sortDir = "desc",
        int page = 1,
        int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _repository.GetAllForAdminAsync(
            searchKeyword, filterUserId, filterProductId,
            fromDate, toDate, sortBy, sortDir, page, pageSize, cancellationToken);

        var userList = await _repository.GetUserListAsync(cancellationToken);
        var productList = await _repository.GetProductListAsync(cancellationToken);
        var stats = await _repository.GetStatsAsync(cancellationToken);

        return new AdminWishlistViewModel
        {
            Items = items,
            TotalCount = total,
            SearchKeyword = searchKeyword,
            FilterUserId = filterUserId,
            FilterProductId = filterProductId,
            FilterFromDate = fromDate,
            FilterToDate = toDate,
            SortBy = sortBy ?? "NgayThem",
            SortDir = sortDir ?? "desc",
            CurrentPage = page,
            PageSize = pageSize,
            UserList = userList,
            ProductList = productList,
            Stats = stats
        };
    }

    public async Task<WishlistStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetStatsAsync(cancellationToken);
    }
}
