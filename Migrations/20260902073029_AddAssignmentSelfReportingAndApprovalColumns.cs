using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentSelfReportingAndApprovalColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndApprovedByUserId",
                table: "Assignments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EndReportedBySelf",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StartApprovedByUserId",
                table: "Assignments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StartReportedBySelf",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_EndApprovedByUserId",
                table: "Assignments",
                column: "EndApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_StartApprovedByUserId",
                table: "Assignments",
                column: "StartApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_EndApprovedByUserId",
                table: "Assignments",
                column: "EndApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_StartApprovedByUserId",
                table: "Assignments",
                column: "StartApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_EndApprovedByUserId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_StartApprovedByUserId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_EndApprovedByUserId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_StartApprovedByUserId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "EndApprovedByUserId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "EndReportedBySelf",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "StartApprovedByUserId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "StartReportedBySelf",
                table: "Assignments");
        }
    }
}
