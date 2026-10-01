using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations;

[Migration("20261001010000_ChangePatientMedicineQuantityToInt")]
public partial class ChangePatientMedicineQuantityToInt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "Quantity",
            table: "PatientMedicines",
            type: "int",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,4)",
            oldPrecision: 18,
            oldScale: 4);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "Quantity",
            table: "PatientMedicines",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int");
    }
}
