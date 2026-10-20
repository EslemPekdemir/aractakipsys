using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aractakipsys.Migrations
{
    /// <inheritdoc />
    public partial class AddPerDiemHybridFieldsToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PerDiemHours",
                table: "Assignments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PerDiemRate",
                table: "Assignments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerDiemType",
                table: "Assignments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Hourly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerDiemHours",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "PerDiemRate",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "PerDiemType",
                table: "Assignments");
        }
    }
}
