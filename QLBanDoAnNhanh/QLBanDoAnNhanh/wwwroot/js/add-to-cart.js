/**
 * add-to-cart.js  –  FoodFast "Thêm vào giỏ hàng" AJAX + Fly-to-Cart Animation
 * ================================================================================
 * Cung cấp hàm chung: FoodFast.addToCart(maSp, quantity, imgElement, btnElement)
 *
 * Tính năng:
 *  - Gọi AJAX POST /GioHangs/AddToCartAjax
 *  - Loading state + chống double-click
 *  - Fly-to-cart animation: clone ảnh bay tới icon giỏ trên header
 *  - Bounce animation trên icon giỏ khi ảnh chạm
 *  - Badge số lượng cập nhật ngay lập tức với pulse
 *  - Toast thành công / lỗi
 *  - Event delegation tự động cho tất cả .btn-buy trên trang
 * ================================================================================
 */

(function () {
    'use strict';

    // -----------------------------------------------------------------------
    // Namespace
    // -----------------------------------------------------------------------
    window.FoodFast = window.FoodFast || {};

    // -----------------------------------------------------------------------
    // Toast
    // -----------------------------------------------------------------------
    let _toastTimer = null;

    function ensureToast() {
        let el = document.getElementById('ff-cart-toast');
        if (!el) {
            el = document.createElement('div');
            el.id = 'ff-cart-toast';
            el.innerHTML = '<span class="ff-toast-icon"></span><span class="ff-toast-msg"></span>';
            document.body.appendChild(el);
        }
        return el;
    }

    function showToast(msg, isError) {
        const toast = ensureToast();
        const icon  = toast.querySelector('.ff-toast-icon');
        const msgEl = toast.querySelector('.ff-toast-msg');

        toast.classList.remove('show', 'ff-toast-error');
        void toast.offsetWidth; // reflow để reset animation

        icon.textContent  = isError ? '⚠️' : '🛒';
        msgEl.textContent = msg;
        if (isError) toast.classList.add('ff-toast-error');
        toast.classList.add('show');

        clearTimeout(_toastTimer);
        _toastTimer = setTimeout(() => toast.classList.remove('show'), 3400);
    }

    // -----------------------------------------------------------------------
    // Cập nhật badge giỏ hàng
    // -----------------------------------------------------------------------
    function updateCartBadge(count) {
        const badge = document.querySelector('.cart-count');
        if (!badge) return;
        badge.textContent = count;
        badge.classList.remove('ff-badge-pulse');
        void badge.offsetWidth;
        badge.classList.add('ff-badge-pulse');
        badge.addEventListener('animationend', () => badge.classList.remove('ff-badge-pulse'), { once: true });
    }

    // -----------------------------------------------------------------------
    // Bounce icon giỏ hàng
    // -----------------------------------------------------------------------
    function bounceCartIcon() {
        const cartImg = document.getElementById('ff-cart-icon');
        if (!cartImg) return;
        cartImg.classList.remove('ff-cart-bounce');
        void cartImg.offsetWidth;
        cartImg.classList.add('ff-cart-bounce');
        cartImg.addEventListener('animationend', () => cartImg.classList.remove('ff-cart-bounce'), { once: true });
    }

    // -----------------------------------------------------------------------
    // Fly-to-cart animation
    // -----------------------------------------------------------------------
    function flyToCart(imgElement, onComplete) {
        const cartIcon = document.getElementById('ff-cart-icon');
        if (!imgElement || !cartIcon) {
            if (onComplete) onComplete();
            return;
        }

        // Lấy bounding rect của ảnh nguồn
        const srcRect  = imgElement.getBoundingClientRect();
        const destRect = cartIcon.getBoundingClientRect();

        // Kích thước ban đầu
        const startW = Math.min(srcRect.width, 120);
        const startH = Math.min(srcRect.height, 120);

        // Tạo clone ảnh
        const clone = document.createElement('img');
        clone.src = imgElement.src || imgElement.currentSrc || '';
        clone.classList.add('ff-fly-img');

        // Vị trí ban đầu
        clone.style.width  = startW + 'px';
        clone.style.height = startH + 'px';
        clone.style.top    = (srcRect.top  + srcRect.height / 2 - startH / 2) + 'px';
        clone.style.left   = (srcRect.left + srcRect.width  / 2 - startW / 2) + 'px';
        clone.style.opacity = '1';
        clone.style.transition = 'none'; // bắt đầu không có transition

        document.body.appendChild(clone);

        // Tính vị trí đích (trung tâm icon giỏ)
        const destTop  = destRect.top  + destRect.height / 2;
        const destLeft = destRect.left + destRect.width  / 2;

        // Kích hoạt transition sau 1 frame
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                clone.style.transition = ''; // bật lại từ CSS
                clone.style.top    = (destTop  - 16) + 'px';
                clone.style.left   = (destLeft - 16) + 'px';
                clone.style.width  = '32px';
                clone.style.height = '32px';
                clone.style.opacity = '0.2';
            });
        });

        // Cleanup sau khi animation xong
        clone.addEventListener('transitionend', function handler(e) {
            if (e.propertyName !== 'opacity') return;
            clone.removeEventListener('transitionend', handler);
            clone.remove();
            if (onComplete) onComplete();
        });

        // Fallback timeout đề phòng transitionend không bắn
        setTimeout(() => {
            if (clone.parentNode) clone.remove();
            if (onComplete) onComplete();
        }, 1000);
    }

    // -----------------------------------------------------------------------
    // Hàm chính: FoodFast.addToCart
    // -----------------------------------------------------------------------
    FoodFast.addToCart = async function (maSp, quantity, imgElement, btnElement) {
        if (!maSp) return;

        // Chống double-click
        if (btnElement && btnElement.disabled) return;
        if (btnElement) {
            btnElement.disabled = true;
            btnElement.classList.add('ff-loading');
        }

        const originalText = btnElement ? btnElement.innerHTML : '';

        try {
            const res = await fetch('/GioHangs/AddToCartAjax', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'MaSp=' + encodeURIComponent(maSp) + '&quantity=' + encodeURIComponent(quantity || 1)
            });

            if (!res.ok) {
                throw new Error('HTTP ' + res.status);
            }

            const data = await res.json();

            if (!data.success) {
                showToast(data.message || 'Không thể thêm vào giỏ hàng.', true);
                return;
            }

            // Cập nhật badge ngay lập tức
            updateCartBadge(data.cartCount);

            // Chạy animation bay
            flyToCart(imgElement, function () {
                bounceCartIcon();
                showToast('Đã thêm vào giỏ hàng.', false);
            });

        } catch (err) {
            showToast('Có lỗi xảy ra. Vui lòng thử lại.', true);
            console.error('[FoodFast AddToCart]', err);
        } finally {
            // Khôi phục nút sau 1.2s (đủ cho animation hoàn thành)
            setTimeout(() => {
                if (btnElement) {
                    btnElement.disabled = false;
                    btnElement.classList.remove('ff-loading');
                    btnElement.innerHTML = originalText;
                }
            }, 1200);
        }
    };

    // -----------------------------------------------------------------------
    // Event delegation – tự động xử lý tất cả .btn-buy có data-ma-sp
    // Áp dụng cho: server-rendered + JS-rendered cards
    // -----------------------------------------------------------------------
    document.addEventListener('click', function (e) {
        // Tìm nút btn-buy gần nhất trong bubbling
        const btn = e.target.closest('.btn-buy[data-ma-sp]');
        if (!btn) return;

        e.preventDefault();
        e.stopPropagation();

        const maSp    = btn.dataset.maSp;
        const imgSrc  = btn.dataset.img || '';
        const quantity = parseInt(btn.dataset.quantity || '1', 10);

        // Tìm ảnh trong card cha
        const card = btn.closest('.product-card, .product-item, [data-product-card]') ||
                     btn.parentElement;
        let imgEl = null;
        if (card) {
            imgEl = card.querySelector('img.product-image, img.product-img, img');
        }

        // Nếu không tìm được ảnh từ DOM nhưng có data-img, tạo image element giả
        if (!imgEl && imgSrc) {
            imgEl = new Image();
            imgEl.src = imgSrc;
            // Đặt vị trí gần nút
            const btnRect = btn.getBoundingClientRect();
            imgEl.style.cssText = `position:fixed; top:${btnRect.top}px; left:${btnRect.left}px; width:60px; height:60px;`;
        }

        FoodFast.addToCart(maSp, quantity, imgEl, btn);
    });

})();
