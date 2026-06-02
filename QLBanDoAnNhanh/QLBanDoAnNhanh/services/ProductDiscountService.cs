using QLBanDoAnNhanh.Models;
using QLBanDoAnNhanh.Repositories;
using QLBanDoAnNhanh.ViewModels;

namespace QLBanDoAnNhanh.Services;

public interface IProductDiscountService
{
    Task<ProductDiscountIndexViewModel> GetManagementPageAsync(CancellationToken cancellationToken = default);
    Task ApplyDiscountAsync(ProductDiscountFormViewModel model, CancellationToken cancellationToken = default);
    Task CancelDiscountAsync(int productId, CancellationToken cancellationToken = default);
    Task ExpireOutdatedDiscountsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SanPham>> GetFeaturedDiscountProductsAsync(int take = 8, CancellationToken cancellationToken = default);
    bool IsDiscountActive(SanPham product, DateTime? currentDate = null);
    decimal GetEffectivePrice(SanPham product, DateTime? currentDate = null);
}

public class ProductDiscountService : IProductDiscountService
{
    private readonly IProductDiscountRepository _repository;

    public ProductDiscountService(IProductDiscountRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductDiscountIndexViewModel> GetManagementPageAsync(CancellationToken cancellationToken = default)
    {
        await ExpireOutdatedDiscountsAsync(cancellationToken);

        var products = await _repository.GetAllProductsAsync(cancellationToken);
        var items = products.Select(ToListItem).ToList();

        return new ProductDiscountIndexViewModel
        {
            Products = items,
            Dashboard = BuildDashboard(items),
            Form = new ProductDiscountFormViewModel
            {
                DiscountStartDate = DateTime.Now,
                DiscountEndDate = DateTime.Now.AddDays(7)
            }
        };
    }

    public async Task ApplyDiscountAsync(ProductDiscountFormViewModel model, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(model.MaSp, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sản phẩm cần áp dụng giảm giá.");

        var discount = await _repository.CreateDiscountAsync(
            model.DiscountPercent,
            model.DiscountStartDate!.Value,
            model.DiscountEndDate!.Value,
            cancellationToken);

        product.MaGiamGiaNavigation = discount;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelDiscountAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(productId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sản phẩm cần hủy giảm giá.");

        var defaultDiscount = await _repository.GetOrCreateDefaultDiscountAsync(cancellationToken);
        product.MaGiamGia = defaultDiscount.MaGiamGia;
        product.MaGiamGiaNavigation = defaultDiscount;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public Task ExpireOutdatedDiscountsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.ExpireDiscountsAsync(DateTime.Now, cancellationToken);
    }

    public async Task<IReadOnlyList<SanPham>> GetFeaturedDiscountProductsAsync(int take = 8, CancellationToken cancellationToken = default)
    {
        await ExpireOutdatedDiscountsAsync(cancellationToken);
        return await _repository.GetActiveDiscountProductsAsync(take, cancellationToken);
    }

    public bool IsDiscountActive(SanPham product, DateTime? currentDate = null)
    {
        var now = currentDate ?? DateTime.Now;

        var discount = product.MaGiamGiaNavigation;

        return discount != null &&
               discount.GiaTri > 0 &&
               discount.ThoiGianBatDau <= now &&
               discount.ThoiGianKetThuc >= now;
    }

    public decimal GetEffectivePrice(SanPham product, DateTime? currentDate = null)
    {
        return IsDiscountActive(product, currentDate)
            ? CalculateDiscountPrice(Convert.ToDecimal(product.GiaTien), GetDiscountPercent(product))
            : Convert.ToDecimal(product.GiaTien);
    }

    private static decimal CalculateDiscountPrice(decimal originalPrice, decimal discountPercent)
    {
        return Math.Round(originalPrice - originalPrice * discountPercent / 100, 0, MidpointRounding.AwayFromZero);
    }

    private ProductDiscountListItemViewModel ToListItem(SanPham product)
    {
        var status = GetStatus(product);
        var originalPrice = Convert.ToDecimal(product.GiaTien);
        var discountPercent = GetDiscountPercent(product);

        return new ProductDiscountListItemViewModel
        {
            MaSp = product.MaSp,
            TenSp = product.TenSp,
            HinhAnh = product.HinhAnh1,
            GiaGoc = originalPrice,
            DiscountPercent = discountPercent,
            DiscountPrice = discountPercent > 0 ? CalculateDiscountPrice(originalPrice, discountPercent) : null,
            DiscountStartDate = product.MaGiamGiaNavigation?.ThoiGianBatDau,
            DiscountEndDate = product.MaGiamGiaNavigation?.ThoiGianKetThuc,
            Status = status
        };
    }

    private ProductDiscountStatus GetStatus(SanPham product)
    {
        if (IsDiscountActive(product))
        {
            return ProductDiscountStatus.DangGiamGia;
        }

        var discount = product.MaGiamGiaNavigation;
        if (discount != null &&
            discount.ThoiGianKetThuc < DateTime.Now &&
            discount.GiaTri > 0)
        {
            return ProductDiscountStatus.HetHan;
        }

        return ProductDiscountStatus.ChuaApDung;
    }

    private static ProductDiscountDashboardViewModel BuildDashboard(IReadOnlyList<ProductDiscountListItemViewModel> items)
    {
        var activeItems = items.Where(i => i.IsActive).ToList();
        var highest = activeItems.OrderByDescending(i => i.DiscountPercent).FirstOrDefault();

        return new ProductDiscountDashboardViewModel
        {
            TotalDiscountProducts = activeItems.Count,
            HighestDiscountProductName = highest?.TenSp ?? "Chưa có",
            HighestDiscountPercent = highest?.DiscountPercent ?? 0,
            TotalDiscountAmount = activeItems.Sum(i => i.AmountSaved)
        };
    }

    private static decimal GetDiscountPercent(SanPham product)
    {
        return product.MaGiamGiaNavigation?.GiaTri ?? 0;
    }
}
