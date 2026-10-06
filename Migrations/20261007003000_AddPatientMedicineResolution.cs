using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations
{
    public partial class AddPatientMedicineResolution : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResolutionDocumentId",
                table: "PatientMedicines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicines_ResolutionDocumentId",
                table: "PatientMedicines",
                column: "ResolutionDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedicines_PatientDocuments_ResolutionDocumentId",
                table: "PatientMedicines",
                column: "ResolutionDocumentId",
                principalTable: "PatientDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedicines_PatientDocuments_ResolutionDocumentId",
                table: "PatientMedicines");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedicines_ResolutionDocumentId",
                table: "PatientMedicines");

            migrationBuilder.DropColumn(
                name: "ResolutionDocumentId",
                table: "PatientMedicines");
        }
    }
}
