using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFuelTypeToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FuelType",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Mevcut veritabanındaki değerleri Türkçe metin olarak normalize et
            migrationBuilder.Sql("UPDATE Vehicles SET FuelType = 'Benzin' WHERE FuelType = '0' OR FuelType = 'Gasoline'");
            migrationBuilder.Sql("UPDATE Vehicles SET FuelType = 'Dizel' WHERE FuelType = '1' OR FuelType = 'Diesel'");
            migrationBuilder.Sql("UPDATE Vehicles SET FuelType = 'LPG' WHERE FuelType = '2' OR FuelType = 'Lpg'");
            migrationBuilder.Sql("UPDATE Vehicles SET FuelType = 'Elektrik' WHERE FuelType = '3' OR FuelType = 'Electric'");
            migrationBuilder.Sql("UPDATE Vehicles SET FuelType = 'Hibrit' WHERE FuelType = '4' OR FuelType = 'Hybrid'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FuelType",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
