USE master;
GO

-- Tạo Cơ Sở Dữ Liệu
CREATE DATABASE QLBanDoAnNhanh3;
GO

USE QLBanDoAnNhanh3;
GO

-- Xóa Cơ Sở Dữ Liệu Nếu Cần Thiết
-- DROP DATABASE QLBanDoAnNha nh;
drop DATABASE QLBanDoAnNhanh3;

-- Tạo Bảng Danh Mục
CREATE TABLE [dbo].[DanhMuc](
    [maDM] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [tenDM] NVARCHAR(100) NOT NULL
);

-- Tạo Bảng Giảm Giá
CREATE TABLE GiamGia
(
    MaGiamGia INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
    GiaTri INT NOT NULL DEFAULT 0,
	ThoiGianBatDau datetime not null default getdate(),
  ThoiGianKetThuc datetime not null default getdate(),
);

-- Tạo Bảng Khuyến Mãi
CREATE TABLE KhuyenMai
(
    MaKhuyenMai varchar(6) NOT NULL PRIMARY KEY,
    GiaTri INT NOT NULL,
    ThoiGianBatDau DATETIME NOT NULL DEFAULT GETDATE(),
    ThoiGianKetThuc DATETIME NOT NULL DEFAULT GETDATE(),
    TrangThai BIT NOT NULL DEFAULT 0,
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
    DieuKienApDung INT DEFAULT 0,
    SoLuong INT NOT NULL DEFAULT 1
);

-- Tạo Bảng Sản Phẩm
CREATE TABLE [dbo].[SanPham](
    [maSP] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [tenSP] NVARCHAR(700) NOT NULL,
    [MaGiamGia] INT NOT NULL,
    [thanhPhan] NVARCHAR(700) NOT NULL,
    [giaTien] FLOAT NOT NULL,
    [donVi] FLOAT NOT NULL,
    [chitietSP] NVARCHAR(1000) NULL,
    [maDM] INT NOT NULL,
    [SLBanTrongNgay] INT NULL,
    [hinhAnh1] NVARCHAR(700) NULL,
    [hinhAnh2] NVARCHAR(700) NULL,
   
    FOREIGN KEY (MaGiamGia) REFERENCES GiamGia(MaGiamGia),
    FOREIGN KEY (maDM) REFERENCES DanhMuc(maDM)
);

-- Tạo Bảng Phân Quyền
CREATE TABLE [dbo].[PhanQuyen](
    [roleID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [roleName] NVARCHAR(20) NOT NULL
);

-- Tạo Bảng Người Dùng
CREATE TABLE [dbo].[NguoiDung](
    [maNguoiDung] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [username] NVARCHAR(200) NOT NULL,
    [trangThai] NVARCHAR(50) NOT NULL,
    [hoTen] NVARCHAR(200) NOT NULL,
    [email] NVARCHAR(200) NOT NULL,
    [sdt] NVARCHAR(200) NOT NULL,
    [matkhau] NVARCHAR(200) NOT NULL,
    [roleID] INT NOT NULL,
    FOREIGN KEY (roleID) REFERENCES PhanQuyen(roleID)
);

-- Tạo Bảng Đơn Hàng
CREATE TABLE [dbo].[DonHang](
    [maDH] NVARCHAR(255) NOT NULL PRIMARY KEY,
    [username] NVARCHAR(200) NOT NULL,
    [diachi] NVARCHAR(700) NOT NULL,
    MaKhuyenMai varchar(6)  NULL,
    [tongTien] FLOAT NOT NULL,
    [soLuong] INT NOT NULL,
    [trangThai] NVARCHAR(700) NOT NULL,
    [createdAt] DATETIME NULL,
    [updatedAt] DATETIME NULL,
    [maNguoiDung] INT,
    FOREIGN KEY (MaKhuyenMai) REFERENCES KhuyenMai(MaKhuyenMai),
    FOREIGN KEY ([maNguoiDung]) REFERENCES [NguoiDung]([maNguoiDung])
);

-- Tạo Bảng Thanh Toán
CREATE TABLE ThanhToan
(
    MaThanhToan INT IDENTITY(1,1) PRIMARY KEY,
    maDH NVARCHAR(255) NOT NULL,
    PhuongThucThanhToan NVARCHAR(50) NOT NULL,
    NgayThanhToan DATETIME DEFAULT GETDATE(),
    TongTien FLOAT NOT NULL,
    TrangThaiThanhToan BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (maDH) REFERENCES DonHang(maDH)
);

-- Tạo Bảng Bình Luận


CREATE TABLE BinhLuan
(
    MaBinhLuan INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    maSP INT NOT NULL,
    MaNguoiDung INT NOT NULL,
    NoiDung NVARCHAR(MAX) NOT NULL,
    NgayBinhLuan DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (maSP) REFERENCES SanPham(maSP),
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(maNguoiDung)
);

-- Tạo Bảng Đánh Giá
CREATE TABLE DanhGia
(
    MaDanhGia INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    MaSanPham INT NOT NULL,
    MaNguoiDung INT NOT NULL,
    SoSao decimal NULL,
     NgayBinhLuan DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(maSP),
    FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(maNguoiDung)
);

-- Tạo Bảng Hình Ảnh
CREATE TABLE [dbo].[HinhAnh](
    [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [urlHinh] NVARCHAR(2000) NOT NULL,
    [maSP] INT NULL,
    FOREIGN KEY (maSP) REFERENCES SanPham(maSP)
);

-- Tạo Bảng Chi Tiết Đơn Hàng
CREATE TABLE [dbo].[ChiTietDonHang](
    [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [maDH] NVARCHAR(255) NOT NULL,
    [maSP] INT NOT NULL,
    [soLuong] INT NOT NULL,
    [tongTien] FLOAT NOT NULL,
    FOREIGN KEY ([maSP]) REFERENCES SanPham(maSP),
    FOREIGN KEY (maDH) REFERENCES DonHang(maDH)
);

-- Tạo Bảng Giỏ Hàng
CREATE TABLE [dbo].[GioHang](
    [maGH] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [maNguoiDung] INT NULL,
    [soLuong] INT NULL,
    FOREIGN KEY (maNguoiDung) REFERENCES NguoiDung(maNguoiDung)
);

-- Tạo Bảng Chi Tiết Giỏ Hàng
CREATE TABLE [dbo].[ChiTietGioHang](
    [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [maGH] INT NULL,
    [soLuongSP] INT NULL,
    [maSP] INT NULL,
    [tongTien] FLOAT NULL,
    FOREIGN KEY (maSP) REFERENCES SanPham(maSP),
    FOREIGN KEY (maGH) REFERENCES GioHang(maGH)
);

-- Tạo Bảng Chi Nhánh
CREATE TABLE [dbo].[ChiNhanh](
    [MaChiNhanh]   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [TenChiNhanh]  NVARCHAR(200)     NOT NULL,
    [DiaChi]       NVARCHAR(500)     NOT NULL,
    [SoDienThoai]  NVARCHAR(20)      NOT NULL,
    [Email]        NVARCHAR(200)     NULL,
    [GioMoCua]     NVARCHAR(10)      NOT NULL DEFAULT '07:00',
    [GioDongCua]   NVARCHAR(10)      NOT NULL DEFAULT '22:00',
    [Latitude]     FLOAT             NULL,
    [Longitude]    FLOAT             NULL,
    [HinhAnh]      NVARCHAR(500)     NULL,
    [TrangThai]    BIT               NOT NULL DEFAULT 1
);