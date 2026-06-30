using System;
using System.Collections.Generic;

namespace QLBanDoAnNhanh.Models;

public partial class ThanhToan
{
    public int MaThanhToan { get; set; }

    public string MaDh { get; set; }

    public string PhuongThucThanhToan { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public double TongTien { get; set; }

    public bool TrangThaiThanhToan { get; set; }

    public string PaymentMethod { get; set; }

    public string PaymentStatus { get; set; }

    public string TransactionId { get; set; }

    public DateTime? PaidAt { get; set; }

    public string QrCodeUrl { get; set; }

    public DateTime? PaymentExpiresAt { get; set; }

    public string MomoRequestId { get; set; }

    public string MomoPayUrl { get; set; }

    public virtual DonHang MaDhNavigation { get; set; }
}
