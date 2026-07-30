document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    var API_BASE = 'https://provinces.open-api.vn/api';

    var provinceSelect = document.getElementById('province');
    var districtSelect = document.getElementById('district');
    var wardSelect = document.getElementById('ward');
    var streetInput = document.getElementById('streetAddress');
    var diaChiHidden = document.getElementById('DiaChi');
    var addressPreview = document.getElementById('addressPreview');
    var addressError = document.getElementById('addressError');
    var checkoutForm = document.getElementById('checkoutForm');

    if (!provinceSelect || !checkoutForm) {
        return;
    }

    var selectedProvinceName = '';
    var selectedDistrictName = '';
    var selectedWardName = '';
    var initialAddress = (checkoutForm.getAttribute('data-initial-address') || '').trim();

    function showError(message) {
        if (!addressError) return;
        addressError.textContent = message;
        addressError.hidden = !message;
    }

    function setLoading(selectEl, loadingEl, isLoading, placeholder) {
        if (loadingEl) {
            loadingEl.hidden = !isLoading;
        }
        selectEl.disabled = isLoading;
        if (isLoading && placeholder) {
            selectEl.innerHTML = '<option value="">' + placeholder + '</option>';
        }
    }

    function fillSelect(selectEl, items, placeholder) {
        var html = '<option value="">' + placeholder + '</option>';
        items.forEach(function (item) {
            html += '<option value="' + item.code + '">' + item.name + '</option>';
        });
        selectEl.innerHTML = html;
        selectEl.disabled = false;
    }

    function resetDistrict() {
        selectedDistrictName = '';
        districtSelect.innerHTML = '<option value="">-- Chọn Quận/Huyện --</option>';
        districtSelect.disabled = true;
        resetWard();
    }

    function resetWard() {
        selectedWardName = '';
        wardSelect.innerHTML = '<option value="">-- Chọn Phường/Xã --</option>';
        wardSelect.disabled = true;
    }

    function buildFullAddress() {
        var street = (streetInput.value || '').trim();
        if (!selectedProvinceName || !selectedDistrictName || !selectedWardName || !street) {
            return '';
        }
        return street + ', ' + selectedWardName + ', ' + selectedDistrictName + ', ' + selectedProvinceName;
    }

    function updatePreview() {
        var full = buildFullAddress();
        if (addressPreview) {
            if (full) {
                addressPreview.textContent = full;
                addressPreview.hidden = false;
            } else {
                addressPreview.hidden = true;
                addressPreview.textContent = '';
            }
        }
        if (diaChiHidden) {
            diaChiHidden.value = full;
            diaChiHidden.dispatchEvent(new CustomEvent('addressChanged', { detail: full }));
        }
    }

    function findOptionByText(selectEl, targetText) {
        var target = (targetText || '').trim().toLowerCase();
        if (!target) return null;
        for (var i = 0; i < selectEl.options.length; i++) {
            var txt = (selectEl.options[i].text || '').trim().toLowerCase();
            if (txt === target) return selectEl.options[i];
        }
        return null;
    }

    function splitAddressParts(fullAddress) {
        if (!fullAddress) return null;
        var parts = fullAddress.split(',').map(function (x) { return x.trim(); }).filter(Boolean);
        if (parts.length < 4) return null;
        return {
            street: parts.slice(0, parts.length - 3).join(', '),
            ward: parts[parts.length - 3],
            district: parts[parts.length - 2],
            province: parts[parts.length - 1]
        };
    }

    async function applyInitialAddress() {
        var parsed = splitAddressParts(initialAddress);
        if (!parsed) return;

        // Điền sẵn đường/số nhà
        if (streetInput && !streetInput.value.trim()) {
            streetInput.value = parsed.street;
        }

        // Chọn tỉnh/thành
        var provinceOpt = findOptionByText(provinceSelect, parsed.province);
        if (!provinceOpt) return;
        provinceSelect.value = provinceOpt.value;
        selectedProvinceName = provinceOpt.text;

        await loadDistricts(provinceOpt.value);

        // Chọn quận/huyện
        var districtOpt = findOptionByText(districtSelect, parsed.district);
        if (!districtOpt) {
            updatePreview();
            return;
        }
        districtSelect.value = districtOpt.value;
        selectedDistrictName = districtOpt.text;

        await loadWards(districtOpt.value);

        // Chọn phường/xã
        var wardOpt = findOptionByText(wardSelect, parsed.ward);
        if (!wardOpt) {
            updatePreview();
            return;
        }
        wardSelect.value = wardOpt.value;
        selectedWardName = wardOpt.text;

        updatePreview();
    }

    async function fetchJson(url) {
        var response = await fetch(url);
        if (!response.ok) {
            throw new Error('HTTP ' + response.status);
        }
        return response.json();
    }

    async function loadProvinces() {
        var loadingEl = document.getElementById('provinceLoading');
        setLoading(provinceSelect, loadingEl, true, 'Đang tải Tỉnh/Thành phố...');
        showError('');

        try {
            var provinces = await fetchJson(API_BASE + '/p/');
            fillSelect(provinceSelect, provinces, '-- Chọn Tỉnh/Thành phố --');
        } catch (err) {
            provinceSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
            provinceSelect.disabled = true;
            showError('Không thể tải danh sách Tỉnh/Thành phố. Vui lòng thử lại sau.');
            console.error('Load provinces failed:', err);
        } finally {
            if (loadingEl) loadingEl.hidden = true;
        }
    }

    async function loadDistricts(provinceCode) {
        var loadingEl = document.getElementById('districtLoading');
        setLoading(districtSelect, loadingEl, true, 'Đang tải Quận/Huyện...');
        showError('');

        try {
            var data = await fetchJson(API_BASE + '/p/' + provinceCode + '?depth=2');
            var districts = data.districts || [];
            fillSelect(districtSelect, districts, '-- Chọn Quận/Huyện --');
        } catch (err) {
            districtSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
            districtSelect.disabled = true;
            showError('Không thể tải danh sách Quận/Huyện. Vui lòng chọn lại Tỉnh/Thành phố.');
            console.error('Load districts failed:', err);
        } finally {
            if (loadingEl) loadingEl.hidden = true;
        }
    }

    async function loadWards(districtCode) {
        var loadingEl = document.getElementById('wardLoading');
        setLoading(wardSelect, loadingEl, true, 'Đang tải Phường/Xã...');
        showError('');

        try {
            var data = await fetchJson(API_BASE + '/d/' + districtCode + '?depth=2');
            var wards = data.wards || [];
            fillSelect(wardSelect, wards, '-- Chọn Phường/Xã --');
        } catch (err) {
            wardSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
            wardSelect.disabled = true;
            showError('Không thể tải danh sách Phường/Xã. Vui lòng chọn lại Quận/Huyện.');
            console.error('Load wards failed:', err);
        } finally {
            if (loadingEl) loadingEl.hidden = true;
        }
    }

    provinceSelect.addEventListener('change', function () {
        var code = provinceSelect.value;
        var option = provinceSelect.options[provinceSelect.selectedIndex];
        selectedProvinceName = option ? option.text : '';
        resetDistrict();
        updatePreview();

        if (code) {
            loadDistricts(code);
        }
    });

    districtSelect.addEventListener('change', function () {
        var code = districtSelect.value;
        var option = districtSelect.options[districtSelect.selectedIndex];
        selectedDistrictName = option ? option.text : '';
        resetWard();
        updatePreview();

        if (code) {
            loadWards(code);
        }
    });

    wardSelect.addEventListener('change', function () {
        var option = wardSelect.options[wardSelect.selectedIndex];
        selectedWardName = option ? option.text : '';
        updatePreview();
    });

    streetInput.addEventListener('input', updatePreview);

    checkoutForm.addEventListener('submit', function (e) {
        var full = buildFullAddress();
        if (!full) {
            e.preventDefault();
            showError('Vui lòng chọn đầy đủ Tỉnh/Thành phố, Quận/Huyện, Phường/Xã và nhập số nhà, tên đường.');
            return;
        }
        diaChiHidden.value = full;
        showError('');
    });

    loadProvinces().then(applyInitialAddress);
});
