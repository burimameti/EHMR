using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations;

[DbContext(typeof(DesktopTherapyDbContext))]
[Migration("20261011003000_AddPatientMedicineDosingFrequency")]
public partial class AddPatientMedicineDosingFrequency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DosingFrequency",
            table: "PatientMedicines",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Месечно");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DosingFrequency",
            table: "PatientMedicines");
    }
}
