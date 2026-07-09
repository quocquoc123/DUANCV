using QLBanDoAnNhanh.DTOs;
using QLBanDoAnNhanh.Repositories;

namespace QLBanDoAnNhanh.Services;

public sealed class StatisticsService : IStatisticsService
{
    private readonly IStatisticsRepository _statisticsRepository;

    public StatisticsService(IStatisticsRepository statisticsRepository)
    {
        _statisticsRepository = statisticsRepository;
    }

    public Task<RevenueSummaryDto> GetRevenueAsync(string period, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        return _statisticsRepository.GetRevenueAsync(period, fromDate, toDate, cancellationToken);
    }

    public Task<ProductStatisticsDto> GetProductStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        return _statisticsRepository.GetProductStatisticsAsync(fromDate, toDate, cancellationToken);
    }

    public Task<CustomerStatisticsDto> GetCustomerStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        return _statisticsRepository.GetCustomerStatisticsAsync(fromDate, toDate, cancellationToken);
    }

    public Task<OrderStatisticsDto> GetOrderStatisticsAsync(string groupBy, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        return _statisticsRepository.GetOrderStatisticsAsync(groupBy, fromDate, toDate, cancellationToken);
    }

    public Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        return _statisticsRepository.GetPaymentStatisticsAsync(fromDate, toDate, cancellationToken);
    }

    public Task<PromotionStatisticsDto> GetPromotionStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        return _statisticsRepository.GetPromotionStatisticsAsync(fromDate, toDate, cancellationToken);
    }
}
