using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.DTOs;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Repositories;

public sealed class StatisticsRepository : IStatisticsRepository
{
    // Trạng thái được coi là hoàn thành và tạo ra doanh thu thực tế.
    // Hệ thống chỉ dùng các trạng thái: "Chưa Giao", "Đang Giao", "Đã Giao", "Đã Hủy", "Chờ thanh toán".
    // Chỉ "Đã Giao" mới đồng nghĩa giao hàng thành công → được tính vào doanh thu.
    private static readonly string[] SuccessfulStatuses =
    {
        "Đã Giao"
    };

    // Trạng thái hủy đơn.
    private static readonly string[] CancelledStatuses =
    {
        "Đã Hủy"
    };

    private readonly QlbanDoAnNhanh3Context _context;

    public StatisticsRepository(QlbanDoAnNhanh3Context context)
    {
        _context = context;
    }

    public async Task<RevenueSummaryDto> GetRevenueAsync(string period, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        // Đếm tổng số đơn mọi trạng thái trong kỳ (dùng cho KPI "Tổng đơn hàng").
        var totalOrderCount = await GetOrders(fromDate, toDate).CountAsync(cancellationToken);

        // Chỉ tổng hợp doanh thu từ đơn thành công.
        var orders = GetSuccessfulOrders(fromDate, toDate);
        var summary = await orders
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalRevenue = g.Sum(x => x.TongTien),
                OrderCount = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var series = await GetRevenueSeriesAsync(orders, period, cancellationToken);
        var successOrderCount = summary?.OrderCount ?? 0;

        return new RevenueSummaryDto
        {
            TotalRevenue = summary?.TotalRevenue ?? 0,
            OrderCount = successOrderCount,
            TotalOrderCount = totalOrderCount,
            AverageOrderValue = successOrderCount == 0 ? 0 : (summary?.TotalRevenue ?? 0) / successOrderCount,
            Series = series
        };
    }

    public async Task<ProductStatisticsDto> GetProductStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        // Lấy tổng tiền NET (đã trừ KM) của từng đơn thành công để phân bổ tỷ lệ.
        var successfulOrderNetMap = await GetSuccessfulOrders(fromDate, toDate)
            .Select(x => new { x.MaDh, x.TongTien })
            .ToDictionaryAsync(x => x.MaDh, x => x.TongTien, cancellationToken);

        var orderIds = successfulOrderNetMap.Keys.ToList();

