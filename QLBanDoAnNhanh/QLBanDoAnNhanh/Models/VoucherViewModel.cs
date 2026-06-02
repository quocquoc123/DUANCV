namespace QLBanDoAnNhanh.Models
{
    /// <summary>
    /// ViewModel để quản lý thông tin voucher trong giỏ hàng
    /// </summary>
    public class VoucherViewModel
    {
        public string MaKhuyenMai { get; set; }
        public int GiaTri { get; set; }
        public int DieuKienApDung { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public bool TrangThai { get; set; }
    }

    /// <summary>
    /// Response khi kiểm tra voucher
    /// </summary>
    public class VoucherCheckResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public VoucherViewModel Data { get; set; }
    }

    /// <summary>
    /// ViewModel cho GioHang khi áp dụng voucher
    /// </summary>
    public class GioHangVoucherViewModel
    {
        public double TongTien { get; set; }
        public double TienGiam { get; set; }
        public double TongSauGiam { get; set; }
        public string MaKhuyenMai { get; set; }
        public int GiaTri { get; set; }
    }
}
