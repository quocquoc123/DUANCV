namespace QLBanDoAnNhanh.Models
{
    /// <summary>
    /// ViewModel cho chi nhánh cửa hàng.
    /// Dữ liệu hardcode trong Controller, không cần thay đổi database.
    /// </summary>
    public class ChiNhanhViewModel
    {
        public int Id { get; set; }

        /// <summary>Tên chi nhánh</summary>
        public string TenChiNhanh { get; set; } = string.Empty;

        /// <summary>Địa chỉ đầy đủ</summary>
        public string DiaChi { get; set; } = string.Empty;

        /// <summary>Số điện thoại liên hệ</summary>
        public string SoDienThoai { get; set; } = string.Empty;

        /// <summary>Giờ mở cửa (HH:mm)</summary>
        public string GioMoCua { get; set; } = "07:00";

        /// <summary>Giờ đóng cửa (HH:mm)</summary>
        public string GioDongCua { get; set; } = "22:00";

        /// <summary>Vĩ độ (latitude) để hiển thị bản đồ</summary>
        public double Latitude { get; set; }

        /// <summary>Kinh độ (longitude) để hiển thị bản đồ</summary>
        public double Longitude { get; set; }

        /// <summary>URL ảnh đại diện chi nhánh</summary>
        public string HinhAnh { get; set; } = string.Empty;

        /// <summary>Quận/Huyện (hiển thị phụ)</summary>
        public string Quan { get; set; } = string.Empty;

        /// <summary>
        /// Tính toán trạng thái đang mở cửa dựa vào giờ hiện tại.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                var now = TimeOnly.FromDateTime(DateTime.Now);
                var open = TimeOnly.Parse(GioMoCua);
                var close = TimeOnly.Parse(GioDongCua);
                return now >= open && now <= close;
            }
        }
    }
}
