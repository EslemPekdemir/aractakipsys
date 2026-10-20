using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterFullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequesterEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequesterPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "Pending"),
                    FulfilledAssignmentId = table.Column<int>(type: "int", nullable: true),
                    FulfilledByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleRequests_AspNetUsers_FulfilledByUserId",
                        column: x => x.FulfilledByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleRequests_Assignments_FulfilledAssignmentId",
                        column: x => x.FulfilledAssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRequests_CreatedAt",
                table: "VehicleRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRequests_FulfilledAssignmentId",
                table: "VehicleRequests",
                column: "FulfilledAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRequests_FulfilledByUserId",
                table: "VehicleRequests",
                column: "FulfilledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRequests_RequesterEmail",
                table: "VehicleRequests",
                column: "RequesterEmail");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRequests_Status",
                table: "VehicleRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleRequests");
        }
    }
}
