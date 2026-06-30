(function () {
  function initReviewOverlay() {
    var overlay = document.getElementById("reviewOverlay");
    if (!overlay) return;
    if (overlay.dataset.initialized === "1") return;
    overlay.dataset.initialized = "1";

    var orderId = overlay.dataset.reviewOrderId || "";
    var productId = overlay.dataset.reviewProductId || "";
    if (!orderId || !productId) return;

    var seenKey = "ff-review-popup-seen-" + orderId;
    if (window.sessionStorage.getItem(seenKey) === "1") return;

    var submitBtn = document.getElementById("reviewSubmitButton");
    var commentField = document.getElementById("reviewComment");
    var ratingValue = document.getElementById("reviewRatingValue");
    var commentValue = document.getElementById("reviewCommentValue");
    var successState = document.getElementById("reviewSuccessState");
    var ratingForm = document.getElementById("reviewRatingForm");
    var tokenInput = ratingForm ? ratingForm.querySelector("input[name='__RequestVerificationToken']") : null;
    var selectedStar = 0;

    var openTimer = null;
    var isClosing = false;

    function openOverlay() {
      if (window.sessionStorage.getItem(seenKey) === "1") return;
      if (isClosing) return;
      if (overlay.hidden === false) return;
      overlay.hidden = false;
      document.body.style.overflow = "hidden";
      window.sessionStorage.setItem(seenKey, "1");
      openTimer = null;
    }

    var escHandler = null;

    function destroyOverlay() {
      if (openTimer) {
        window.clearTimeout(openTimer);
        openTimer = null;
      }
      if (escHandler) {
        document.removeEventListener("keydown", escHandler);
        escHandler = null;
      }
      if (overlay && overlay.parentNode) {
        overlay.remove();
      }
      document.body.style.overflow = "";
    }

    function closeOverlay() {
      if (overlay.hidden === true || isClosing) return;
      isClosing = true;
      // User dismissed/handled popup for this order, do not reopen.
      window.sessionStorage.setItem(seenKey, "1");
      if (openTimer) {
        window.clearTimeout(openTimer);
        openTimer = null;
      }
      overlay.classList.add("is-closing");
      window.setTimeout(function () {
        destroyOverlay();
        isClosing = false;
      }, 240);
    }

    function setStars(value) {
      selectedStar = value;
      if (ratingValue) ratingValue.value = String(value);
      if (submitBtn) submitBtn.disabled = value < 1;

      var stars = overlay.querySelectorAll(".review-star");
      stars.forEach(function (starEl) {
        var starVal = Number(starEl.dataset.star || "0");
        starEl.classList.toggle("is-active", starVal <= value);
        starEl.classList.toggle("is-selected", starVal === value);
      });
    }

    function postForm(url, formData, headers) {
      return fetch(url, {
        method: "POST",
        headers: headers || { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
        body: new URLSearchParams(formData).toString(),
        credentials: "same-origin"
      });
    }

    overlay.querySelectorAll("[data-review-close]").forEach(function (el) {
      el.addEventListener("click", closeOverlay);
    });

    overlay.addEventListener("click", function (ev) {
      if (ev.target === overlay) closeOverlay();
    });

    escHandler = function (ev) {
      if (ev.key === "Escape" && overlay.hidden !== true) {
        closeOverlay();
      }
    };
    document.addEventListener("keydown", escHandler);

    overlay.querySelectorAll(".review-star").forEach(function (starEl) {
      starEl.addEventListener("click", function () {
        setStars(Number(starEl.dataset.star || "0"));
      });
    });

    if (submitBtn) {
      submitBtn.addEventListener("click", async function () {
        if (selectedStar < 1) return;

        submitBtn.disabled = true;
        submitBtn.textContent = "Đang gửi...";

        try {
          var ratingData = {
            maSP: productId,
            rating: String(selectedStar)
          };

          var headers = {
            "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
          };
          if (tokenInput && tokenInput.value) {
            headers["RequestVerificationToken"] = tokenInput.value;
            ratingData.__RequestVerificationToken = tokenInput.value;
          }

          await postForm("/SanPhams/AddAndUpdateRating", ratingData, headers);

          var comment = (commentField && commentField.value ? commentField.value.trim() : "");
          if (comment && commentValue) {
            commentValue.value = comment;
            await postForm("/SanPhams/AddComment", {
              maSP: productId,
              noiDung: comment
            });
          }

          if (successState) successState.hidden = false;
          submitBtn.textContent = "Đã gửi";
          window.setTimeout(closeOverlay, 700);
        } catch (err) {
          submitBtn.disabled = false;
          submitBtn.textContent = "Gửi đánh giá";
        }
      });
    }

    openTimer = window.setTimeout(openOverlay, 700);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initReviewOverlay);
  } else {
    initReviewOverlay();
  }
})();
