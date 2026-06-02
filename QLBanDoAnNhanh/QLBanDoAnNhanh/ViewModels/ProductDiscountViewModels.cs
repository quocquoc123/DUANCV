using System.ComponentModel.DataAnnotations;

namespace QLBanDoAnNhanh.ViewModels;

public enum ProductDiscountStatus
{
    DangGiamGia,
    ChuaApDung,
    HetHan
}

public class ProductDiscountListItemViewModel
{
    public int MaSp { get; set; }
    public string TenSp { get; set; }
    public string HinhAnh { get; set; }
    public decimal GiaGoc { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal? DiscountPrice { get; set; }
    public DateTime? DiscountStartDate { get; set; }
    public DateTime? DiscountEndDate { get; set; }
    public ProductDiscountStatus Status { get; set; }

    public bool IsActive => Status == ProductDiscountStatus.DangGiamGia;
    public decimal GiaSauGiam => IsActive && DiscountPrice.HasValue ? DiscountPrice.Value : GiaGoc;
    public decimal AmountSaved => Math.Max(0, GiaGoc - GiaSauGiam);

    public string StatusText => Status switch
    {
        ProductDiscountStatus.DangGiamGia => "Đang giảm giá",
        ProductDiscountStatus.HetHan => "Hết hạn",
        _ => "Chưa áp dụng"
    };

    public string StatusCssClass => Status switch
    {
        ProductDiscountStatus.DangGiamGia => "badge bg-success",
        ProductDiscountStatus.HetHan => "badge bg-secondary",
        _ => "badge bg-warning text-dark"
    };
}

public class ProductDiscountIndexViewModel
{
    public IReadOnlyList<ProductDiscountListItemViewModel> Products { get; set; } = Array.Empty<ProductDiscountListItemViewModel>();
    public ProductDiscountDashboardViewModel Dashboard { get; set; } = new();
    public ProductDiscountFormViewModel Form { get; set; } = new();
}

public class ProductDiscountDashboardViewModel
{
    public int TotalDiscountProducts { get; set; }
    public string HighestDiscountProductName { get; set; } = "Chưa có";
    public decimal HighestDiscountPercent { get; set; }
    public decimal TotalDiscountAmount { get; set; }
}

public class ProductDiscountFormViewModel : IValidatableObject
{
    [Required]
    public int MaSp { get; set; }

    public string TenSp { get; set; }

    public decimal GiaGoc { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập phần trăm giảm giá.")]
    [Range(1, 100, ErrorMessage = "Phần trăm giảm giá phải từ 1 đến 100.")]
    public decimal DiscountPercent { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    public DateTime? DiscountStartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    public DateTime? DiscountEndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountStartDate.HasValue &&
            DiscountEndDate.HasValue &&
            DiscountEndDate.Value <= DiscountStartDate.Value)
        {
            yield return new ValidationResult(
                "Ngày kết thúc phải lớn hơn ngày bắt đầu.",
                new[] { nameof(DiscountEndDate) });
        }
    }
}
