using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLBanDoAnNhanh.Models
{
    /// <summary>
    /// Bảng liên kết nhiều-nhiều giữa SanPham và ChiNhanh.
    /// </summary>
    [Table("SanPhamChiNhanh")]
    public class SanPhamChiNhanh
    {
        [Required]
        public int MaSp { get; set; }

        [Required]
        public int MaChiNhanh { get; set; }

        // Navigation properties
        public virtual SanPham MaSpNavigation { get; set; }
        public virtual ChiNhanh MaChiNhanhNavigation { get; set; }
    }
}
