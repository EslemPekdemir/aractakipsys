using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleTypeAndTireSeasonToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VehicleType",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TireSeason",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Mevcut verileri Türkçe metinlere dönüştür
            migrationBuilder.Sql("UPDATE Vehicles SET TireSeason = 'Yazlık' WHERE TireSeason = '1' OR TireSeason = 'Summer'");
            migrationBuilder.Sql("UPDATE Vehicles SET TireSeason = 'Kışlık' WHERE TireSeason = '2' OR TireSeason = 'Winter'");
            migrationBuilder.Sql("UPDATE Vehicles SET TireSeason = '4 Mevsim' WHERE TireSeason = '3' OR TireSeason = 'AllSeason'");
            migrationBuilder.Sql("UPDATE Vehicles SET VehicleType = 'Sedan' WHERE VehicleType = '0'");
            migrationBuilder.Sql("UPDATE Vehicles SET VehicleType = 'Minibüs' WHERE VehicleType = '1' OR VehicleType = 'Minibus'");
            migrationBuilder.Sql("UPDATE Vehicles SET VehicleType = 'Panelvan' WHERE VehicleType = '2'");
            migrationBuilder.Sql("UPDATE Vehicles SET VehicleType = 'Kamyonet' WHERE VehicleType = '3'");
            migrationBuilder.Sql("UPDATE Vehicles SET VehicleType = 'Otobüs' WHERE VehicleType = '4' OR VehicleType = 'Otobus'");
            migrationBuilder.Sql("UPDATE Vehicles SET VehicleType = 'Diğer' WHERE VehicleType = '5' OR VehicleType = 'Diger'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VehicleType",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "TireSeason",
                table: "Vehicles",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
