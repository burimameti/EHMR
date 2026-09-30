using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmaceuticalReferenceToPatientMedicines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PharmaceuticalReference",
                table: "PatientMedicines",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PharmaceuticalReference",
                table: "PatientMedicines");
        }
    }
}