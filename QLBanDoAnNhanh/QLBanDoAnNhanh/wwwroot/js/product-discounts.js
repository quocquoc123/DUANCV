(function () {
    const currencyFormatter = new Intl.NumberFormat("vi-VN", {
        style: "currency",
        currency: "VND",
        maximumFractionDigits: 0
    });

    function toNumber(value) {
        const number = Number(String(value || "").replace(",", "."));
        return Number.isFinite(number) ? number : 0;
    }

    function calculateDiscountPrice(originalPrice, percent) {
        if (percent <= 0 || percent > 100) {
            return originalPrice;
        }

        return Math.round(originalPrice - originalPrice * percent / 100);
    }

    function updatePreview() {
        const originalPrice = toNumber($("#discountOriginalPriceValue").val());
        const percent = toNumber($("#discountPercent").val());
        const discountPrice = calculateDiscountPrice(originalPrice, percent);

        $("#discountPreviewPrice").text(currencyFormatter.format(discountPrice));
    }

    function showClientError(message) {
        $("#discountClientError").removeClass("d-none").text(message);
    }

    function hideClientError() {
        $("#discountClientError").addClass("d-none").text("");
    }

    $(function () {
        $("#productDiscountTable").DataTable({
            language: {
                search: "Tìm kiếm:",
                lengthMenu: "Hiển thị _MENU_ dòng",
                info: "Hiển thị _START_ đến _END_ trong _TOTAL_ dòng",
                infoEmpty: "Không có dữ liệu",
                zeroRecords: "Không tìm thấy sản phẩm",
                paginate: {
                    first: "Đầu",
                    last: "Cuối",
                    next: "Sau",
                    previous: "Trước"
                }
            },
            order: [[1, "asc"]],
            columnDefs: [
                { orderable: false, targets: [0, 8] }
            ]
        });

        $(document).on("click", ".js-open-discount-modal", function () {
            const button = $(this);
            const originalPrice = toNumber(button.data("original-price"));

            hideClientError();
            $("#discountProductId").val(button.data("product-id"));
            $("#discountProductName").val(button.data("product-name"));
            $("#discountOriginalPrice").val(currencyFormatter.format(originalPrice));
            $("#discountOriginalPriceValue").val(originalPrice);
            $("#discountPercent").val(button.data("discount-percent") || "");
            $("#discountStartDate").val(button.data("start-date"));
            $("#discountEndDate").val(button.data("end-date"));

            updatePreview();
        });

        $("#discountPercent").on("input", updatePreview);

        $("#discountForm").on("submit", function (event) {
            hideClientError();

            const percent = toNumber($("#discountPercent").val());
            const startDate = new Date($("#discountStartDate").val());
            const endDate = new Date($("#discountEndDate").val());

            if (percent < 1 || percent > 100) {
                event.preventDefault();
                showClientError("Phần trăm giảm giá phải từ 1 đến 100.");
                return;
            }

            if (!$("#discountStartDate").val() || !$("#discountEndDate").val() || endDate <= startDate) {
                event.preventDefault();
                showClientError("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
            }
        });

        $(".js-cancel-discount-form").on("submit", function (event) {
            if (!confirm("Bạn có chắc muốn hủy giảm giá sản phẩm này?")) {
                event.preventDefault();
            }
        });
    });
})();
