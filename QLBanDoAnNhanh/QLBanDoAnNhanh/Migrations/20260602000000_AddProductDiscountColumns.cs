using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLBanDoAnNhanh.Migrations;

public partial class AddProductDiscountColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "DiscountPercent",
            table: "SanPham",
            type: "decimal(5,2)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "DiscountPrice",
            table: "SanPham",
            type: "decimal(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DiscountStartDate",
            table: "SanPham",
            type: "datetime",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DiscountEndDate",
            table: "SanPham",
            type: "datetime",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsDiscount",
            table: "SanPham",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DiscountPercent", table: "SanPham");
        migrationBuilder.DropColumn(name: "DiscountPrice", table: "SanPham");
        migrationBuilder.DropColumn(name: "DiscountStartDate", table: "SanPham");
        migrationBuilder.DropColumn(name: "DiscountEndDate", table: "SanPham");
        migrationBuilder.DropColumn(name: "IsDiscount", table: "SanPham");
    }
}
