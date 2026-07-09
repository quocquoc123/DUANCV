using QLBanDoAnNhanh.DTOs;

namespace QLBanDoAnNhanh.Repositories;

public interface IStatisticsRepository
{
    Task<RevenueSummaryDto> GetRevenueAsync(string period, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    Task<ProductStatisticsDto> GetProductStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    Task<CustomerStatisticsDto> GetCustomerStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    Task<OrderStatisticsDto> GetOrderStatisticsAsync(string groupBy, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    Task<PromotionStatisticsDto> GetPromotionStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
}
