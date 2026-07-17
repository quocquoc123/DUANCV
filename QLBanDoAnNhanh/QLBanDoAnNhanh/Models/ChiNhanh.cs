using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLBanDoAnNhanh.Models
{
    /// <summary>
    /// Entity ánh xạ bảng ChiNhanh trong database.
    /// </summary>
    [Table("ChiNhanh")]
    public class ChiNhanh
    {
        [Key]
        public int MaChiNhanh { get; set; }

        /// <summary>Tên chi nhánh</summary>
        [Required(ErrorMessage = "Tên chi nhánh không được để trống.")]
        [MaxLength(200)]
        [Display(Name = "Tên chi nhánh")]
        public string TenChiNhanh { get; set; } = string.Empty;

        /// <summary>Địa chỉ đầy đủ</summary>
        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [MaxLength(500)]
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; } = string.Empty;

        /// <summary>Số điện thoại liên hệ</summary>
        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [MaxLength(20)]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        /// <summary>Email chi nhánh</summary>
        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        /// <summary>Giờ mở cửa (HH:mm)</summary>
        [Required(ErrorMessage = "Giờ mở cửa không được để trống.")]
        [MaxLength(10)]
        [Display(Name = "Giờ mở cửa")]
        public string GioMoCua { get; set; } = "07:00";

        /// <summary>Giờ đóng cửa (HH:mm)</summary>
        [Required(ErrorMessage = "Giờ đóng cửa không được để trống.")]
        [MaxLength(10)]
        [Display(Name = "Giờ đóng cửa")]
        public string GioDongCua { get; set; } = "22:00";

        /// <summary>Vĩ độ (latitude) để hiển thị bản đồ</summary>
        [Display(Name = "Vĩ độ (Latitude)")]
        public double? Latitude { get; set; }

        /// <summary>Kinh độ (longitude) để hiển thị bản đồ</summary>
        [Display(Name = "Kinh độ (Longitude)")]
        public double? Longitude { get; set; }

        /// <summary>URL hình ảnh (lưu trên Cloudinary)</summary>
        [MaxLength(500)]
        [Display(Name = "Hình ảnh")]
        public string? HinhAnh { get; set; }

        /// <summary>Trạng thái hoạt động (true = đang hoạt động)</summary>
        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;

        // ----------------------------------------------------------------
        // Computed properties (không lưu DB)
        // ----------------------------------------------------------------

        /// <summary>
        /// Kiểm tra chi nhánh có đang trong giờ mở cửa không.
        /// </summary>
        [NotMapped]
        public bool IsOpen
        {
            get
            {
                if (!TrangThai) return false;
                try
                {
                    var now   = TimeOnly.FromDateTime(DateTime.Now);
                    var open  = TimeOnly.Parse(GioMoCua);
                    var close = TimeOnly.Parse(GioDongCua);
                    return now >= open && now <= close;
                }
                catch { return false; }
            }
        }

        /// <summary>Chuỗi giờ hoạt động dạng "07:00 – 22:00"</summary>
        [NotMapped]
        public string GioHoatDong => $"{GioMoCua} – {GioDongCua}";

        // Navigation
        public virtual ICollection<SanPhamChiNhanh> SanPhamChiNhanhs { get; set; } = new List<SanPhamChiNhanh>();
    }
}
