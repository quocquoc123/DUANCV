-- =============================================
-- Script tạo bảng ChiNhanh
-- Chạy trên database: QLBanDoAnNhanh3
-- =============================================

USE QLBanDoAnNhanh3;
GO

-- Tạo bảng ChiNhanh nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ChiNhanh' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[ChiNhanh] (
        [MaChiNhanh]   INT IDENTITY(1,1) NOT NULL,
        [TenChiNhanh]  NVARCHAR(200)     NOT NULL,
        [DiaChi]       NVARCHAR(500)     NOT NULL,
        [SoDienThoai]  NVARCHAR(20)      NOT NULL,
        [Email]        NVARCHAR(200)     NULL,
        [GioMoCua]     NVARCHAR(10)      NOT NULL DEFAULT N'07:00',
        [GioDongCua]   NVARCHAR(10)      NOT NULL DEFAULT N'22:00',
        [Latitude]     FLOAT             NULL,
        [Longitude]    FLOAT             NULL,
        [HinhAnh]      NVARCHAR(500)     NULL,
        [TrangThai]    BIT               NOT NULL DEFAULT 1,
        CONSTRAINT [PK__ChiNhanh__MaChiNhanh] PRIMARY KEY ([MaChiNhanh])
    );

    PRINT N'Bảng ChiNhanh đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT N'Bảng ChiNhanh đã tồn tại.';
END
GO

-- Thêm dữ liệu mẫu
IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[ChiNhanh])
BEGIN
    INSERT INTO [dbo].[ChiNhanh] ([TenChiNhanh], [DiaChi], [SoDienThoai], [Email], [GioMoCua], [GioDongCua], [Latitude], [Longitude], [HinhAnh], [TrangThai])
    VALUES
        (N'Food Fast – Quận 1', N'123 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP.HCM', N'028 3822 1234', N'q1@foodfast.vn', N'07:00', N'22:00', 10.7769, 106.7009, NULL, 1),
        (N'Food Fast – Quận 3', N'45 Võ Văn Tần, Phường 6, Quận 3, TP.HCM', N'028 3930 5678', N'q3@foodfast.vn', N'07:30', N'21:30', 10.7756, 106.6856, NULL, 1),
        (N'Food Fast – Bình Thạnh', N'289 Xô Viết Nghệ Tĩnh, Phường 25, Bình Thạnh, TP.HCM', N'028 3512 9876', N'binhThanh@foodfast.vn', N'08:00', N'22:00', 10.8043, 106.7145, NULL, 1),
        (N'Food Fast – Thủ Đức', N'78 Võ Văn Ngân, Phường Bình Thọ, TP. Thủ Đức, TP.HCM', N'028 3722 4321', N'thuduc@foodfast.vn', N'07:00', N'21:00', 10.8501, 106.7717, NULL, 1),
        (N'Food Fast – Gò Vấp', N'156 Nguyễn Văn Nghi, Phường 7, Gò Vấp, TP.HCM', N'028 3894 6789', N'govap@foodfast.vn', N'07:00', N'22:30', 10.8395, 106.6728, NULL, 1);

    PRINT N'Dữ liệu mẫu đã được thêm.';
END
GO
