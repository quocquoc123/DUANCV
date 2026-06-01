/**
 * Food Fast — Register UI only (password visibility toggle).
 */
(function () {
  'use strict';

  document.querySelectorAll('[data-toggle-password]').forEach(function (btn) {
    var targetId = btn.getAttribute('aria-controls');
    var input = targetId ? document.getElementById(targetId) : null;
    if (!input) return;

    btn.addEventListener('click', function () {
      var isPassword = input.type === 'password';
      input.type = isPassword ? 'text' : 'password';
      btn.classList.toggle('is-visible', isPassword);
      btn.setAttribute('aria-label', isPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
    });
  });
})();
