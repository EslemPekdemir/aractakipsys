using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddSignedDocumentFieldsToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignedDocumentOriginalFileName",
                table: "Assignments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedDocumentPath",
                table: "Assignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedDocumentUploadedAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedDocumentUploadedByUserId",
                table: "Assignments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_SignedDocumentUploadedByUserId",
                table: "Assignments",
                column: "SignedDocumentUploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AspNetUsers_SignedDocumentUploadedByUserId",
                table: "Assignments",
                column: "SignedDocumentUploadedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AspNetUsers_SignedDocumentUploadedByUserId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_SignedDocumentUploadedByUserId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "SignedDocumentOriginalFileName",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "SignedDocumentPath",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "SignedDocumentUploadedAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "SignedDocumentUploadedByUserId",
                table: "Assignments");
        }
    }
}
