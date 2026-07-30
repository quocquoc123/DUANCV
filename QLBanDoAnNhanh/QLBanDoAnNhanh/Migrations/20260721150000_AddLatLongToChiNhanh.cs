using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLBanDoAnNhanh.Migrations;

public partial class AddLatLongToChiNhanh : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "Latitude",
            table: "ChiNhanh",
            type: "float",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "Longitude",
            table: "ChiNhanh",
            type: "float",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Latitude", table: "ChiNhanh");
        migrationBuilder.DropColumn(name: "Longitude", table: "ChiNhanh");
    }
}
