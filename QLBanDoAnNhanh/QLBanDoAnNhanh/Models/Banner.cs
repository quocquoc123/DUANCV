using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLBanDoAnNhanh.Models
{
    /// <summary>
    /// Banner hero (trái/phải) cho trang chủ hoặc từng danh mục.
    /// MaDm = null → trang chủ; có MaDm → banner của danh mục đó.
    /// </summary>
    [Table("Banner")]
    public class Banner
    {
        [Key]
        public int MaBanner { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [MaxLength(200)]
        [Display(Name = "Tiêu đề")]
        public string TieuDe { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Hình ảnh")]
        public string HinhAnh { get; set; } = string.Empty;

        /// <summary>Left | Right</summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Vị trí")]
        public string ViTri { get; set; } = "Left";

        /// <summary>null = trang chủ; có giá trị = banner theo danh mục</summary>
        [Display(Name = "Danh mục")]
        public int? MaDm { get; set; }

        [Display(Name = "Thứ tự")]
        public int ThuTu { get; set; } = 0;

        [Display(Name = "Đang hiển thị")]
        public bool TrangThai { get; set; } = true;

        [Display(Name = "Ngày cập nhật")]
        public DateTime? NgayCapNhat { get; set; }

        [ForeignKey(nameof(MaDm))]
        public virtual DanhMuc MaDmNavigation { get; set; } = null!;
    }
}
