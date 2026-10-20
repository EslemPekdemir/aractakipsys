using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddAccommodationAndPerDiemToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AccommodationCost",
                table: "Assignments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccommodationNights",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpenseNotes",
                table: "Assignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PerDiemAmount",
                table: "Assignments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PerDiemDays",
                table: "Assignments",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationCost",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "AccommodationNights",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ExpenseNotes",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "PerDiemAmount",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "PerDiemDays",
                table: "Assignments");
        }
    }
}
