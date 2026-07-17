using QLBanDoAnNhanh.DTOs;

namespace QLBanDoAnNhanh.ViewModels;

public class WishlistPageViewModel
{
    public List<WishlistItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class AdminWishlistViewModel
{
    public List<AdminWishlistItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }

    // Filters
    public string SearchKeyword { get; set; }
    public int? FilterUserId { get; set; }
    public int? FilterProductId { get; set; }
    public DateTime? FilterFromDate { get; set; }
    public DateTime? FilterToDate { get; set; }

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Sort
    public string SortBy { get; set; } = "NgayThem";
    public string SortDir { get; set; } = "desc";

    // Dropdown data for filters
    public List<(int Id, string HoTen)> UserList { get; set; } = new();
    public List<(int Id, string TenSp)> ProductList { get; set; } = new();

    // Stats
    public WishlistStatsDto Stats { get; set; }
}
