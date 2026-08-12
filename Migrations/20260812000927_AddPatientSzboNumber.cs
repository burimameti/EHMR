using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientSzboNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SzboNumber",
                table: "Patients",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                ;WITH NumberedPatients AS
                (
                    SELECT Id, ROW_NUMBER() OVER (ORDER BY RegistrationDate, Id) AS RowNumber
                    FROM Patients
                    WHERE SzboNumber = ''
                )
                UPDATE p
                SET SzboNumber = 'SZBO-' + RIGHT('000000' + CAST(n.RowNumber AS varchar(6)), 6)
                FROM Patients p
                INNER JOIN NumberedPatients n ON n.Id = p.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_SzboNumber",
                table: "Patients",
                column: "SzboNumber",
                unique: true,
                filter: "[SzboNumber] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_SzboNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "SzboNumber",
                table: "Patients");
        }
    }
}
