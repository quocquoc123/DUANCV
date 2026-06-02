# 📋 HƯỚNG DẪN SỬ DỤNG CHỨC NĂNG MÃ GIẢM GIÁ (VOUCHER)

## 🎯 Tổng Quan

Hệ thống **Mã Giảm Giá (Voucher)** cho phép quản lý các mã khuyến mãi và áp dụng chúng khi thanh toán đơn hàng.

---

## 📁 Cấu Trúc File

```
QLBanDoAnNhanh/
├── Models/
│   └── VoucherViewModel.cs           # ViewModels cho Voucher
├── Services/
│   └── VoucherService.cs             # Service xử lý logic Voucher
├── Controllers/
│   ├── VoucherController.cs          # API Controller cho Voucher
│   ├── KhuyenMaisController.cs       # Quản lý khuyến mãi (Admin)
│   └── GioHangsController.cs         # Giỏ hàng (đã cập nhật)
├── Views/
│   ├── KhuyenMais/                   # Views quản lý khuyến mãi
│   │   ├── Index.cshtml              # Danh sách
│   │   ├── Create.cshtml             # Tạo mới
│   │   ├── Edit.cshtml               # Sửa
│   │   ├── Details.cshtml            # Chi tiết
│   │   └── Delete.cshtml             # Xóa
│   └── GioHangs/
│       └── Index.cshtml              # Giỏ hàng (đã cập nhật)
├── wwwroot/
│   ├── js/
│   │   └── voucher.js                # JavaScript xử lý Voucher (AJAX)
│   └── css/
│       └── voucher.css               # CSS cho Voucher
└── Program.cs                         # (đã cập nhật)
```

---

## 🚀 PHẦN 1: QUẢN LÝ KHUYẾN MÃI (ADMIN)

### 📍 Truy Cập

**URL:** `/KhuyenMais/Index`

### ✨ Chức Năng

#### 1. **Danh Sách Khuyến Mãi**
- Hiển thị tất cả mã khuyến mãi
- Thông tin: Mã, Giá trị (%), Thời gian, Số lượng, Trạng thái
- Tìm kiếm, lọc theo trạng thái

#### 2. **Tạo Mã Khuyến Mãi**
**URL:** `/KhuyenMais/Create`

**Form Nhập Liệu:**
```
Mã Khuyến Mãi          : FOOD10 (bắt buộc, không được trùng)
Giá Trị Giảm (%)       : 10 (1-100, bắt buộc)
Số Lượng Mã            : 100 (bắt buộc, > 0)
Điều Kiện Áp Dụng      : 50000 (tuỳ chọn, VND)
Thời Gian Bắt Đầu      : 2024-01-01 10:00 (bắt buộc)
Thời Gian Kết Thúc     : 2024-12-31 23:59 (bắt buộc)
Kích Hoạt              : ☐ (checkbox)
```

**Validation:**
- ✅ Mã không được trống
- ✅ Mã không được trùng lặp
- ✅ Giá trị phải từ 1-100%
- ✅ Số lượng phải > 0
- ✅ Thời gian kết thúc > thời gian bắt đầu

**Ví Dụ:**
```
Mã: FOOD10        → Giảm 10%
Mã: FOOD20        → Giảm 20%
Mã: VIP30         → Giảm 30%
Mã: WELCOME50     → Giảm 50%
```

#### 3. **Sửa Mã Khuyến Mãi**
**URL:** `/KhuyenMais/Edit/{MaKhuyenMai}`

- Mã không thể thay đổi
- Có thể sửa tất cả trường khác
- Ngày tạo được giữ nguyên

#### 4. **Xóa Mã Khuyến Mãi**
**URL:** `/KhuyenMais/Delete/{MaKhuyenMai}`

- Xác nhận trước khi xóa
- Hiển thị chi tiết mã trước khi xóa

---

## 🛒 PHẦN 2: ÁPDỤNG VOUCHER TRONG GIỎ HÀNG

### 📍 Truy Cập

**URL:** `/GioHangs/Index`

### ✨ Giao Diện

Trên trang giỏ hàng, **bên phải** (sidebar), trước phần "Thanh Toán":

```
┌─────────────────────────┐
│   Mã Giảm Giá          │
├─────────────────────────┤
│                         │
│ [ Nhập mã giảm giá ]   │ ← Input field
│                [Áp dụng]   │ ← Button
│                         │
│ VD: FOOD10, FOOD20     │ ← Hint text
│                         │
│ ┌─ Mã: FOOD10 -10% ─┐ │ ← Display khi áp dụng
│ └─────────────────────┘ │
│                         │
├─────────────────────────┤
│ Thông Tin Thanh Toán   │
└─────────────────────────┘
```

### 🔄 Quy Trình Áp Dụng Voucher

#### **Bước 1: Nhập Mã**
- Nhập mã vào ô input
- Ví dụ: `FOOD10`

#### **Bước 2: Nhấn Áp Dụng**
- Click nút "Áp dụng" hoặc nhấn **Enter**
- Hệ thống gửi yêu cầu AJAX đến API

#### **Bước 3: Kiểm Tra (Phía Server)**

API endpoint: `POST /api/voucher/check`

**Kiểm tra các điều kiện:**

1. ✅ Mã có tồn tại?
2. ✅ Trạng thái đã kích hoạt?
3. ✅ Nằm trong thời gian sử dụng?
4. ✅ Số lượng còn > 0?
5. ✅ Đơn hàng đạt điều kiện tối thiểu?

#### **Bước 4: Hiển Thị Kết Quả**

**Nếu Thành Công:**
```
✓ Áp dụng mã FOOD10 thành công - Giảm 10%!

[Mã: FOOD10 -10%] [X]  ← Hiển thị voucher
```

