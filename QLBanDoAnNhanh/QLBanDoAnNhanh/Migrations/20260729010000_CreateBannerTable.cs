using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLBanDoAnNhanh.Migrations
{
    /// <inheritdoc />
    public partial class CreateBannerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('Banner', 'U') IS NULL
CREATE TABLE Banner (
  MaBanner INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  TieuDe NVARCHAR(200) NOT NULL,
  HinhAnh NVARCHAR(500) NULL,
  ViTri NVARCHAR(20) NOT NULL CONSTRAINT DF_Banner_ViTri DEFAULT N'Left',
  MaDm INT NULL,
  ThuTu INT NOT NULL CONSTRAINT DF_Banner_ThuTu DEFAULT 0,
  TrangThai BIT NOT NULL CONSTRAINT DF_Banner_TrangThai DEFAULT 1,
  NgayCapNhat DATETIME NULL,
  CONSTRAINT FK_Banner_DanhMuc FOREIGN KEY (MaDm) REFERENCES DanhMuc(maDM) ON DELETE SET NULL
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('Banner', 'U') IS NOT NULL DROP TABLE Banner;");
        }
    }
}
