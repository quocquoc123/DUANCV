# 🧪 HƯỚNG DẪN KIỂM TRA CHỨC NĂNG VOUCHER

## 📋 Mục Lục

1. [Chuẩn Bị](#chuẩn-bị)
2. [Test Admin](#test-admin)
3. [Test User](#test-user)
4. [Test Edge Cases](#test-edge-cases)
5. [Database Query](#database-query)

---

## 🔧 Chuẩn Bị

### **Bước 1: Ensure các file đã được tạo**

```
✓ Models/VoucherViewModel.cs
✓ Services/VoucherService.cs
✓ Controllers/VoucherController.cs
✓ wwwroot/js/voucher.js
✓ wwwroot/css/voucher.css
✓ Views/KhuyenMais/* (đã cập nhật)
✓ Views/GioHangs/Index.cshtml (đã cập nhật)
✓ Controllers/GioHangsController.cs (đã cập nhật)
✓ Controllers/KhuyenMaisController.cs (đã cập nhật)
✓ Program.cs (đã cập nhật)
```

### **Bước 2: Clean Build**

```bash
# Đóng Visual Studio
# Xóa thư mục: bin, obj

# Mở lại Visual Studio
# Build Solution: Ctrl + Shift + B
# Hoặc: dotnet build
```

### **Bước 3: Run Application**

```bash
dotnet run
# Hoặc F5 trong Visual Studio
```

**Expected:** Ứng dụng chạy trên `https://localhost:7001`

---

## 👨‍💼 TEST ADMIN

### **Test 1: Tạo Voucher Mới**

**Step 1:** Truy cập `/KhuyenMais/Create`

```
URL: https://localhost:7001/KhuyenMais/Create
```

**Step 2:** Điền form

| Field | Value |
|-------|-------|
| Mã Khuyến Mãi | `FOOD10` |
| Giá Trị Giảm | `10` |
| Số Lượng Mã | `100` |
| Điều Kiện Áp Dụng | `0` (để trống = không có điều kiện) |
| Thời Gian Bắt Đầu | `2024-01-01 00:00` |
| Thời Gian Kết Thúc | `2024-12-31 23:59` |
| Kích Hoạt | ✅ (checked) |

**Step 3:** Click "Tạo Khuyến Mãi"

**Expected Result:**
```
✅ Tạo thành công
✅ Redirect về /KhuyenMais/Index
✅ Thấy FOOD10 trong danh sách
✅ Trạng thái: Kích Hoạt ✓
```

---

### **Test 2: Kiểm Tra Validation - Mã Trùng**

**Step 1:** Tạo voucher với mã `FOOD10` lần 2

**Step 2:** Click "Tạo Khuyến Mãi"

**Expected Result:**
```
❌ Hiển thị lỗi: "Mã khuyến mãi này đã tồn tại!"
```

---

### **Test 3: Kiểm Tra Validation - Giá Trị Invalid**

**Step 1:** Tạo voucher với:
```
Mã: TEST100
Giá Trị: 150 (> 100%)
```

**Step 2:** Click "Tạo Khuyến Mãi"

**Expected Result:**
```
❌ Hiển thị lỗi: "Giá trị giảm phải từ 1-100!"
```

---

### **Test 4: Kiểm Tra Validation - Thời Gian Invalid**

**Step 1:** Tạo voucher với:
```
Mã: TEST101
Thời Gian Bắt Đầu: 2024-12-31 23:59
Thời Gian Kết Thúc: 2024-01-01 00:00 (bắt đầu > kết thúc)
```

**Step 2:** Click "Tạo Khuyến Mãi"

**Expected Result:**
```
❌ Hiển thị lỗi: "Thời gian kết thúc phải sau thời gian bắt đầu!"
```

---

### **Test 5: Danh Sách Voucher**

**Step 1:** Truy cập `/KhuyenMais/Index`

**Expected Result:**
```
✅ Hiển thị danh sách voucher
✅ Mã        : FOOD10
✅ Giảm Giá  : 10% (badge xanh)
✅ Trạng Thái: Kích Hoạt ✓
✅ Hành Động : [Sửa] [Chi Tiết] [Xóa]
```

---

### **Test 6: Sửa Voucher**

**Step 1:** Click [Sửa] trên FOOD10

**Step 2:** Thay đổi:
```
Giá Trị: 15 (từ 10 → 15)
Số Lượng: 50 (từ 100 → 50)
```

**Step 3:** Click "Cập Nhật Khuyến Mãi"

**Expected Result:**
```
✅ Cập nhật thành công
✅ Redirect về danh sách
✅ FOOD10 hiển thị: 15%, SoLuong = 50
```

---

### **Test 7: Chi Tiết Voucher**

**Step 1:** Click [Chi Tiết] trên FOOD10

**Expected Result:**
```
✅ Hiển thị tất cả thông tin
✅ Mã        : FOOD10
✅ Giảm Giá  : 15%
✅ Số Lượng  : 50
✅ Trạng Thái: Kích Hoạt ✓
✅ Ngày Tạo  : [Timestamp]
```

---

### **Test 8: Xóa Voucher**

**Step 1:** Click [Xóa] trên FOOD10

**Step 2:** Xác nhận "Xóa Khuyến Mãi"

**Expected Result:**
```
✅ Xóa thành công
✅ FOOD10 biến mất khỏi danh sách
```

---

## 👤 TEST USER (Giỏ Hàng & Voucher)

### **Chuẩn Bị**

**Tạo 3 voucher demo:**

```sql
INSERT INTO KhuyenMai 
VALUES 
('FOOD10', 10, GETDATE(), DATEADD(DAY, 365, GETDATE()), 1, GETDATE(), 0, 100),
('VIP50', 50, GETDATE(), DATEADD(DAY, 365, GETDATE()), 1, GETDATE(), 500000, 50),
('LIMITED5', 5, GETDATE(), DATEADD(DAY, 30, GETDATE()), 1, GETDATE(), NULL, 3);
```

---

### **Test 1: Thêm Sản Phẩm**

**Step 1:** Truy cập `/SanPhams/TrangChu` (Trang Chủ)

**Step 2:** Click "Mua Ngay" trên 1 sản phẩm

**Expected Result:**
```
✅ Sản phẩm được thêm vào giỏ
✅ Session "CartItemCount" cập nhật
✅ Redirect về /GioHangs/Index
```

---

### **Test 2: Giỏ Hàng Có Phần Voucher**

**Step 1:** Ở `/GioHangs/Index`

**Expected Result:**
```
✅ Bên phải sidebar có section "Mã Giảm Giá"
✅ Input: [ Nhập mã giảm giá ] [Áp dụng]
✅ Hint text: "VD: FOOD10, FOOD20, VIP30"
✅ Alert container trống
```

---

### **Test 3: Áp Dụng Voucher - Success**

**Step 1:** Nhập `FOOD10` vào input

**Step 2:** Click "Áp dụng" (hoặc nhấn Enter)

**Expected Result - UI:**
```
✅ Alert hiển thị:
   "✓ Áp dụng mã FOOD10 thành công - Giảm 10%!"

✅ Badge hiển thị:
   [Mã: FOOD10 -10%] [X]

✅ Tính toán cập nhật:
   Giảm giá : XXX VND (tính từ tổng * 10%)
   Tổng thanh toán: YYY VND (giảm)

✅ Hidden input "hiddenVoucherCode" = "FOOD10"
```

**Expected Result - Ví Dụ Cụ Thể:**

Nếu tổng giỏ = 1,000,000 VND:
```
Tạm tính       : 1,000,000 VND
Phí vận chuyển : Miễn phí
Giảm giá       : 100,000 VND ← (1,000,000 * 10 / 100)
─────────────────────────────
Tổng thanh toán: 900,000 VND ← (1,000,000 - 100,000)
```

---

### **Test 4: Áp Dụng Voucher - Error Cases**

#### **4.1: Mã không tồn tại**

**Input:** `INVALID123`

**Expected:**
```
❌ Alert: "Mã giảm giá không tồn tại!"
```

#### **4.2: Mã không kích hoạt**

**Setup:** Tạo voucher `DISABLED1` với TrangThai = false

**Input:** `DISABLED1`

**Expected:**
```
❌ Alert: "Mã giảm giá chưa được kích hoạt!"
```

#### **4.3: Mã hết hạn**

**Setup:** Tạo voucher `EXPIRED1` với ThoiGianKetThuc = 2020-01-01

**Input:** `EXPIRED1`

**Expected:**
```
❌ Alert: "Mã giảm giá đã hết hạn!"
```

#### **4.4: Mã hết lượt**

**Setup:** Tạo voucher `NOLIMIT1` với SoLuong = 0

**Input:** `NOLIMIT1`

**Expected:**
```
❌ Alert: "Mã giảm giá đã hết lượt sử dụng!"
```

#### **4.5: Đơn hàng chưa đủ điều kiện**

**Setup:** Tạo voucher `MINORDER1` với DieuKienApDung = 5,000,000

**Giỏ hàng:** 1,000,000 VND

**Input:** `MINORDER1`

**Expected:**
```
❌ Alert: "Đơn hàng phải từ 5,000,000 VND để áp dụng mã này!"
```

---

### **Test 5: Xóa Voucher (Remove)**

**Step 1:** Sau khi áp dụng FOOD10, click [X] trên badge

**Expected:**
```
✅ Input được xóa: [ Nhập mã giảm giá ]
✅ Badge biến mất
✅ Giảm giá reset = 0 VND
✅ Tổng thanh toán = tổng gốc
✅ SessionStorage: removedVoucher
```

---

### **Test 6: Reload Trang - Voucher Persistent**

**Step 1:** Áp dụng FOOD10

**Step 2:** F5 reload trang

**Expected:**
```
✅ Voucher vẫn còn (từ sessionStorage)
✅ Giảm giá vẫn hiển thị
✅ Badge vẫn có
```

---

### **Test 7: Thanh Toán Với Voucher**

**Step 1:** Áp dụng FOOD10

**Step 2:** Điền form:
```
Họ tên: Nguyễn Văn A
Số điện thoại: 0123456789
Địa chỉ: 123 Nguyễn Hữu Cảnh, Q1, HCM
```

**Step 3:** Click "Thanh Toán"

**Expected - Database:**
```sql
-- Check DonHang
SELECT MaDh, MaKhuyenMai, TongTien 
FROM DonHang 
ORDER BY CreatedAt DESC 
LIMIT 1

Result:
MaDh          : [GUID]
MaKhuyenMai   : FOOD10 ✅
TongTien      : 900000 (giả sử gốc 1,000,000)
```

**Expected - Giao Diện:**
```
✅ Message: "Thanh toán thành công! Cảm ơn bạn đã mua hàng."
✅ Redirect: /SanPhams/TrangChu
✅ Giỏ hàng trống
```

---

### **Test 8: Kiểm Tra SoLuong Giảm**

**Step 1:** Trước khi checkout:
```sql
SELECT SoLuong FROM KhuyenMai WHERE MaKhuyenMai = 'FOOD10'
Result: 100
```

**Step 2:** Sau khi checkout FOOD10:
```sql
SELECT SoLuong FROM KhuyenMai WHERE MaKhuyenMai = 'FOOD10'
Result: 99 ✅ (giảm từ 100 xuống 99)
```

---

## 🔬 TEST EDGE CASES

### **Test 1: Multiple Vouchers Applied**

**Step 1:** Áp dụng FOOD10

**Step 2:** Thay đổi input thành VIP50, click "Áp dụng"

**Expected:**
```
✅ Voucher mới thay thế voucher cũ
✅ Badge cập nhật: Mã: VIP50 -50%
✅ Giảm giá tính lại (nếu điều kiện đủ)
```

---

### **Test 2: Giỏ Hàng Rỗng + Voucher**

**Step 1:** Xóa tất cả sản phẩm khỏi giỏ

**Step 2:** Nhập voucher

**Expected:**
```
❌ Alert: "Giỏ hàng trống!"
❌ Không cho phép áp dụng
```

---

### **Test 3: Voucher + PayPal**

**Step 1:** Áp dụng FOOD10

**Step 2:** Click "PayPal"

**Expected:**
```
✅ Hidden input "hiddenVoucherCode" được gửi
✅ PayPal nhận voucher
```

---

### **Test 4: API Direct Call**

**Using Postman/curl:**

```bash
curl -X POST https://localhost:7001/api/voucher/check \
  -H "Content-Type: application/json" \
  -d '{
    "maKhuyenMai": "FOOD10",
    "tongTien": 500000
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "✓ Áp dụng mã FOOD10 thành công - Giảm 10%!",
  "data": {
    "maKhuyenMai": "FOOD10",
    "giaTri": 10,
    "dieuKienApDung": 0,
    "thoiGianBatDau": "2024-01-01T00:00:00",
    "thoiGianKetThuc": "2024-12-31T23:59:00",
    "trangThai": true
  }
}
```

---

## 📊 DATABASE QUERY

### **Kiểm Tra Voucher**

```sql
-- Tất cả voucher
SELECT * FROM KhuyenMai

-- Voucher đang hoạt động
SELECT * FROM KhuyenMai 
WHERE TrangThai = 1 
AND GETDATE() BETWEEN ThoiGianBatDau AND ThoiGianKetThuc

-- Voucher hết lượt
SELECT * FROM KhuyenMai WHERE SoLuong = 0

-- Voucher đã được dùng
SELECT MaDh, MaKhuyenMai, TongTien FROM DonHang 
WHERE MaKhuyenMai IS NOT NULL

-- Đếm voucher đã dùng
SELECT MaKhuyenMai, COUNT(*) as LanSuDung 
FROM DonHang 
WHERE MaKhuyenMai IS NOT NULL 
GROUP BY MaKhuyenMai
```

---

## ✅ Checklist Kiểm Tra

- [ ] Admin: Tạo voucher thành công
- [ ] Admin: Validation mã trùng
- [ ] Admin: Validation giá trị
- [ ] Admin: Validation thời gian
- [ ] Admin: Danh sách hiển thị đúng
- [ ] Admin: Sửa voucher
- [ ] Admin: Xem chi tiết
- [ ] Admin: Xóa voucher

- [ ] User: Thêm sản phẩm vào giỏ
- [ ] User: Phần voucher hiển thị
- [ ] User: Áp dụng voucher success
- [ ] User: Alert error mã không tồn tại
- [ ] User: Alert error mã hết hạn
- [ ] User: Alert error mã hết lượt
- [ ] User: Alert error điều kiện
- [ ] User: Xóa voucher
- [ ] User: Reload trang, voucher persist
- [ ] User: Thanh toán với voucher
- [ ] User: SoLuong voucher giảm

- [ ] API: Endpoint check voucher
- [ ] API: Endpoint calculate discount

- [ ] Database: DonHang lưu MaKhuyenMai
- [ ] Database: KhuyenMai SoLuong giảm

---

## 🎯 Result Summary

| Test | Expected | Actual | Pass |
|------|----------|--------|------|
| Admin: Tạo voucher | ✅ | ? | ? |
| Admin: Validation | ✅ | ? | ? |
| Admin: Danh sách | ✅ | ? | ? |
| User: Áp dụng voucher | ✅ | ? | ? |
| User: Error handling | ✅ | ? | ? |
| User: Thanh toán | ✅ | ? | ? |
| DB: Save voucher | ✅ | ? | ? |
| API: Check voucher | ✅ | ? | ? |

---

**Status:** Ready for QA ✅
