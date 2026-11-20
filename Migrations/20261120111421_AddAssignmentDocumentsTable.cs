using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentDocumentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssignmentDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentDocuments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssignmentDocuments_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssignmentDocuments_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentDocuments_AssignmentId",
                table: "AssignmentDocuments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentDocuments_DriverId",
                table: "AssignmentDocuments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentDocuments_UploadedAt",
                table: "AssignmentDocuments",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentDocuments_UploadedByUserId",
                table: "AssignmentDocuments",
                column: "UploadedByUserId");

            // Mevcut tekil imzalı belgeleri veri kaybı olmadan yeni AssignmentDocuments tablosuna aktar
            migrationBuilder.Sql(@"
                INSERT INTO [AssignmentDocuments] (
                    [AssignmentId],
                    [DriverId],
                    [FilePath],
                    [OriginalFileName],
                    [FileSizeBytes],
                    [Description],
                    [UploadedAt],
                    [UploadedByUserId],
                    [CreatedAt],
                    [CreatedBy],
                    [UpdatedAt],
                    [UpdatedBy],
                    [IsDeleted]
                )
                SELECT
                    a.[Id],
                    NULL,
                    a.[SignedDocumentPath],
                    COALESCE(NULLIF(LTRIM(RTRIM(a.[SignedDocumentOriginalFileName])), N''), N'Imzali_Belge.pdf'),
                    CAST(0 AS bigint),
                    NULL,
                    COALESCE(a.[SignedDocumentUploadedAt], a.[UpdatedAt], a.[CreatedAt], GETUTCDATE()),
                    a.[SignedDocumentUploadedByUserId],
                    COALESCE(a.[SignedDocumentUploadedAt], a.[CreatedAt], GETUTCDATE()),
                    COALESCE(a.[SignedDocumentUploadedByUserId], a.[CreatedBy], N'SystemMigration'),
                    NULL,
                    NULL,
                    CAST(0 AS bit)
                FROM [Assignments] a
                WHERE a.[SignedDocumentPath] IS NOT NULL
                  AND LTRIM(RTRIM(a.[SignedDocumentPath])) <> N''
                  AND NOT EXISTS (
                      SELECT 1 FROM [AssignmentDocuments] d
                      WHERE d.[AssignmentId] = a.[Id] AND d.[FilePath] = a.[SignedDocumentPath]
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentDocuments");
        }
    }
}
