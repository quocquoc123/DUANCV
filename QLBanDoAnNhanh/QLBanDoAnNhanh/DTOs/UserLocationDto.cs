namespace QLBanDoAnNhanh.DTOs
{
    /// <summary>
    /// DTO nhận tọa độ vị trí người dùng từ client (Geolocation API).
    /// Cả hai trường đều nullable để xử lý trường hợp người dùng từ chối cấp quyền định vị.
    /// </summary>
    public class UserLocationDto
    {
        /// <summary>Vĩ độ (latitude) của người dùng.</summary>
        public double? Latitude  { get; set; }

        /// <summary>Kinh độ (longitude) của người dùng.</summary>
        public double? Longitude { get; set; }
    }
}
