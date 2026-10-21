using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase3AssignmentChecksAndFaultReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CheckFuel",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CheckHydraulic",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CheckOil",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CheckTires",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CheckWater",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChecksCompletedAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OffRouteDispatcherName",
                table: "Assignments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OffRouteDispatcherTitle",
                table: "Assignments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OffRouteReason",
                table: "Assignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OffRouteReportedAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OffRouteReportedByUserId",
                table: "Assignments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VehicleFaultReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: true),
                    AssignmentId = table.Column<int>(type: "int", nullable: true),
                    CurrentKm = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "Pending"),
                    ReviewerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewerNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleFaultReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleFaultReports_AspNetUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleFaultReports_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleFaultReports_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleFaultReports_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_OffRouteReportedByUserId",
                table: "Assignments",
                column: "OffRouteReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleFaultReports_AssignmentId",
                table: "VehicleFaultReports",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleFaultReports_DriverId",
                table: "VehicleFaultReports",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleFaultReports_ReportDate",
                table: "VehicleFaultReports",
                column: "ReportDate");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleFaultReports_ReviewerUserId",
                table: "VehicleFaultReports",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleFaultReports_VehicleId_Status",
                table: "VehicleFaultReports",
                columns: new[] { "VehicleId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_OffRouteReportedByUserId",
                table: "Assignments",
                column: "OffRouteReportedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_OffRouteReportedByUserId",
                table: "Assignments");

            migrationBuilder.DropTable(
                name: "VehicleFaultReports");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_OffRouteReportedByUserId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CheckFuel",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CheckHydraulic",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CheckOil",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CheckTires",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CheckWater",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ChecksCompletedAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "OffRouteDispatcherName",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "OffRouteDispatcherTitle",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "OffRouteReason",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "OffRouteReportedAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "OffRouteReportedByUserId",
                table: "Assignments");
        }
    }
}
