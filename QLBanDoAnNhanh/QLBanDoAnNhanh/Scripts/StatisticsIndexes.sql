-- Optional indexes for statistics APIs.
-- Run on SQL Server if the DonHang/ChiTietDonHang/ThanhToan tables grow large.

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_DonHang_CreatedAt_TrangThai'
      AND object_id = OBJECT_ID('dbo.DonHang')
)
BEGIN
    CREATE INDEX IX_DonHang_CreatedAt_TrangThai
    ON dbo.DonHang (createdAt, trangThai)
    INCLUDE (maDH, maNguoiDung, username, tongTien, MaKhuyenMai);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ChiTietDonHang_MaDH_MaSP'
      AND object_id = OBJECT_ID('dbo.ChiTietDonHang')
)
BEGIN
    CREATE INDEX IX_ChiTietDonHang_MaDH_MaSP
    ON dbo.ChiTietDonHang (maDH, maSP)
    INCLUDE (soLuong, tongTien);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ThanhToan_NgayThanhToan_Status_Method'
      AND object_id = OBJECT_ID('dbo.ThanhToan')
)
BEGIN
    CREATE INDEX IX_ThanhToan_NgayThanhToan_Status_Method
    ON dbo.ThanhToan (NgayThanhToan, TrangThaiThanhToan, PhuongThucThanhToan)
    INCLUDE (maDH, TongTien, PaymentMethod, PaymentStatus, PaidAt);
END;
