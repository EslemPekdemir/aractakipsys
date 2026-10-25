using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleTires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Size = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DotDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ProductionYear = table.Column<int>(type: "int", nullable: true),
                    Season = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InstalledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstalledKm = table.Column<int>(type: "int", nullable: true),
                    RecommendedReplaceKm = table.Column<int>(type: "int", nullable: true),
                    RecommendedReplaceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleTires_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTires_VehicleId_IsDeleted",
                table: "VehicleTires",
                columns: new[] { "VehicleId", "IsDeleted" });

            // MEVCUT VERİYİ TAŞI (Eski veride olmayan ebat, dot, takılma tarihi, takılma km NULL bırakılır)
            migrationBuilder.Sql(@"
                INSERT INTO VehicleTires (
                    VehicleId, Brand, Size, DotDate, ProductionYear, Season, 
                    InstalledDate, InstalledKm, RecommendedReplaceKm, RecommendedReplaceDate, 
                    Cost, Notes, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
                )
                SELECT 
                    Id AS VehicleId,
                    ISNULL(NULLIF(LTRIM(RTRIM(TireBrand)), ''), N'Bilinmiyor') AS Brand,
                    NULL AS Size,
                    NULL AS DotDate,
                    TireYear AS ProductionYear,
                    ISNULL(NULLIF(LTRIM(RTRIM(TireSeason)), ''), N'Dört Mevsim') AS Season,
                    NULL AS InstalledDate,
                    NULL AS InstalledKm,
                    NULL AS RecommendedReplaceKm,
                    NULL AS RecommendedReplaceDate,
                    NULL AS Cost,
                    N'Araç mevcut verisinden taşınan ilk lastik kaydı' AS Notes,
                    GETUTCDATE() AS CreatedAt,
                    ISNULL(CreatedBy, N'System') AS CreatedBy,
                    NULL AS UpdatedAt,
                    NULL AS UpdatedBy,
                    0 AS IsDeleted
                FROM Vehicles
                WHERE IsDeleted = 0 AND TireBrand IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleTires");
        }
    }
}
