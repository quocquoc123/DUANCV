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
    let branchList      = [];    // Danh sách chi nhánh từ API (bao gồm lat/lon)
    let hasOutOfStock   = false; // Có sản phẩm hết hàng tại chi nhánh hay không
    let isAutoSelecting = false; // Đang tự động chọn → tránh trigger event change lặp
    const walkingCache  = new Map(); // Cache kết quả OSRM Walking theo branchId
    let distanceToken   = 0;    // Token dùng để hủy kết quả cũ khi đổi chi nhánh

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
    // Helper: Cập nhật text option trong dropdown chi nhánh kèm khoảng cách
    // Ưu tiên dùng cache walking; nếu chưa có thì để trống khoảng cách
    // -----------------------------------------------------------------------
    function populateBranchSelectOptions() {
        if (!chiNhanhSelect || !branchList || !branchList.length) return;
        var currentVal = chiNhanhSelect.value;
        chiNhanhSelect.innerHTML = '<option value="">Chọn chi nhánh giao hàng</option>';
        branchList.forEach(function (cn) {
            var opt = document.createElement('option');
            var id  = cn.maChiNhanh || cn.MaChiNhanh;
            opt.value = id;
            var label = cn.tenChiNhanh || cn.TenChiNhanh || '';
            // Lấy khoảng cách từ cache walking (đã tính chính xác)
            if (walkingCache.has(id)) {
                var cached = walkingCache.get(id);
                if (cached && cached.distKm != null) {
                    var distStr = cached.distKm < 1
                        ? Math.round(cached.distKm * 1000) + ' m'
                        : cached.distKm.toFixed(1) + ' km';
                    label += ' (' + distStr + ')';
                }
            }
            opt.textContent = label;
            chiNhanhSelect.appendChild(opt);
        });
        if (currentVal) chiNhanhSelect.value = currentVal;
    }

    // Cập nhật text cho một option cụ thể trong dropdown sau khi có dữ liệu cache
    function updateOptionLabel(branchId) {
        if (!chiNhanhSelect) return;
        var cached = walkingCache.get(branchId);
        if (!cached || cached.distKm == null) return;
        var opt = chiNhanhSelect.querySelector('option[value="' + branchId + '"]');
        if (!opt) return;
        var cn  = branchList.find(function (b) { return (b.maChiNhanh || b.MaChiNhanh) == branchId; });
        if (!cn) return;
        var label    = cn.tenChiNhanh || cn.TenChiNhanh || '';
        var distStr  = cached.distKm < 1
            ? Math.round(cached.distKm * 1000) + ' m'
            : cached.distKm.toFixed(1) + ' km';
        opt.textContent = label + ' (' + distStr + ')';
    }

    // -----------------------------------------------------------------------
    // Helper: Lấy khoảng cách đường đi bộ thực tế qua OSRM Walking API
    // -----------------------------------------------------------------------
    async function getWalkingDistanceOsrm(lat1, lon1, lat2, lon2) {
        try {
            var url = 'https://router.project-osrm.org/route/v1/foot/' + lon1 + ',' + lat1 + ';' + lon2 + ',' + lat2 + '?overview=false';
            var res = await fetch(url);
            if (res.ok) {
                var data = await res.json();
                if (data.code === 'Ok' && data.routes && data.routes.length > 0) {
                    var distM = data.routes[0].distance;
                    var durS  = data.routes[0].duration;
                    var distKm = Math.round((distM / 1000.0) * 100) / 100;
                    var durMin = Math.max(1, Math.round(durS / 60.0));
                    return { distKm: distKm, durText: durMin + ' phút đi bộ' };
                }
            }
        } catch (e) {}
        return { distKm: null, durText: '' };
    }

    // -----------------------------------------------------------------------
    // Helper: Hiển thị thông tin chi nhánh đã chọn bên dưới dropdown
    // Dùng cache walking để tránh nhảy số liệu;
    // nếu chưa có cache thì gọi API 1 lần, lưu cache và cập nhật UI
    // -----------------------------------------------------------------------
    function showBranchInfo(branch, customDistance, customDuration) {
        if (!branch) { branchInfo.hidden = true; return; }
        branchInfoName.textContent = branch.tenChiNhanh || branch.TenChiNhanh || '';
        branchInfoAddr.textContent = branch.diaChi      || branch.DiaChi      || '';

        var branchDistEl = document.getElementById('branchInfoDistance');
        if (branchDistEl) {
            var branchId = branch.maChiNhanh || branch.MaChiNhanh;
            var lat = branch.latitude  != null ? branch.latitude  : branch.Latitude;
            var lng = branch.longitude != null ? branch.longitude : branch.Longitude;

            if (customDistance != null && customDistance > 0) {
                // Khoảng cách được cung cấp sẵn từ server (khi tính phí giao hàng)
                var distTxt = customDistance < 1
                    ? (Math.round(customDistance * 1000)) + ' m'
                    : customDistance.toFixed(1) + ' km';
                var durTxt = customDuration || '';
                branchDistEl.innerHTML = '🚶 Khoảng cách đi bộ: <strong>' + distTxt + '</strong>' + (durTxt ? ' (' + durTxt + ')' : '');
                branchDistEl.hidden = false;
                // Lưu vào cache để dropdown đồng bộ
                walkingCache.set(branchId, { distKm: customDistance, durText: durTxt });
                updateOptionLabel(branchId);
            } else if (window.userLocation && window.userLocation.lat != null && window.userLocation.lng != null && lat != null && lng != null) {
                // Kiểm tra cache trước
                if (walkingCache.has(branchId)) {
                    var cached = walkingCache.get(branchId);
                    if (cached && cached.distKm != null) {
                        var cachedTxt = cached.distKm < 1
                            ? Math.round(cached.distKm * 1000) + ' m'
                            : cached.distKm.toFixed(1) + ' km';
                        branchDistEl.innerHTML = '🚶 Khoảng cách đi bộ: <strong>' + cachedTxt + '</strong>' + (cached.durText ? ' (' + cached.durText + ')' : '');
                    } else {
                        branchDistEl.innerHTML = '<span style="font-size:0.82rem;color:#6b7280;">🚶 Không xác định được lộ trình</span>';
                    }
                    branchDistEl.hidden = false;
                    return;
                }

                // Chưa có cache → gọi API 1 lần, dùng token để tránh race condition
                var myToken = ++distanceToken;
                branchDistEl.innerHTML = '🚶 Khoảng cách đi bộ: <small class="text-muted"><i class="fas fa-spinner fa-spin me-1"></i>Đang tải...</small>';
                branchDistEl.hidden = false;

                var currentLat = window.userLocation.lat;
                var currentLng = window.userLocation.lng;
                getWalkingDistanceOsrm(currentLat, currentLng, lat, lng).then(function (res) {
                    // Bỏ qua nếu người dùng đã đổi chi nhánh khác trong lúc chờ
                    if (myToken !== distanceToken) return;

                    // Lưu cache
                    walkingCache.set(branchId, res);
                    updateOptionLabel(branchId);

                    if (res && res.distKm != null) {
                        var realTxt = res.distKm < 1 ? Math.round(res.distKm * 1000) + ' m' : res.distKm.toFixed(1) + ' km';
                        branchDistEl.innerHTML = '🚶 Khoảng cách đi bộ: <strong>' + realTxt + '</strong>' + (res.durText ? ' (' + res.durText + ')' : '');
                    } else {
                        branchDistEl.innerHTML = '<span style="font-size:0.82rem;color:#6b7280;">🚶 Không xác định được lộ trình</span>';
                    }
                });
            } else {
                branchDistEl.innerHTML = '<span style="font-weight: normal; font-size: 0.82rem; color: #6b7280;">📍 Nhập địa chỉ để xem khoảng cách</span>';
                branchDistEl.hidden = false;
            }
        }

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

        var maChiNhanh = branch.maChiNhanh || branch.MaChiNhanh;

        // Đặt cờ để ngăn event change kích hoạt lại checkStock lần nữa
        isAutoSelecting = true;
        chiNhanhSelect.value = maChiNhanh;
        selectedHidden.value = maChiNhanh;
        isAutoSelecting = false;

        // Hiển thị thông tin chi nhánh kèm khoảng cách
        showBranchInfo(branch, branch.distanceKm);

        // Badge gợi ý
        if (branch.hasFullCoverage && usedGeolocation && branch.distanceKm != null) {
            var distTxt = branch.distanceKm < 1
                ? (Math.round(branch.distanceKm * 1000)) + ' m'
                : branch.distanceKm.toFixed(1) + ' km';
            autoBadge.innerHTML  = '📍 Du hang va gan ban &nbsp;·&nbsp; ' + distTxt;
        } else if (branch.hasFullCoverage) {
            autoBadge.innerHTML  = '✅ Du hang cho toan bo gio';
        } else if (usedGeolocation && branch.distanceKm != null) {
            var nearTxt = branch.distanceKm < 1
                ? (Math.round(branch.distanceKm * 1000)) + ' m'
                : branch.distanceKm.toFixed(1) + ' km';
            autoBadge.innerHTML  = '⚠️ Gan ban nhat trong nhom phu hop &nbsp;·&nbsp; ' + nearTxt;
        } else {
            var coverageCount = branch.coverageCount || 0;
            var missingCount = branch.missingCount || 0;
            autoBadge.innerHTML  = '⚠️ Tam goi y &nbsp;·&nbsp; Co ' + coverageCount + ' mon, thieu ' + missingCount + ' mon';
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
                populateBranchSelectOptions();
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
                window.userLocation = { lat: lat, lng: lon };
                populateBranchSelectOptions();
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
        var found = branchList.find(function (cn) { return (cn.maChiNhanh || cn.MaChiNhanh) == val; });
        showBranchInfo(found || null);

        // Kiểm tra tồn kho
        checkStockByBranch(parseInt(val, 10));
    });

    // -----------------------------------------------------------------------
    // Export global helper để Index.cshtml gọi khi tính phí vận chuyển thành công
    // -----------------------------------------------------------------------
    window.updateCartBranchInfo = function (maChiNhanh, distanceKm, durationText) {
        if (!chiNhanhSelect) return;
        isAutoSelecting = true;
        chiNhanhSelect.value = maChiNhanh;
        selectedHidden.value = maChiNhanh;
        isAutoSelecting = false;

        var found = branchList.find(function (cn) { return (cn.maChiNhanh || cn.MaChiNhanh) == maChiNhanh; });
        if (found) {
            showBranchInfo(found, distanceKm, durationText);
        }
        populateBranchSelectOptions();
        checkStockByBranch(parseInt(maChiNhanh, 10));
    };

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
