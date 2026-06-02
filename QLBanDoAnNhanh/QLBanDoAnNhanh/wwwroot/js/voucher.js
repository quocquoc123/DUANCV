/**
 * Xử lý logic voucher/mã giảm giá trong giỏ hàng
 */

class VoucherManager {
    constructor() {
        this.voucherCode = null;
        this.voucherData = null;
        this.apiBaseUrl = '/api/voucher';
    }

    /**
     * Khởi tạo event listeners
     */
    init() {
        const applyBtn = document.getElementById('applyVoucherBtn');
        const removeBtn = document.getElementById('removeVoucherBtn');
        const voucherInput = document.getElementById('voucherCodeInput');
        const checkoutForm = document.getElementById('checkoutForm');

        if (applyBtn) {
            applyBtn.addEventListener('click', () => this.applyVoucher());
        }

        if (removeBtn) {
            removeBtn.addEventListener('click', () => this.removeVoucher());
        }

        if (voucherInput) {
            voucherInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.applyVoucher();
                }
            });
        }

        // Thêm handler cho form submit
        if (checkoutForm) {
            checkoutForm.addEventListener('submit', (e) => {
                this.updateHiddenVoucherInput(this.voucherCode);
            });
        }

        // Tính tiền khi tải trang
        this.recalculateTotal();
    }

    /**
     * Áp dụng voucher
     */
    async applyVoucher() {
        const voucherInput = document.getElementById('voucherCodeInput');
        const code = voucherInput?.value?.trim().toUpperCase();

        if (!code) {
            this.showAlert('danger', 'Vui lòng nhập mã giảm giá!');
            return;
        }

        // Lấy tổng tiền từ element
        const tongTienElement = document.getElementById('cartTotalAmount');
        const tongTien = this.parsePrice(tongTienElement?.textContent || '0');

        if (tongTien <= 0) {
            this.showAlert('danger', 'Giỏ hàng trống!');
            return;
        }

        // Hiển thị loading
        this.showLoading(true);

        try {
            const response = await fetch(`${this.apiBaseUrl}/check`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    maKhuyenMai: code,
                    tongTien: tongTien
                })
            });

            const data = await response.json();

            if (data.success) {
                this.voucherCode = code;
                this.voucherData = data.data;

                // Tính tiền giảm
                const tienGiam = this.calculateDiscount(tongTien, data.data.giaTri);
                const tongSauGiam = tongTien - tienGiam;

                // Cập nhật giao diện
                this.updateVoucherUI(code, data.data.giaTri, tienGiam, tongSauGiam);

                // Hiển thị success message
                this.showAlert('success', data.message);

                // Lưu vào session storage
                this.saveVoucherToSession(code, data.data.giaTri, tienGiam, tongSauGiam);
            } else {
                this.showAlert('danger', data.message);
            }
        } catch (error) {
            console.error('Error:', error);
            this.showAlert('danger', 'Lỗi khi kiểm tra mã giảm giá. Vui lòng thử lại!');
        } finally {
            this.showLoading(false);
        }
    }

    /**
     * Xóa voucher
     */
    removeVoucher() {
        const voucherInput = document.getElementById('voucherCodeInput');
        if (voucherInput) voucherInput.value = '';

        this.voucherCode = null;
        this.voucherData = null;

        // Cập nhật giao diện
        const voucherSection = document.getElementById('voucherSection');
        if (voucherSection) {
            voucherSection.classList.add('d-none');
        }

        const discountAmount = document.getElementById('discountAmount');
        if (discountAmount) {
            discountAmount.textContent = '0 VND';
        }

        const cartTotalAmount = document.getElementById('cartTotalAmount');
        const tongTien = this.parsePrice(cartTotalAmount?.textContent || '0');

        const totalAfterDiscount = document.getElementById('totalAfterDiscount');
        if (totalAfterDiscount) {
            totalAfterDiscount.textContent = this.formatCurrency(tongTien);
        }

        // Clear hidden input
        const hiddenVoucher = document.getElementById('hiddenVoucherCode');
        if (hiddenVoucher) {
            hiddenVoucher.value = '';
        }

        // Xóa từ session storage
        sessionStorage.removeItem('appliedVoucher');
        sessionStorage.removeItem('voucherData');
        sessionStorage.removeItem('voucherDiscount');

        this.showAlert('info', 'Đã xóa mã giảm giá!');
    }

    /**
     * Cập nhật UI khi áp dụng voucher thành công
     */
    updateVoucherUI(code, giaTri, tienGiam, tongSauGiam) {
        // Cập nhật badge hiển thị
        const voucherCodeDisplay = document.getElementById('voucherCodeDisplay');
        if (voucherCodeDisplay) voucherCodeDisplay.textContent = code;

        const voucherPercentDisplay = document.getElementById('voucherPercentDisplay');
        if (voucherPercentDisplay) voucherPercentDisplay.textContent = giaTri;

        // Cập nhật các giá trị tiền
        const discountAmount = document.getElementById('discountAmount');
        if (discountAmount) discountAmount.textContent = this.formatCurrency(tienGiam);

        const totalAfterDiscount = document.getElementById('totalAfterDiscount');
        if (totalAfterDiscount) totalAfterDiscount.textContent = this.formatCurrency(tongSauGiam);

        // Hiển thị voucher badge
        const voucherSection = document.getElementById('voucherSection');
        if (voucherSection) {
            voucherSection.classList.remove('d-none');
        }

        // Cập nhật hidden input để gửi cùng form
        this.updateHiddenVoucherInput(code);
    }

    /**
     * Cập nhật hidden input để gửi voucher
     */
    updateHiddenVoucherInput(code) {
        let hiddenInput = document.getElementById('hiddenVoucherCode');
        
        if (!hiddenInput) {
            // Tạo nếu chưa tồn tại
            const checkoutForm = document.getElementById('checkoutForm');
            if (checkoutForm) {
                hiddenInput = document.createElement('input');
                hiddenInput.type = 'hidden';
                hiddenInput.id = 'hiddenVoucherCode';
                hiddenInput.name = 'voucherCode';
                checkoutForm.appendChild(hiddenInput);
            }
        }
        
        if (hiddenInput) {
            hiddenInput.value = code || '';
        }
    }

    /**
     * Tính tiền giảm
     */
    calculateDiscount(tongTien, giaTri) {
        return Math.round(tongTien * (giaTri / 100));
    }

    /**
     * Parse giá từ chuỗi (loại bỏ VND, dấu phẩy)
     */
    parsePrice(priceStr) {
        if (!priceStr) return 0;
        // Loại bỏ "VND", dấu phẩy, khoảng trắng
        return parseFloat(String(priceStr).replace(/[^\d.-]/g, '')) || 0;
    }

    /**
     * Format tiền tệ
     */
    formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(Math.round(amount)).replace('₫', 'VND');
    }

    /**
     * Hiển thị alert
     */
    showAlert(type, message) {
        const alertContainer = document.getElementById('voucherAlertContainer');
        if (!alertContainer) return;

        const alertClass = {
            'success': 'alert-success',
            'danger': 'alert-danger',
            'info': 'alert-info',
            'warning': 'alert-warning'
        }[type] || 'alert-info';

        const alertHtml = `
            <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Đóng"></button>
            </div>
        `;

        alertContainer.innerHTML = alertHtml;

        // Auto dismiss sau 4 giây nếu là success
        if (type === 'success') {
            setTimeout(() => {
                const alert = alertContainer.querySelector('.alert');
                if (alert) {
                    alert.classList.remove('show');
                    setTimeout(() => alert.remove(), 150);
                }
            }, 4000);
        }
    }

    /**
     * Hiển thị loading
     */
    showLoading(show) {
        const applyBtn = document.getElementById('applyVoucherBtn');
        if (!applyBtn) return;

        if (show) {
            applyBtn.disabled = true;
            applyBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang kiểm tra...';
        } else {
            applyBtn.disabled = false;
            applyBtn.innerHTML = '<i class="fas fa-check"></i> Áp dụng';
        }
    }

    /**
     * Tính lại tổng tiền (dùng khi tải lại trang)
     */
    recalculateTotal() {
        const cartTotalAmount = document.getElementById('cartTotalAmount');
        const totalAfterDiscount = document.getElementById('totalAfterDiscount');
        
        if (cartTotalAmount && totalAfterDiscount) {
            const tongTien = this.parsePrice(cartTotalAmount.textContent);
            totalAfterDiscount.textContent = this.formatCurrency(tongTien);
        }
    }

    /**
     * Lưu voucher vào session storage
     */
    saveVoucherToSession(code, giaTri, tienGiam, tongSauGiam) {
        sessionStorage.setItem('appliedVoucher', code);
        sessionStorage.setItem('voucherData', JSON.stringify({
            code: code,
            giaTri: giaTri,
            tienGiam: tienGiam,
            tongSauGiam: tongSauGiam
        }));
    }

    /**
     * Load voucher từ session storage khi tải lại trang
     */
    loadVoucherFromSession() {
        const code = sessionStorage.getItem('appliedVoucher');
        const dataStr = sessionStorage.getItem('voucherData');

        if (code && dataStr) {
            try {
                const voucherData = JSON.parse(dataStr);
                this.voucherCode = code;
                this.voucherData = {
                    maKhuyenMai: code,
                    giaTri: voucherData.giaTri
                };
                
                // Cập nhật UI
                this.updateVoucherUI(code, voucherData.giaTri, voucherData.tienGiam, voucherData.tongSauGiam);
                
                // Cập nhật input
                const voucherInput = document.getElementById('voucherCodeInput');
                if (voucherInput) voucherInput.value = code;
            } catch (e) {
                console.error('Error loading voucher from session:', e);
                sessionStorage.removeItem('appliedVoucher');
                sessionStorage.removeItem('voucherData');
            }
        }
    }

    /**
     * Lấy mã voucher đã áp dụng
     */
    getAppliedVoucher() {
        return this.voucherCode;
    }

    /**
     * Lấy dữ liệu voucher
     */
    getVoucherData() {
        return this.voucherData;
    }
}

// Khởi tạo khi trang load
document.addEventListener('DOMContentLoaded', () => {
    const voucherManager = new VoucherManager();
    voucherManager.init();
    voucherManager.loadVoucherFromSession();

    // Lưu vào window để sử dụng ở các nơi khác
    window.voucherManager = voucherManager;
});
