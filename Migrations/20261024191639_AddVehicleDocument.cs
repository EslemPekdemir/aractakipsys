using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PolicyOrStationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleDocuments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDocuments_VehicleId",
                table: "VehicleDocuments",
                column: "VehicleId");

            // Mevcut araçların Inspection ve Insurance verilerini yeni VehicleDocuments tablosuna taşı
            migrationBuilder.Sql(@"
                -- Fenni Muayene kayıtlarını aktar
                INSERT INTO VehicleDocuments (VehicleId, DocumentType, StartDate, EndDate, Cost, PolicyOrStationNumber, CompanyName, Notes, CreatedAt, CreatedBy, IsDeleted)
                SELECT 
                    Id AS VehicleId,
                    'Inspection' AS DocumentType,
                    InspectionStartDate AS StartDate,
                    InspectionExpiryDate AS EndDate,
                    0.00 AS Cost,
                    NULL AS PolicyOrStationNumber,
                    N'TÜVTÜRK' AS CompanyName,
                    N'Sistem aktarımı (Faz 5-A öncesi mevcut kayıt)' AS Notes,
                    GETUTCDATE() AS CreatedAt,
                    'System_Migration' AS CreatedBy,
                    0 AS IsDeleted
                FROM Vehicles
                WHERE IsDeleted = 0 
                  AND InspectionExpiryDate IS NOT NULL;

                -- Trafik Sigortası kayıtlarını aktar
                INSERT INTO VehicleDocuments (VehicleId, DocumentType, StartDate, EndDate, Cost, PolicyOrStationNumber, CompanyName, Notes, CreatedAt, CreatedBy, IsDeleted)
                SELECT 
                    Id AS VehicleId,
                    'Insurance' AS DocumentType,
                    InsuranceStartDate AS StartDate,
                    InsuranceExpiryDate AS EndDate,
                    0.00 AS Cost,
                    NULL AS PolicyOrStationNumber,
                    N'Trafik Sigortası Şirketi' AS CompanyName,
                    N'Sistem aktarımı (Faz 5-A öncesi mevcut kayıt)' AS Notes,
                    GETUTCDATE() AS CreatedAt,
                    'System_Migration' AS CreatedBy,
                    0 AS IsDeleted
                FROM Vehicles
                WHERE IsDeleted = 0 
                  AND InsuranceExpiryDate IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleDocuments");
        }
    }
}
