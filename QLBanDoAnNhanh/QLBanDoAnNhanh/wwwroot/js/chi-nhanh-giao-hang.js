/**
 * chi-nhanh-giao-hang.js
 * Xử lý dropdown chọn chi nhánh giao hàng tại trang giỏ hàng.
 *
 * Luồng chính:
 *  1. Tải danh sách tất cả chi nhánh (API: GetChiNhanhs).
 *  2. Xin quyền định vị người dùng (Geolocation API).
 *  3. Gọi API GetSuggestedBranch (gửi tọa độ hoặc null) → server tính chi nhánh
 *     gần nhất có đủ hàng cho giỏ hàng → tự động chọn vào dropdown.
 *  4. Hiển thị badge "📍 Gần bạn nhất" nếu có tọa độ, hoặc "✅ Phù hợp nhất" nếu không.
 *  5. Khi người dùng thay đổi thủ công → kiểm tra tồn kho, cập nhật UI.
 */
document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    // -----------------------------------------------------------------------
    // Tham chiếu DOM
    // -----------------------------------------------------------------------
    const chiNhanhSelect   = document.getElementById('chiNhanhSelect');
    const selectedHidden   = document.getElementById('selectedChiNhanh');
    const branchInfo       = document.getElementById('branchInfo');
    const branchInfoName   = document.getElementById('branchInfoName');
    const branchInfoAddr   = document.getElementById('branchInfoAddress');
    const branchStockAlert = document.getElementById('branchStockAlert');
    const btnCheckout      = document.querySelector('.btn-checkout');

    if (!chiNhanhSelect) return; // Trang không có dropdown → thoát

    // -----------------------------------------------------------------------
    // Biến lưu trạng thái
    // -----------------------------------------------------------------------
    let branchList    = [];    // Danh sách chi nhánh từ API (bao gồm lat/lon)
    let hasOutOfStock = false; // Có sản phẩm hết hàng tại chi nhánh hay không
    let isAutoSelecting = false; // Đang tự động chọn → tránh trigger event change lặp

    // -----------------------------------------------------------------------
    // Tạo badge hiển thị gợi ý tự động
    // -----------------------------------------------------------------------
    const autoBadge = document.createElement('span');
    autoBadge.id        = 'branchAutoBadge';
    autoBadge.className = 'branch-auto-badge';
    autoBadge.hidden    = true;
    // Chèn badge vào ngay sau select
    chiNhanhSelect.parentNode.insertBefore(autoBadge, chiNhanhSelect.nextSibling);

    // -----------------------------------------------------------------------
    // Helper: Hiển thị trạng thái nút Thanh toán
    // -----------------------------------------------------------------------
    function updateCheckoutBtn() {
        if (!btnCheckout) return;
        if (hasOutOfStock) {
            btnCheckout.disabled = true;
            btnCheckout.title    = 'Một số sản phẩm không còn hàng tại chi nhánh này';
        } else {
            btnCheckout.disabled = false;
            btnCheckout.title    = '';
        }
    }

    // -----------------------------------------------------------------------
    // Helper: Hiển thị thông tin chi nhánh đã chọn bên dưới dropdown
    // -----------------------------------------------------------------------
    function showBranchInfo(branch) {
        if (!branch) { branchInfo.hidden = true; return; }
        branchInfoName.textContent = branch.tenChiNhanh || branch.TenChiNhanh || '';
        branchInfoAddr.textContent = branch.diaChi      || branch.DiaChi      || '';
        branchInfo.hidden = false;
    }

    function escapeHtml(str) {
        if (!str) return '';
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    // -----------------------------------------------------------------------
    // Helper: Kiểm tra tồn kho theo chi nhánh đã chọn (gọi khi user chọn thủ công)
    // -----------------------------------------------------------------------
    function checkStockByBranch(maChiNhanh) {
        fetch('/GioHangs/CheckStockByBranch', {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(maChiNhanh)
        })
        .then(function (res) {
            if (!res.ok) throw new Error('Lỗi kiểm tra tồn kho');
            return res.json();
        })
        .then(function (data) {
            hasOutOfStock = data.outOfStock;
            if (hasOutOfStock && data.items && data.items.length > 0) {
                var names = data.items.map(function (item) { return item.tenSp || item.TenSp; }).join(', ');
                branchStockAlert.innerHTML = '⚠️ Các sản phẩm sau không còn hàng tại chi nhánh này: <strong>' + escapeHtml(names) + '</strong>.';
                branchStockAlert.hidden = false;
            } else {
                branchStockAlert.hidden = true;
            }
            updateCheckoutBtn();
        })
        .catch(function (err) {
            console.warn('[ChiNhanh] ' + err.message);
            hasOutOfStock = false;
            branchStockAlert.hidden = true;
            updateCheckoutBtn();
        });
    }

    // -----------------------------------------------------------------------
    // Helper: Tự động chọn chi nhánh vào dropdown và cập nhật UI
    // -----------------------------------------------------------------------
    function applyAutoSelect(branch, usedGeolocation) {
        if (!branch) return;

        var maChiNhanh = branch.maChiNhanh;

        // Đặt cờ để ngăn event change kích hoạt lại checkStock lần nữa
        isAutoSelecting = true;
        chiNhanhSelect.value = maChiNhanh;
        selectedHidden.value = maChiNhanh;
        isAutoSelecting = false;

        // Hiển thị thông tin chi nhánh
        showBranchInfo(branch);

        // Badge gợi ý
        if (usedGeolocation && branch.distanceKm != null) {
            var distTxt = branch.distanceKm < 1
                ? (Math.round(branch.distanceKm * 1000)) + ' m'
                : branch.distanceKm.toFixed(1) + ' km';
            autoBadge.innerHTML  = '📍 Gần bạn nhất &nbsp;·&nbsp; ' + distTxt;
        } else {
            autoBadge.innerHTML  = '✅ Phù hợp với giỏ hàng';
        }
        autoBadge.hidden = false;

        // Kiểm tra tồn kho cho chi nhánh đã chọn tự động
        if (!branch.hasFullCoverage) {
            checkStockByBranch(maChiNhanh);
        } else {
            hasOutOfStock = false;
            branchStockAlert.hidden = true;
            updateCheckoutBtn();
        }
    }

    // -----------------------------------------------------------------------
    // Bước 1: Tải danh sách chi nhánh → populate dropdown
    // -----------------------------------------------------------------------
    function loadChiNhanhs() {
        return fetch('/GioHangs/GetChiNhanhs', { method: 'GET', credentials: 'same-origin' })
            .then(function (res) {
                if (!res.ok) throw new Error('Lỗi khi tải danh sách chi nhánh.');
                return res.json();
            })
            .then(function (data) {
                branchList = data;
                data.forEach(function (cn) {
                    var opt = document.createElement('option');
                    opt.value       = cn.maChiNhanh;
                    opt.textContent = cn.tenChiNhanh;
                    chiNhanhSelect.appendChild(opt);
                });
                return data;
            });
    }

    // -----------------------------------------------------------------------
    // Bước 2: Gọi API GetSuggestedBranch (có hoặc không có tọa độ)
    // -----------------------------------------------------------------------
    function fetchSuggestedBranch(lat, lon) {
        var body = (lat !== null && lon !== null)
            ? { latitude: lat, longitude: lon }
            : {};

        return fetch('/GioHangs/GetSuggestedBranch', {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        })
        .then(function (res) {
            if (!res.ok) throw new Error('Lỗi khi lấy chi nhánh gợi ý.');
            return res.json();
        });
    }

    // -----------------------------------------------------------------------
    // Bước 2a: Xin quyền định vị, sau đó gọi API
    // -----------------------------------------------------------------------
    function requestLocationThenSuggest() {
        if (!navigator.geolocation) {
            // Trình duyệt không hỗ trợ → gọi API không có tọa độ
            fetchSuggestedBranch(null, null).then(function (data) {
                if (data.success && data.branch) applyAutoSelect(data.branch, false);
            }).catch(function (e) { console.warn('[ChiNhanh] ' + e.message); });
            return;
        }

        navigator.geolocation.getCurrentPosition(
            // Thành công: có tọa độ
            function (pos) {
                var lat = pos.coords.latitude;
                var lon = pos.coords.longitude;
                fetchSuggestedBranch(lat, lon).then(function (data) {
                    if (data.success && data.branch) applyAutoSelect(data.branch, true);
                }).catch(function (e) { console.warn('[ChiNhanh] ' + e.message); });
            },
            // Thất bại / người dùng từ chối → gọi API không tọa độ
            function () {
                fetchSuggestedBranch(null, null).then(function (data) {
                    if (data.success && data.branch) applyAutoSelect(data.branch, false);
                }).catch(function (e) { console.warn('[ChiNhanh] ' + e.message); });
            },
            { timeout: 6000, maximumAge: 60000, enableHighAccuracy: false }
        );
    }

    // -----------------------------------------------------------------------
    // Event: Người dùng thay đổi chi nhánh thủ công
    // -----------------------------------------------------------------------
    chiNhanhSelect.addEventListener('change', function () {
        if (isAutoSelecting) return; // Bỏ qua nếu đang auto-select

        var val = this.value;
        selectedHidden.value = val;

        // Ẩn badge gợi ý khi user tự chọn
        autoBadge.hidden = true;

        // Reset trạng thái tồn kho
        branchStockAlert.hidden = true;
        hasOutOfStock = false;
        updateCheckoutBtn();

        if (!val) {
            branchInfo.hidden = true;
            return;
        }

        // Hiển thị thông tin chi nhánh
        var found = branchList.find(function (cn) { return cn.maChiNhanh == val; });
        showBranchInfo(found || null);

        // Kiểm tra tồn kho
        checkStockByBranch(parseInt(val, 10));
    });

    // -----------------------------------------------------------------------
    // Khởi động: load danh sách → xin định vị → auto-select
    // -----------------------------------------------------------------------
    loadChiNhanhs()
        .then(function () {
            // Sau khi dropdown đã có đầy đủ options mới auto-select
            requestLocationThenSuggest();
        })
        .catch(function (e) {
            console.warn('[ChiNhanh] Không tải được danh sách chi nhánh: ' + e.message);
        });

});
