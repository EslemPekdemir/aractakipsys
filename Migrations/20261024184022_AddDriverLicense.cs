using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverLicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverLicenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    LicenseClass = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LicenseExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverLicenses_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverLicenses_DriverId",
                table: "DriverLicenses",
                column: "DriverId");

            migrationBuilder.Sql(@"
                INSERT INTO DriverLicenses (DriverId, LicenseClass, LicenseExpiryDate, CreatedAt, CreatedBy, IsDeleted)
                SELECT Id, ISNULL(LicenseClass, 'B'), ISNULL(LicenseExpiryDate, DATEADD(year, 5, GETDATE())), GETUTCDATE(), 'System_Migration', 0
                FROM Drivers
                WHERE IsDeleted = 0 AND (LicenseClass IS NOT NULL OR LicenseExpiryDate IS NOT NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverLicenses");
        }
    }
}