**Giao diện cập nhật:**
```
Tạm tính          : 500,000 VND
Phí vận chuyển    : Miễn phí
─────────────────────────
Giảm giá          : 50,000 VND  ← Tính tự động
─────────────────────────
Tổng thanh toán   : 450,000 VND ← Cập nhật
```

**Nếu Thất Bại:**
```
❌ Mã giảm giá không tồn tại!

Hoặc:
❌ Mã đã hết hạn!

Hoặc:
❌ Đơn hàng phải từ 100,000 VND để áp dụng mã này!
```

#### **Bước 5: Xóa Voucher**
- Click nút **X** trên badge voucher
- Hoặc xóa nội dung input và bỏ qua

#### **Bước 6: Thanh Toán**
- Click "Thanh Toán" hoặc "PayPal"
- Voucher được lưu khi tạo đơn hàng

---

## 💾 PHẦN 3: THANH TOÁN

### 🔄 Quy Trình

1. **Người dùng áp dụng voucher** (tuỳ chọn)
2. **Nhấn "Thanh Toán"**
3. **Server kiểm tra lại voucher:**
   - Mã có hợp lệ?
   - Còn số lượng?
4. **Lưu vào DonHang:**
   - Cột `MaKhuyenMai` lưu mã đã dùng
5. **Giảm số lượng voucher:**
   - `SoLuong -= 1`
6. **Tạo đơn hàng:**
   - Tất cả chi tiết được lưu
   - Giao diện thanh toán thành công

### 📊 Ví Dụ

**Trước áp dụng:**
```
DonHang {
  MaDh: "order-123",
  TongTien: 500000,
  MaKhuyenMai: null
}
```

**Sau áp dụng FOOD10 (giảm 10%):**
```
DonHang {
  MaDh: "order-123",
  TongTien: 500000,  ← Vẫn lưu tổng gốc
  MaKhuyenMai: "FOOD10"  ← Lưu mã đã dùng
}

KhuyenMai {
  MaKhuyenMai: "FOOD10",
  SoLuong: 99  ← Giảm từ 100 xuống 99
}
```

---

## 🔌 API Endpoints

### 1. Kiểm Tra Voucher

**Endpoint:** `POST /api/voucher/check`

**Request:**
```json
{
  "maKhuyenMai": "FOOD10",
  "tongTien": 500000
}
```

**Response Success:**
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

**Response Error:**
```json
{
  "success": false,
  "message": "Mã giảm giá không tồn tại!"
}
```

### 2. Tính Tiền Giảm

**Endpoint:** `POST /api/voucher/calculate`

**Request:**
```json
{
  "maKhuyenMai": "FOOD10",
  "tongTien": 500000,
  "giaTri": 10
}
```

**Response:**
```json
{
  "success": true,
  "tongTien": 500000,
  "giaTri": 10,
  "tienGiam": 50000,
  "tongSauGiam": 450000,
  "maKhuyenMai": "FOOD10"
}
```

---

## 🛠️ Cách Sử Dụng Từ Code

### **Service**

```csharp
// Kiểm tra voucher
var result = await _voucherService.CheckVoucherAsync("FOOD10", 500000);
if (result.Success)
{
    // Áp dụng thành công
    var tienGiam = _voucherService.CalculateDiscount(500000, result.Data.GiaTri);
    
    // Giảm số lượng
    await _voucherService.DecrementVoucherCountAsync("FOOD10");
}
```

### **Controller**

```csharp
[HttpPost]
public IActionResult Checkout(string voucherCode = null)
{
    if (!string.IsNullOrWhiteSpace(voucherCode))
    {
        var voucher = _context.KhuyenMais
            .FirstOrDefault(km => km.MaKhuyenMai == voucherCode);
        
        if (voucher != null && voucher.TrangThai && voucher.SoLuong > 0)
        {
            // Lưu voucher
            donHang.MaKhuyenMai = voucherCode;
            voucher.SoLuong -= 1;
        }
    }
    
    _context.SaveChanges();
}
```

---

## ✅ Checklist Kiểm Tra

- [ ] VoucherService được đăng ký trong Program.cs
- [ ] VoucherController có endpoint `/api/voucher/check`
- [ ] GioHangsController nhận tham số `voucherCode`
- [ ] View GioHang có phần nhập voucher
- [ ] JavaScript voucher.js được load
- [ ] CSS voucher.css được load
- [ ] Voucher được lưu vào DonHang.MaKhuyenMai
- [ ] SoLuong voucher giảm khi thanh toán thành công

---

## 🐛 Troubleshooting

### **Problem:** API trả về lỗi 404
**Solution:** 
- Kiểm tra endpoint: `/api/voucher/check`
- Kiểm tra VoucherController có tồn tại

### **Problem:** Voucher không được áp dụng
**Solution:**
- Kiểm tra trạng thái: `TrangThai = true`
- Kiểm tra ngày: trong khoảng `ThoiGianBatDau` - `ThoiGianKetThuc`
- Kiểm tra số lượng: `SoLuong > 0`
- Kiểm tra điều kiện: `TongTien >= DieuKienApDung`

### **Problem:** Tiền giảm không tính đúng
**Solution:**
- Công thức: `tienGiam = tongTien * (giaTri / 100)`
- Ví dụ: 500,000 * (10 / 100) = 50,000

---

## 📞 Liên Hệ & Hỗ Trợ

- **Database:** Không cần thay đổi cấu trúc
- **Migration:** Không cần tạo mới
- **Backup:** Sao lưu bảng `KhuyenMai` trước khi thay đổi

---

**Phiên Bản:** 1.0  
**Ngày:** 2024  
**Status:** ✅ Production Ready
