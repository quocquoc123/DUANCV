/**
 * tinh-phi-giao-hang.js
 * Hàm gọi API /SanPhams/TinhPhiGiaoHang bằng fetch API (POST)
 */

/**
 * Gọi API tính phí giao hàng và gợi ý chi nhánh gần nhất.
 * @param {Object} options
 * @param {number} [options.sanPhamId=0] - ID sản phẩm cần kiểm tra tồn kho (tùy chọn)
 * @param {string} [options.diaChiKhachHang=''] - Địa chỉ của khách hàng (nếu chưa có tọa độ)
 * @param {number|null} [options.latKhachHang=null] - Vĩ độ của khách hàng (nếu có từ Autocomplete / Geolocation)
 * @param {number|null} [options.lngKhachHang=null] - Kinh độ của khách hàng (nếu có từ Autocomplete / Geolocation)
 * @param {string} [options.tieuChi='distance'] - "distance" (km gần nhất) hoặc "duration" (thời gian nhanh nhất)
 * @returns {Promise<Object>} Trả về object kết quả từ Server { success, chiNhanhId, tenChiNhanh, khoangCachKm, thoiGianText, phiGiaoHang, phiGiaoHangFormatted, message }
 */
async function tinhPhiGiaoHang({ sanPhamId = 0, diaChiKhachHang = '', latKhachHang = null, lngKhachHang = null, tieuChi = 'distance' } = {}) {
    try {
        const response = await fetch('/SanPhams/TinhPhiGiaoHang', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify({
                sanPhamId: parseInt(sanPhamId, 10) || 0,
                diaChiKhachHang: diaChiKhachHang ? diaChiKhachHang.trim() : '',
                latKhachHang: latKhachHang !== null ? parseFloat(latKhachHang) : null,
                lngKhachHang: lngKhachHang !== null ? parseFloat(lngKhachHang) : null,
                tieuChi: tieuChi
            })
        });

        if (!response.ok) {
            throw new Error(`Lỗi HTTP ${response.status}: ${response.statusText}`);
        }

        const data = await response.json();
        return data;
    } catch (error) {
        console.error('[TinhPhiGiaoHang Error]:', error);
        return {
            success: false,
            message: 'Không thể kết nối đến máy chủ: ' + error.message
        };
    }
}

/**
 * Ví dụ minh họa: Gắn sự kiện tính phí giao hàng từ ô nhập địa chỉ hoặc nút "Tính phí giao hàng"
 */
function initShippingFeeCalculator() {
    const btnCalculate  = document.getElementById('btnTinhPhiGiaoHang');
    const inputAddress  = document.getElementById('diaChiKhachHangInput');
    const tieuChiSelect = document.getElementById('tieuChiSelect');
    const resultBox     = document.getElementById('ketQuaGiaoHangBox');

    if (!btnCalculate || !inputAddress) return;

    btnCalculate.addEventListener('click', async function () {
        const address = inputAddress.value ? inputAddress.value.trim() : '';
        if (!address) {
            alert('Vui lòng nhập địa chỉ giao hàng.');
            return;
        }

        btnCalculate.disabled = true;
        btnCalculate.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> Đang tính...';

        if (resultBox) {
            resultBox.style.display = 'block';
            resultBox.className = 'alert alert-info mt-3';
            resultBox.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i> Đang tìm chi nhánh gần nhất và tính phí giao hàng...';
        }

        const criteria = tieuChiSelect ? tieuChiSelect.value : 'distance';

        // Gọi hàm API
        const result = await tinhPhiGiaoHang({
            diaChiKhachHang: address,
            tieuChi: criteria
        });

        btnCalculate.disabled = false;
        btnCalculate.innerHTML = '<i class="fas fa-search-location me-1"></i> Tính phí';

        if (resultBox) {
            resultBox.style.display = 'block';
            if (result.success) {
                resultBox.className = 'alert alert-success mt-3';
                resultBox.innerHTML = `
                    <h5 class="alert-heading"><i class="fas fa-store me-1"></i> Chi nhánh phục vụ: <strong>${result.tenChiNhanh}</strong></h5>
                    <p class="mb-1">📍 <strong>Địa chỉ chi nhánh:</strong> ${result.diaChiChiNhanh}</p>
                    <p class="mb-1">🚗 <strong>Khoảng cách:</strong> ${result.khoangCachKm} km (${result.thoiGianText})</p>
                    <p class="mb-0 text-danger font-weight-bold fs-5">💰 <strong>Phí giao hàng:</strong> ${result.phiGiaoHangFormatted}</p>
                `;
            } else {
                resultBox.className = 'alert alert-danger mt-3';
                resultBox.innerHTML = `<i class="fas fa-exclamation-circle me-1"></i> <strong>Không thể tính phí:</strong> ${result.message}`;
            }
        }
    });

    // Cho phép ấn Enter ở ô địa chỉ để tính phí
    inputAddress.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            btnCalculate.click();
        }
    });
}

// Tự động khởi tạo nếu DOM đã tải xong
document.addEventListener('DOMContentLoaded', initShippingFeeCalculator);
