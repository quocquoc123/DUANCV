(function () {
    'use strict';

    function pad(value) {
        return String(value).padStart(2, '0');
    }

    function renderMomoQrCanvas() {
        var canvas = document.getElementById('momoQrCanvas');
        var payloadInput = document.getElementById('momoQrPayload');
        if (!canvas || !payloadInput || typeof QRCode === 'undefined') {
            var img = document.getElementById('momoQrImage');
            if (img) {
                img.hidden = false;
            }
            return;
        }

        var payload = payloadInput.value;
        if (!payload) {
            return;
        }

        QRCode.toCanvas(canvas, payload, {
            width: 240,
            margin: 1,
            color: { dark: '#000000', light: '#ffffff' }
        }, function (error) {
            if (error) {
                var img = document.getElementById('momoQrImage');
                if (img) {
                    img.hidden = false;
                    canvas.style.display = 'none';
                }
            }
        });
    }

    function initMomoQrPage() {
        renderMomoQrCanvas();

        var countdownEl = document.getElementById('momoCountdown');
        if (!countdownEl) {
            return;
        }

        var expiresValue = countdownEl.getAttribute('data-expires');
        var orderId = countdownEl.getAttribute('data-order');
        var statusEl = document.getElementById('momoPaymentStatus');
        var qrImage = document.getElementById('momoQrImage');
        var expired = false;

        function updateCountdown() {
            if (!expiresValue || expired) {
                return;
            }

            var remaining = new Date(expiresValue).getTime() - Date.now();
            if (remaining <= 0) {
                countdownEl.textContent = '00:00';
                countdownEl.classList.add('is-expired');
                expired = true;

                if (statusEl) {
                    statusEl.textContent = 'QR đã hết hiệu lực';
                }

                if (qrImage) {
                    qrImage.style.opacity = '0.35';
                    qrImage.style.filter = 'grayscale(1)';
                }

                fetch('/ThanhToan/ExpireMomoPayment?maDh=' + encodeURIComponent(orderId))
                    .catch(function () { });

                setTimeout(function () {
                    window.location.reload();
                }, 1500);
                return;
            }

            var totalSeconds = Math.floor(remaining / 1000);
            var minutes = Math.floor(totalSeconds / 60);
            var seconds = totalSeconds % 60;
            countdownEl.textContent = pad(minutes) + ':' + pad(seconds);
        }

        function pollStatus() {
            if (!orderId || expired) {
                return;
            }

            fetch('/ThanhToan/CheckMomoStatus?maDh=' + encodeURIComponent(orderId))
                .then(function (res) { return res.json(); })
                .then(function (data) {
                    if (data.status === 'Paid') {
                        window.location.href = '/ThanhToan/MomoSuccess?maDh=' + encodeURIComponent(orderId);
                        return;
                    }

                    if (data.status === 'Expired') {
                        expired = true;
                        window.location.reload();
                    }
                })
                .catch(function () { });
        }

        updateCountdown();
        setInterval(updateCountdown, 1000);
        setInterval(pollStatus, 3000);
    }

    function initCartPaymentMethod() {
        var form = document.getElementById('checkoutForm');
        if (!form) {
            return;
        }

        var options = form.querySelectorAll('.payment-method-option');
        options.forEach(function (option) {
            var radio = option.querySelector('input[type="radio"]');
            if (!radio) {
                return;
            }

            function syncSelected() {
                options.forEach(function (opt) {
                    opt.classList.toggle('is-selected', opt.querySelector('input[type="radio"]')?.checked);
                });
            }

            radio.addEventListener('change', syncSelected);
            syncSelected();
        });

        form.addEventListener('submit', function (e) {
            var submitter = e.submitter;
            if (submitter && submitter.classList.contains('btn-paypal')) {
                return;
            }

            var selected = form.querySelector('input[name="paymentMethod"]:checked');
            if (selected && selected.value === 'momo') {
                e.preventDefault();
                form.action = '/ThanhToan/CreateMomoPayment';
                form.method = 'post';
                form.submit();
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initMomoQrPage();
            initCartPaymentMethod();
        });
    } else {
        initMomoQrPage();
        initCartPaymentMethod();
    }
})();
