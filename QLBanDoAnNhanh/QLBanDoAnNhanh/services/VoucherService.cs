using Microsoft.EntityFrameworkCore;
using QLBanDoAnNhanh.Models;

namespace QLBanDoAnNhanh.Services
{
    /// <summary>
    /// Service quản lý Voucher (Mã Giảm Giá)
    /// </summary>
    public class VoucherService
    {
        private readonly QlbanDoAnNhanh3Context _context;

        public VoucherService(QlbanDoAnNhanh3Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Kiểm tra và lấy thông tin voucher
        /// </summary>
        public async Task<VoucherCheckResponse> CheckVoucherAsync(string maKhuyenMai, double tongTien)
        {
            // Kiểm tra mã trống
            if (string.IsNullOrWhiteSpace(maKhuyenMai))
            {
                return new VoucherCheckResponse
                {
                    Success = false,
                    Message = "Vui lòng nhập mã giảm giá!"
                };
            }

            // Tìm kiếm mã giảm giá
            var khuyenMai = await _context.KhuyenMais
                .FirstOrDefaultAsync(km => km.MaKhuyenMai.ToUpper() == maKhuyenMai.ToUpper());

            if (khuyenMai == null)
            {
                return new VoucherCheckResponse
                {
                    Success = false,
                    Message = "Mã giảm giá không tồn tại!"
                };
            }

            // Kiểm tra trạng thái
            if (!khuyenMai.TrangThai)
            {
                return new VoucherCheckResponse
                {
                    Success = false,
                    Message = "Mã giảm giá chưa được kích hoạt!"
                };
            }

            // Kiểm tra thời gian
            var now = DateTime.Now;
            if (now < khuyenMai.ThoiGianBatDau)
            {
                return new VoucherCheckResponse
                {
                    Success = false,
                    Message = "Mã giảm giá chưa có hiệu lực!"
                };
            }

            if (now > khuyenMai.ThoiGianKetThuc)
            {
                return new VoucherCheckResponse
                {
                    Success = false,
                    Message = "Mã giảm giá đã hết hạn!"
                };
            }

            // Kiểm tra số lượng còn lại
            if (khuyenMai.SoLuong <= 0)
            {
                return new VoucherCheckResponse
                {
                    Success = false,
                    Message = "Mã giảm giá đã hết lượt sử dụng!"
                };
            }

            // Kiểm tra điều kiện áp dụng
            if (khuyenMai.DieuKienApDung.HasValue && tongTien < khuyenMai.DieuKienApDung)
            {
                return new VoucherCheckResponse
                {
                    Success = false,
                    Message = $"Đơn hàng phải từ {string.Format("{0:N0}", khuyenMai.DieuKienApDung)} VND để áp dụng mã này!"
                };
            }

            // Tất cả kiểm tra đều thành công
            return new VoucherCheckResponse
            {
                Success = true,
                Message = $"✓ Áp dụng mã {khuyenMai.MaKhuyenMai} thành công - Giảm {khuyenMai.GiaTri}%!",
                Data = new VoucherViewModel
                {
                    MaKhuyenMai = khuyenMai.MaKhuyenMai,
                    GiaTri = khuyenMai.GiaTri,
                    DieuKienApDung = khuyenMai.DieuKienApDung ?? 0,
                    ThoiGianBatDau = khuyenMai.ThoiGianBatDau,
                    ThoiGianKetThuc = khuyenMai.ThoiGianKetThuc,
                    TrangThai = khuyenMai.TrangThai
                }
            };
        }

        /// <summary>
        /// Tính tiền giảm từ voucher
        /// </summary>
        public double CalculateDiscount(double tongTien, int giaTri)
        {
            if (giaTri <= 0 || giaTri > 100)
                return 0;

            return tongTien * (giaTri / 100.0);
        }

        /// <summary>
        /// Giảm số lượng sử dụng của voucher
        /// </summary>
        public async Task<bool> DecrementVoucherCountAsync(string maKhuyenMai)
        {
            try
            {
                var khuyenMai = await _context.KhuyenMais
                    .FirstOrDefaultAsync(km => km.MaKhuyenMai == maKhuyenMai);

                if (khuyenMai == null)
                    return false;

                if (khuyenMai.SoLuong > 0)
                {
                    khuyenMai.SoLuong -= 1;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy thông tin voucher
        /// </summary>
        public async Task<KhuyenMai> GetVoucherAsync(string maKhuyenMai)
        {
            return await _context.KhuyenMais
                .FirstOrDefaultAsync(km => km.MaKhuyenMai == maKhuyenMai);
        }
    }
}