        // Lấy chi tiết đơn hàng (giá gốc từng item, chưa trừ KM).
        var orderDetails = await _context.ChiTietDonHangs
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.MaDh))
            .Select(x => new { x.MaDh, x.MaSp, x.SoLuong, GrossRevenue = x.TongTien })
            .ToListAsync(cancellationToken);

        // Phân bổ khuyến mãi theo tỷ lệ: mỗi item nhận phần doanh thu NET tương ứng.
        // NetRevenue_item = GrossRevenue_item × (DonHang.TongTien / Σ GrossRevenue đơn đó)
        var soldProducts = orderDetails
            .GroupBy(x => x.MaDh)
            .SelectMany(orderGroup =>
            {
                var netTotal = successfulOrderNetMap.TryGetValue(orderGroup.Key, out var net) ? net : 0.0;
                var grossTotal = orderGroup.Sum(x => x.GrossRevenue);
                var ratio = grossTotal == 0 ? 1.0 : netTotal / grossTotal;
                return orderGroup.Select(item => new
                {
                    item.MaSp,
                    item.SoLuong,
                    NetRevenue = item.GrossRevenue * ratio
                });
            })
            .GroupBy(x => x.MaSp)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantitySold = g.Sum(x => x.SoLuong),
                Revenue = g.Sum(x => x.NetRevenue)
            })
            .ToList();

        var soldProductMap = soldProducts.ToDictionary(x => x.ProductId);

        var products = await _context.SanPhams
            .AsNoTracking()
            .Select(x => new
            {
                x.MaSp,
                x.TenSp
            })
            .ToListAsync(cancellationToken);

        var productStatistics = products
            .Select(product =>
            {
                soldProductMap.TryGetValue(product.MaSp, out var sale);

                return new ProductStatisticItemDto
                {
                    ProductId = product.MaSp,
                    ProductName = product.TenSp,
                    QuantitySold = sale?.QuantitySold ?? 0,
                    Revenue = sale?.Revenue ?? 0
                };
            })
            .ToList();

        var revenueByProduct = productStatistics
            .Where(x => x.Revenue > 0 || x.QuantitySold > 0)
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.QuantitySold)
            .ThenBy(x => x.ProductName)
            .ToList();

        // Chỉ lấy sản phẩm đã bán ít nhất 1 lần để tránh liệt kê SP chưa bán bao giờ.
        var slowSellingProducts = productStatistics
            .Where(x => x.QuantitySold > 0)
            .OrderBy(x => x.QuantitySold)
            .ThenBy(x => x.Revenue)
            .ThenBy(x => x.ProductName)
            .Take(10)
            .ToList();

        // Doanh thu theo danh mục – phân bổ khuyến mãi theo cùng tỷ lệ.
        var categoryDetails = await _context.ChiTietDonHangs
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.MaDh))
            .Join(_context.SanPhams.AsNoTracking(),
                detail => detail.MaSp,
                product => product.MaSp,
                (detail, product) => new { detail, product })
            .Join(_context.DanhMucs.AsNoTracking(),
                item => item.product.MaDm,
                category => category.MaDm,
                (item, category) => new
                {
                    item.detail.MaDh,
                    item.detail.SoLuong,
                    GrossRevenue = item.detail.TongTien,
                    category.MaDm,
                    CategoryName = category.TenDm
                })
            .ToListAsync(cancellationToken);

        var revenueByCategory = categoryDetails
            .GroupBy(x => x.MaDh)
            .SelectMany(orderGroup =>
            {
                var netTotal = successfulOrderNetMap.TryGetValue(orderGroup.Key, out var net) ? net : 0.0;
                var grossTotal = orderGroup.Sum(x => x.GrossRevenue);
                var ratio = grossTotal == 0 ? 1.0 : netTotal / grossTotal;
                return orderGroup.Select(item => new
                {
                    item.MaDm,
                    item.CategoryName,
                    item.SoLuong,
                    NetRevenue = item.GrossRevenue * ratio
                });
            })
            .GroupBy(x => new { x.MaDm, x.CategoryName })
            .Select(g => new CategoryRevenueDto
            {
                CategoryId = g.Key.MaDm,
                CategoryName = g.Key.CategoryName,
                QuantitySold = g.Sum(x => x.SoLuong),
                Revenue = g.Sum(x => x.NetRevenue)
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return new ProductStatisticsDto
        {
            TopSellingProducts = revenueByProduct
                .OrderByDescending(x => x.QuantitySold)
                .ThenByDescending(x => x.Revenue)
                .Take(10)
                .ToList(),
            TopRevenueProducts = revenueByProduct.Take(10).ToList(),
            SlowSellingProducts = slowSellingProducts,
            RevenueByProduct = revenueByProduct,
            RevenueByCategory = revenueByCategory
        };
    }

    public async Task<CustomerStatisticsDto> GetCustomerStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var customerOrders = await GetSuccessfulOrders(fromDate, toDate)
            .GroupBy(x => new { x.MaNguoiDung, x.Username })
            .Select(g => new CustomerStatisticItemDto
            {
                CustomerId = g.Key.MaNguoiDung,
                Username = g.Key.Username,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(x => x.TongTien),
                FirstOrderDate = g.Min(x => x.CreatedAt),
                LastOrderDate = g.Max(x => x.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        var customerIds = customerOrders
            .Where(x => x.CustomerId.HasValue)
            .Select(x => x.CustomerId.Value)
            .ToArray();

        var customers = await _context.NguoiDungs
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.MaNguoiDung))
            .Select(x => new { x.MaNguoiDung, x.HoTen, x.Email })
            .ToDictionaryAsync(x => x.MaNguoiDung, cancellationToken);

        foreach (var item in customerOrders)
        {
            if (item.CustomerId.HasValue && customers.TryGetValue(item.CustomerId.Value, out var customer))
            {
                item.FullName = customer.HoTen;
                item.Email = customer.Email;
            }
        }

        var allCustomerFirstOrders = await GetSuccessfulOrders(null, null)
            .GroupBy(x => new { x.MaNguoiDung, x.Username })
            .Select(g => new CustomerStatisticItemDto
            {
                CustomerId = g.Key.MaNguoiDung,
                Username = g.Key.Username,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(x => x.TongTien),
                FirstOrderDate = g.Min(x => x.CreatedAt),
                LastOrderDate = g.Max(x => x.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        var (start, endExclusive) = NormalizeRange(fromDate, toDate);
        var newCustomers = allCustomerFirstOrders
            .Where(x => IsInsideRange(x.FirstOrderDate, start, endExclusive))
            .OrderByDescending(x => x.FirstOrderDate)
            .Take(50)
            .ToList();

        foreach (var item in newCustomers)
        {
            if (item.CustomerId.HasValue && customers.TryGetValue(item.CustomerId.Value, out var customer))
            {
                item.FullName = customer.HoTen;
                item.Email = customer.Email;
            }
        }

        var returningCustomers = customerOrders
            .Where(x => x.OrderCount >= 2)
            .OrderByDescending(x => x.OrderCount)
            .ThenByDescending(x => x.TotalSpent)
            .Take(50)
            .ToList();

        return new CustomerStatisticsDto
        {
            TopSpendingCustomers = customerOrders
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToList(),
            TopOrderCustomers = customerOrders
                .OrderByDescending(x => x.OrderCount)
                .ThenByDescending(x => x.TotalSpent)
                .Take(10)
                .ToList(),
            NewCustomerCount = allCustomerFirstOrders.Count(x => IsInsideRange(x.FirstOrderDate, start, endExclusive)),
            NewCustomers = newCustomers,
            ReturningCustomerCount = customerOrders.Count(x => x.OrderCount >= 2),
            ReturningCustomers = returningCustomers
        };
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(string groupBy, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var orders = GetOrders(fromDate, toDate);
        var totalOrders = await orders.CountAsync(cancellationToken);

        var ordersByStatus = await orders
            .GroupBy(x => x.TrangThai)
            .Select(g => new OrderStatusStatisticDto
            {
                Status = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        foreach (var item in ordersByStatus)
        {
            item.Rate = totalOrders == 0 ? 0 : item.Count * 100.0 / totalOrders;
        }

        var successOrders = await orders.CountAsync(x => SuccessfulStatuses.Contains(x.TrangThai), cancellationToken);
        var cancelledOrders = await orders.CountAsync(x => CancelledStatuses.Contains(x.TrangThai), cancellationToken);

        return new OrderStatisticsDto
        {
            OrdersByStatus = ordersByStatus,
            SuccessRate = totalOrders == 0 ? 0 : successOrders * 100.0 / totalOrders,
            CancelRate = totalOrders == 0 ? 0 : cancelledOrders * 100.0 / totalOrders,
            OrdersByTime = await GetOrderSeriesAsync(orders, groupBy, cancellationToken)
        };
    }

    public async Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        // Lấy MaDH của các đơn thành công để đảm bảo thống kê phương thức thanh toán
        // khớp với doanh thu (cùng bộ đơn hàng, tránh đếm đơn chưa thanh toán xong).
        var successfulOrderIds = GetSuccessfulOrders(fromDate, toDate).Select(x => x.MaDh);

        var payments = _context.ThanhToans
            .AsNoTracking()
            .Where(x => successfulOrderIds.Contains(x.MaDh));

        var paymentMethods = await payments
            .GroupBy(x => string.IsNullOrEmpty(x.PaymentMethod) ? x.PhuongThucThanhToan : x.PaymentMethod)
            .Select(g => new PaymentMethodStatisticDto
            {
                PaymentMethod = g.Key,
                PaymentCount = g.Count(),
                Revenue = g.Sum(x => x.TongTien)
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(cancellationToken);

        var totalPayments = paymentMethods.Sum(x => x.PaymentCount);
        foreach (var item in paymentMethods)
        {
            item.Rate = totalPayments == 0 ? 0 : item.PaymentCount * 100.0 / totalPayments;
        }

        return new PaymentStatisticsDto
        {
            RevenueByPaymentMethod = paymentMethods,
            PaymentMethodUsageRate = paymentMethods
        };
    }

    public async Task<PromotionStatisticsDto> GetPromotionStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var promotionOrders = await GetSuccessfulOrders(fromDate, toDate)
            .Where(x => !string.IsNullOrEmpty(x.MaKhuyenMai))
            .Join(_context.KhuyenMais.AsNoTracking(),
                order => order.MaKhuyenMai,
                promotion => promotion.MaKhuyenMai,
                (order, promotion) => new
                {
                    PromotionCode = promotion.MaKhuyenMai,
                    DiscountPercent = promotion.GiaTri,
                    Revenue = order.TongTien
                })
            .ToListAsync(cancellationToken);

        var promotions = promotionOrders
            .GroupBy(x => new { x.PromotionCode, x.DiscountPercent })
            .Select(g => new PromotionStatisticItemDto
            {
                PromotionCode = g.Key.PromotionCode,
                DiscountPercent = g.Key.DiscountPercent,
                UsageCount = g.Count(),
                GeneratedRevenue = g.Sum(x => x.Revenue),
                TotalDiscountAmount = g.Sum(x => CalculateDiscountFromNetRevenue(x.Revenue, x.DiscountPercent))
            })
            .OrderByDescending(x => x.UsageCount)
            .ThenByDescending(x => x.GeneratedRevenue)
            .ToList();

        return new PromotionStatisticsDto
        {
            Promotions = promotions
        };
    }

    private IQueryable<DonHang> GetSuccessfulOrders(DateTime? fromDate, DateTime? toDate)
    {
        return GetOrders(fromDate, toDate).Where(x => SuccessfulStatuses.Contains(x.TrangThai));
    }

    private IQueryable<DonHang> GetOrders(DateTime? fromDate, DateTime? toDate)
    {
        var (start, endExclusive) = NormalizeRange(fromDate, toDate);
        var query = _context.DonHangs
            .AsNoTracking()
            .Where(x => x.CreatedAt.HasValue);

        if (start.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= start.Value);
        }

        if (endExclusive.HasValue)
        {
            query = query.Where(x => x.CreatedAt < endExclusive.Value);
        }

        return query;
    }

    private IQueryable<ThanhToan> GetPayments(DateTime? fromDate, DateTime? toDate)
    {
        var (start, endExclusive) = NormalizeRange(fromDate, toDate);
        var query = _context.ThanhToans
            .AsNoTracking()
            .Where(x => x.NgayThanhToan.HasValue || x.PaidAt.HasValue);

        if (start.HasValue)
        {
            query = query.Where(x => (x.PaidAt ?? x.NgayThanhToan) >= start.Value);
        }

        if (endExclusive.HasValue)
        {
            query = query.Where(x => (x.PaidAt ?? x.NgayThanhToan) < endExclusive.Value);
        }

        return query;
    }

    private async Task<IReadOnlyList<ChartPointDto>> GetRevenueSeriesAsync(IQueryable<DonHang> orders, string period, CancellationToken cancellationToken)
    {
        period = NormalizePeriod(period);
        if (period == "week")
        {
            var anchor = new DateTime(2000, 1, 3);
            var rows = await orders
                .GroupBy(x => EF.Functions.DateDiffWeek(anchor, x.CreatedAt.Value))
                .Select(g => new { WeekIndex = g.Key, Revenue = g.Sum(x => x.TongTien), Count = g.Count() })
                .OrderBy(x => x.WeekIndex)
                .ToListAsync(cancellationToken);

            return rows.Select(x =>
            {
                var weekStart = anchor.AddDays(x.WeekIndex * 7);
                return new ChartPointDto
                {
                    Label = $"{weekStart:yyyy-MM-dd}",
                    Date = weekStart,
                    Value = x.Revenue,
                    Count = x.Count
                };
            }).ToList();
        }

        if (period == "month")
        {
            var rows = await orders
                .GroupBy(x => new { x.CreatedAt.Value.Year, x.CreatedAt.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(x => x.TongTien), Count = g.Count() })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync(cancellationToken);

            return rows.Select(x => new ChartPointDto
            {
                Label = $"{x.Year:D4}-{x.Month:D2}",
                Date = new DateTime(x.Year, x.Month, 1),
                Value = x.Revenue,
                Count = x.Count
            }).ToList();
        }

        if (period == "quarter")
        {
            var rows = await orders
                .GroupBy(x => new { x.CreatedAt.Value.Year, Quarter = ((x.CreatedAt.Value.Month - 1) / 3) + 1 })
                .Select(g => new { g.Key.Year, g.Key.Quarter, Revenue = g.Sum(x => x.TongTien), Count = g.Count() })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Quarter)
                .ToListAsync(cancellationToken);

            return rows.Select(x => new ChartPointDto
            {
                Label = $"{x.Year:D4}-Q{x.Quarter}",
                Date = new DateTime(x.Year, ((x.Quarter - 1) * 3) + 1, 1),
                Value = x.Revenue,
                Count = x.Count
            }).ToList();
        }

        if (period == "year")
        {
            var rows = await orders
                .GroupBy(x => x.CreatedAt.Value.Year)
                .Select(g => new { Year = g.Key, Revenue = g.Sum(x => x.TongTien), Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToListAsync(cancellationToken);

            return rows.Select(x => new ChartPointDto
            {
                Label = x.Year.ToString(),
                Date = new DateTime(x.Year, 1, 1),
                Value = x.Revenue,
                Count = x.Count
            }).ToList();
        }

        var dayRows = await orders
            .GroupBy(x => x.CreatedAt.Value.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.TongTien), Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return dayRows.Select(x => new ChartPointDto
        {
            Label = x.Date.ToString("yyyy-MM-dd"),
            Date = x.Date,
            Value = x.Revenue,
            Count = x.Count
        }).ToList();
    }

    private async Task<IReadOnlyList<ChartPointDto>> GetOrderSeriesAsync(IQueryable<DonHang> orders, string groupBy, CancellationToken cancellationToken)
    {
        var revenueSeries = await GetRevenueSeriesAsync(orders, groupBy, cancellationToken);
        return revenueSeries.Select(x => new ChartPointDto
        {
            Label = x.Label,
            Date = x.Date,
            Value = x.Count,
            Count = x.Count
        }).ToList();
    }

    private static string NormalizePeriod(string period)
    {
        return string.IsNullOrWhiteSpace(period)
            ? "day"
            : period.Trim().ToLowerInvariant() switch
            {
                "daily" => "day",
                "ngay" => "day",
                "weekly" => "week",
                "tuan" => "week",
                "monthly" => "month",
                "thang" => "month",
                "quarterly" => "quarter",
                "quy" => "quarter",
                "yearly" => "year",
                "nam" => "year",
                "custom" => "day",
                var value => value
            };
    }

    private static (DateTime? Start, DateTime? EndExclusive) NormalizeRange(DateTime? fromDate, DateTime? toDate)
    {
        var start = fromDate?.Date;
        var endExclusive = toDate?.Date.AddDays(1);

        if (start.HasValue && endExclusive.HasValue && start.Value > endExclusive.Value)
        {
            (start, endExclusive) = (endExclusive.Value.AddDays(-1), start.Value.AddDays(1));
        }

        return (start, endExclusive);
    }

    private static bool IsInsideRange(DateTime? value, DateTime? start, DateTime? endExclusive)
    {
        if (!value.HasValue)
        {
            return false;
        }

        if (start.HasValue && value.Value < start.Value)
        {
            return false;
        }

        return !endExclusive.HasValue || value.Value < endExclusive.Value;
    }

    // Tính số tiền đã giảm từ doanh thu net (giá sau khi trừ khuyến mãi).
    // Giả định: TongTien trong DonHang là giá NET (đã trừ khuyến mãi) — đúng với logic ThanhToanController.
    // Công thức: discount = net * rate / (1 - rate)
    // Ví dụ: net=90.000đ, giảm 10% → discount = 90.000 * 10 / 90 = 10.000đ ✓
    private static double CalculateDiscountFromNetRevenue(double netRevenue, int discountPercent)
    {
        if (discountPercent <= 0 || discountPercent >= 100)
        {
            return 0;
        }

        return netRevenue * discountPercent / (100 - discountPercent);
    }
}
