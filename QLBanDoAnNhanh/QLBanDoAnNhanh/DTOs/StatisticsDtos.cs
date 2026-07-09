namespace QLBanDoAnNhanh.DTOs;

public sealed class DateRangeRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public sealed class RevenueSummaryDto
{
    public double TotalRevenue { get; set; }
    /// <summary>Số đơn hoàn thành (có trạng thái thành công) – dùng để tính doanh thu.</summary>
    public int OrderCount { get; set; }
    /// <summary>Tổng số đơn hàng trong kỳ (mọi trạng thái).</summary>
    public int TotalOrderCount { get; set; }
    public double AverageOrderValue { get; set; }
    public IReadOnlyList<ChartPointDto> Series { get; set; } = Array.Empty<ChartPointDto>();
}

public sealed class ChartPointDto
{
    public string Label { get; set; }
    public DateTime? Date { get; set; }
    public double Value { get; set; }
    public int Count { get; set; }
}

public sealed class ProductStatisticsDto
{
    public IReadOnlyList<ProductStatisticItemDto> TopSellingProducts { get; set; } = Array.Empty<ProductStatisticItemDto>();
    public IReadOnlyList<ProductStatisticItemDto> TopRevenueProducts { get; set; } = Array.Empty<ProductStatisticItemDto>();
    public IReadOnlyList<ProductStatisticItemDto> SlowSellingProducts { get; set; } = Array.Empty<ProductStatisticItemDto>();
    public IReadOnlyList<ProductStatisticItemDto> RevenueByProduct { get; set; } = Array.Empty<ProductStatisticItemDto>();
    public IReadOnlyList<CategoryRevenueDto> RevenueByCategory { get; set; } = Array.Empty<CategoryRevenueDto>();
}

public sealed class ProductStatisticItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int QuantitySold { get; set; }
    public double Revenue { get; set; }
}

public sealed class CategoryRevenueDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public int QuantitySold { get; set; }
    public double Revenue { get; set; }
}

public sealed class CustomerStatisticsDto
{
    public IReadOnlyList<CustomerStatisticItemDto> TopSpendingCustomers { get; set; } = Array.Empty<CustomerStatisticItemDto>();
    public IReadOnlyList<CustomerStatisticItemDto> TopOrderCustomers { get; set; } = Array.Empty<CustomerStatisticItemDto>();
    public int NewCustomerCount { get; set; }
    public IReadOnlyList<CustomerStatisticItemDto> NewCustomers { get; set; } = Array.Empty<CustomerStatisticItemDto>();
    public int ReturningCustomerCount { get; set; }
    public IReadOnlyList<CustomerStatisticItemDto> ReturningCustomers { get; set; } = Array.Empty<CustomerStatisticItemDto>();
}

public sealed class CustomerStatisticItemDto
{
    public int? CustomerId { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public int OrderCount { get; set; }
    public double TotalSpent { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

public sealed class OrderStatisticsDto
{
    public IReadOnlyList<OrderStatusStatisticDto> OrdersByStatus { get; set; } = Array.Empty<OrderStatusStatisticDto>();
    public double SuccessRate { get; set; }
    public double CancelRate { get; set; }
    public IReadOnlyList<ChartPointDto> OrdersByTime { get; set; } = Array.Empty<ChartPointDto>();
}

public sealed class OrderStatusStatisticDto
{
    public string Status { get; set; }
    public int Count { get; set; }
    public double Rate { get; set; }
}

public sealed class PaymentStatisticsDto
{
    public IReadOnlyList<PaymentMethodStatisticDto> RevenueByPaymentMethod { get; set; } = Array.Empty<PaymentMethodStatisticDto>();
    public IReadOnlyList<PaymentMethodStatisticDto> PaymentMethodUsageRate { get; set; } = Array.Empty<PaymentMethodStatisticDto>();
}

public sealed class PaymentMethodStatisticDto
{
    public string PaymentMethod { get; set; }
    public int PaymentCount { get; set; }
    public double Revenue { get; set; }
    public double Rate { get; set; }
}

public sealed class PromotionStatisticsDto
{
    public IReadOnlyList<PromotionStatisticItemDto> Promotions { get; set; } = Array.Empty<PromotionStatisticItemDto>();
}

public sealed class PromotionStatisticItemDto
{
    public string PromotionCode { get; set; }
    public int DiscountPercent { get; set; }
    public int UsageCount { get; set; }
    public double GeneratedRevenue { get; set; }
    public double TotalDiscountAmount { get; set; }
}
