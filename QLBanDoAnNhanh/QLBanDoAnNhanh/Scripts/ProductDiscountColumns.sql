IF COL_LENGTH('dbo.SanPham', 'DiscountPercent') IS NULL
BEGIN
    ALTER TABLE dbo.SanPham ADD DiscountPercent DECIMAL(5,2) NULL;
END;

IF COL_LENGTH('dbo.SanPham', 'DiscountPrice') IS NULL
BEGIN
    ALTER TABLE dbo.SanPham ADD DiscountPrice DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.SanPham', 'DiscountStartDate') IS NULL
BEGIN
    ALTER TABLE dbo.SanPham ADD DiscountStartDate DATETIME NULL;
END;

IF COL_LENGTH('dbo.SanPham', 'DiscountEndDate') IS NULL
BEGIN
    ALTER TABLE dbo.SanPham ADD DiscountEndDate DATETIME NULL;
END;

IF COL_LENGTH('dbo.SanPham', 'IsDiscount') IS NULL
BEGIN
    ALTER TABLE dbo.SanPham
    ADD IsDiscount BIT NOT NULL
        CONSTRAINT DF_SanPham_IsDiscount DEFAULT (0);
END;

UPDATE dbo.SanPham
SET DiscountPrice = giaTien - (giaTien * DiscountPercent / 100.0)
WHERE IsDiscount = 1
  AND DiscountPercent IS NOT NULL
  AND DiscountPercent > 0
  AND DiscountPrice IS NULL;
