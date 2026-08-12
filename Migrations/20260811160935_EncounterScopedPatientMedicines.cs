using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations
{
    /// <inheritdoc />
    public partial class EncounterScopedPatientMedicines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EncounterId",
                table: "PatientMedicines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicines_EncounterId",
                table: "PatientMedicines",
                column: "EncounterId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedicines_Encounters_EncounterId",
                table: "PatientMedicines",
                column: "EncounterId",
                principalTable: "Encounters",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedicines_Encounters_EncounterId",
                table: "PatientMedicines");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedicines_EncounterId",
                table: "PatientMedicines");

            migrationBuilder.DropColumn(
                name: "EncounterId",
                table: "PatientMedicines");
        }
    }
}
