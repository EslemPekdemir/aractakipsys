using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleDelegationAndAuditOnBehalf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DelegationId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnBehalfOfUserId",
                table: "AuditLogs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoleDelegations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DelegatorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DelegateeUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualRevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RevokeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalDelegateeRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DelegatedRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleDelegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleDelegations_AspNetUsers_DelegateeUserId",
                        column: x => x.DelegateeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleDelegations_AspNetUsers_DelegatorUserId",
                        column: x => x.DelegatorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleDelegations_AspNetUsers_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DelegationId",
                table: "AuditLogs",
                column: "DelegationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OnBehalfOfUserId",
                table: "AuditLogs",
                column: "OnBehalfOfUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleDelegations_ActiveCheck",
                table: "RoleDelegations",
                columns: new[] { "DelegateeUserId", "StartDate", "EndDate", "ActualRevokedAt", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleDelegations_DelegatorUserId",
                table: "RoleDelegations",
                column: "DelegatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleDelegations_RevokedByUserId",
                table: "RoleDelegations",
                column: "RevokedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_OnBehalfOfUserId",
                table: "AuditLogs",
                column: "OnBehalfOfUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_RoleDelegations_DelegationId",
                table: "AuditLogs",
                column: "DelegationId",
                principalTable: "RoleDelegations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_OnBehalfOfUserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_RoleDelegations_DelegationId",
                table: "AuditLogs");

            migrationBuilder.DropTable(
                name: "RoleDelegations");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_DelegationId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_OnBehalfOfUserId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DelegationId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OnBehalfOfUserId",
                table: "AuditLogs");
        }
    }
}
