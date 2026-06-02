using Microsoft.AspNetCore.Mvc;
using QLBanDoAnNhanh.Models;
using QLBanDoAnNhanh.Services;

namespace QLBanDoAnNhanh.Controllers
{
    /// <summary>
    /// API Controller để xử lý các yêu cầu liên quan đến voucher
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VoucherController : ControllerBase
    {
        private readonly VoucherService _voucherService;

        public VoucherController(VoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        /// <summary>
        /// Kiểm tra mã voucher
        /// POST: /api/voucher/check
        /// </summary>
        [HttpPost("check")]
        public async Task<IActionResult> CheckVoucher([FromBody] VoucherCheckRequest request)
        {
            // Validation
            if (request == null)
            {
                return BadRequest(new VoucherCheckResponse
                {
                    Success = false,
                    Message = "Yêu cầu không hợp lệ!"
                });
            }

            if (string.IsNullOrWhiteSpace(request.MaKhuyenMai))
            {
                return BadRequest(new VoucherCheckResponse
                {
                    Success = false,
                    Message = "Vui lòng nhập mã giảm giá!"
                });
            }

            if (request.TongTien <= 0)
            {
                return BadRequest(new VoucherCheckResponse
                {
                    Success = false,
                    Message = "Tổng tiền không hợp lệ!"
                });
            }

            // Kiểm tra voucher
            var result = await _voucherService.CheckVoucherAsync(request.MaKhuyenMai, request.TongTien);
            return Ok(result);
        }

        /// <summary>
        /// Tính tiền giảm từ voucher
        /// POST: /api/voucher/calculate
        /// </summary>
        [HttpPost("calculate")]
        public IActionResult CalculateDiscount([FromBody] VoucherCalculateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.MaKhuyenMai))
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            var discount = _voucherService.CalculateDiscount(request.TongTien, request.GiaTri);
            var tongSauGiam = request.TongTien - discount;

            return Ok(new
            {
                success = true,
                tongTien = request.TongTien,
                giaTri = request.GiaTri,
                tienGiam = discount,
                tongSauGiam = tongSauGiam,
                maKhuyenMai = request.MaKhuyenMai
            });
        }
    }

    /// <summary>
    /// Request model cho kiểm tra voucher
    /// </summary>
    public class VoucherCheckRequest
    {
        public string MaKhuyenMai { get; set; }
        public double TongTien { get; set; }
    }

    /// <summary>
    /// Request model cho tính tiền giảm
    /// </summary>
    public class VoucherCalculateRequest
    {
        public string MaKhuyenMai { get; set; }
        public double TongTien { get; set; }
        public int GiaTri { get; set; }
    }
}
