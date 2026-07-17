-- =============================================
-- Script tạo bảng SanPhamYeuThich (Wishlist)
-- Chạy script này trên SQL Server để tạo bảng
-- =============================================

USE [QLBanDoAnNhanh3];
GO

-- Tạo bảng SanPhamYeuThich nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SanPhamYeuThich]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SanPhamYeuThich] (
        [WishlistId]   INT          NOT NULL IDENTITY(1,1),
        [MaNguoiDung]  INT          NOT NULL,
        [MaSp]         INT          NOT NULL,
        [NgayThem]     DATETIME     NOT NULL DEFAULT GETDATE(),

        CONSTRAINT [PK__SanPhamYeuThich__WishlistId] PRIMARY KEY CLUSTERED ([WishlistId] ASC),

        CONSTRAINT [FK__SanPhamYeuThich__NguoiDung] FOREIGN KEY ([MaNguoiDung])
            REFERENCES [dbo].[NguoiDung] ([maNguoiDung])
            ON DELETE CASCADE
            ON UPDATE CASCADE,

        CONSTRAINT [FK__SanPhamYeuThich__SanPham] FOREIGN KEY ([MaSp])
            REFERENCES [dbo].[SanPham] ([maSP])
            ON DELETE CASCADE
            ON UPDATE CASCADE,

        -- Mỗi user chỉ được yêu thích 1 sản phẩm 1 lần
        CONSTRAINT [UQ__SanPhamYeuThich__UserProduct] UNIQUE ([MaNguoiDung], [MaSp])
    );

    PRINT N'Bảng SanPhamYeuThich đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT N'Bảng SanPhamYeuThich đã tồn tại, bỏ qua.';
END
GO
