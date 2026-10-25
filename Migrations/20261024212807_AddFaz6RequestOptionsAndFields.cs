using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddFaz6RequestOptionsAndFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "VehicleRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactFullName",
                table: "VehicleRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "VehicleRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactUserId",
                table: "VehicleRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PassengerCount",
                table: "VehicleRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedEndTime",
                table: "VehicleRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedStartTime",
                table: "VehicleRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedVehicleType",
                table: "VehicleRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequestDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestDepartments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestVehicleTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestVehicleTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRequests_ContactUserId",
                table: "VehicleRequests",
                column: "ContactUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleRequests_AspNetUsers_ContactUserId",
                table: "VehicleRequests",
                column: "ContactUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleRequests_AspNetUsers_ContactUserId",
                table: "VehicleRequests");

            migrationBuilder.DropTable(
                name: "RequestDepartments");

            migrationBuilder.DropTable(
                name: "RequestVehicleTypes");

            migrationBuilder.DropIndex(
                name: "IX_VehicleRequests_ContactUserId",
                table: "VehicleRequests");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "VehicleRequests");

            migrationBuilder.DropColumn(
                name: "ContactFullName",
                table: "VehicleRequests");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "VehicleRequests");

            migrationBuilder.DropColumn(
                name: "ContactUserId",
                table: "VehicleRequests");

            migrationBuilder.DropColumn(
                name: "PassengerCount",
                table: "VehicleRequests");

            migrationBuilder.DropColumn(
                name: "RequestedEndTime",
                table: "VehicleRequests");

            migrationBuilder.DropColumn(
                name: "RequestedStartTime",
                table: "VehicleRequests");

            migrationBuilder.DropColumn(
                name: "RequestedVehicleType",
                table: "VehicleRequests");
        }
    }
}
