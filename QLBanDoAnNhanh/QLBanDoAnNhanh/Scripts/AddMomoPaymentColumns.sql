-- MoMo payment columns for ThanhToan table
IF COL_LENGTH('ThanhToan', 'PaymentMethod') IS NULL
    ALTER TABLE ThanhToan ADD PaymentMethod NVARCHAR(50) NULL;

IF COL_LENGTH('ThanhToan', 'PaymentStatus') IS NULL
    ALTER TABLE ThanhToan ADD PaymentStatus NVARCHAR(20) NULL;

IF COL_LENGTH('ThanhToan', 'TransactionId') IS NULL
    ALTER TABLE ThanhToan ADD TransactionId NVARCHAR(100) NULL;

IF COL_LENGTH('ThanhToan', 'PaidAt') IS NULL
    ALTER TABLE ThanhToan ADD PaidAt DATETIME NULL;

IF COL_LENGTH('ThanhToan', 'QrCodeUrl') IS NULL
    ALTER TABLE ThanhToan ADD QrCodeUrl NVARCHAR(500) NULL;

IF COL_LENGTH('ThanhToan', 'PaymentExpiresAt') IS NULL
    ALTER TABLE ThanhToan ADD PaymentExpiresAt DATETIME NULL;

IF COL_LENGTH('ThanhToan', 'MomoRequestId') IS NULL
    ALTER TABLE ThanhToan ADD MomoRequestId NVARCHAR(100) NULL;

IF COL_LENGTH('ThanhToan', 'MomoPayUrl') IS NULL
    ALTER TABLE ThanhToan ADD MomoPayUrl NVARCHAR(1000) NULL;

GO
