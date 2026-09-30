using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations;

[Migration("20260929230000_AddPharmaceuticalReferenceToPatientMedicine")]
public partial class AddPharmaceuticalReferenceToPatientMedicine : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PharmaceuticalReference",
            table: "PatientMedicines",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PharmaceuticalReference",
            table: "PatientMedicines");
    }
}