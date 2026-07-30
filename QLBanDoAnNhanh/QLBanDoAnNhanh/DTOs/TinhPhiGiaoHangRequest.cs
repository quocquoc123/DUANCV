namespace QLBanDoAnNhanh.DTOs
{
    /// <summary>
    /// Model nhận dữ liệu từ client để tính khoảng cách và phí giao hàng.
    /// </summary>
    public class TinhPhiGiaoHangRequest
    {
        /// <summary>Mã sản phẩm cần kiểm tra chi nhánh có hàng (tùy chọn).</summary>
        public int SanPhamId { get; set; }

        /// <summary>Địa chỉ đầy đủ của khách hàng (dùng nếu chưa có tọa độ).</summary>
        public string? DiaChiKhachHang { get; set; }

        /// <summary>Vĩ độ của khách hàng (tùy chọn nếu chọn từ Autocomplete / Geolocation).</summary>
        public double? LatKhachHang { get; set; }

        /// <summary>Kinh độ của khách hàng (tùy chọn nếu chọn từ Autocomplete / Geolocation).</summary>
        public double? LngKhachHang { get; set; }

        /// <summary>Mã chi nhánh chỉ định cần tính khoảng cách và phí (tùy chọn).</summary>
        public int MaChiNhanh { get; set; }

        /// <summary>Tiêu chí chọn chi nhánh gần nhất: "distance" (km - mặc định) hoặc "duration" (thời gian di chuyển).</summary>
        public string TieuChi { get; set; } = "distance";
    }
}
