using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiDriverAndDestinationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DriverId",
                table: "Assignments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "Assignments",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalDriverDepartment",
                table: "Assignments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalDriverFullName",
                table: "Assignments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDriverless",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsExternalDriver",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Route",
                table: "Assignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssignmentDrivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentDrivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentDrivers_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssignmentDrivers_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentDrivers_AssignmentId_DriverId",
                table: "AssignmentDrivers",
                columns: new[] { "AssignmentId", "DriverId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentDrivers_DriverId",
                table: "AssignmentDrivers",
                column: "DriverId");

            // Veri Aktarımı: Mevcut tüm görevlerin şoförlerini (silinmişler dahil) AssignmentDrivers ara tablosuna IsPrimary=true olarak aktar
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
INSERT INTO AssignmentDrivers (AssignmentId, DriverId, IsPrimary, CreatedAt, CreatedBy, IsDeleted)
SELECT Id, DriverId, 1, CreatedAt, ISNULL(CreatedBy, 'System'), IsDeleted
FROM Assignments
WHERE DriverId IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentDrivers");

            migrationBuilder.DropColumn(
                name: "Destination",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ExternalDriverDepartment",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ExternalDriverFullName",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "IsDriverless",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "IsExternalDriver",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Route",
                table: "Assignments");

            migrationBuilder.AlterColumn<int>(
                name: "DriverId",
                table: "Assignments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
