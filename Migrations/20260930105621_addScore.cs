using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations
{
    /// <inheritdoc />
    public partial class addScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DecisionText",
                table: "TherapyCycles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InactiveReason",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationRegimeId",
                table: "PatientMedicines",
                type: "uniqueidentifier",
                nullable: true);


            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "PatientMedicines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<Guid>(
                name: "TherapyCycleId",
                table: "PatientDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationRegimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Regime = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRegimes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientScores_Encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientScores_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicines_ApplicationRegimeId",
                table: "PatientMedicines",
                column: "ApplicationRegimeId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_TherapyCycleId",
                table: "PatientDocuments",
                column: "TherapyCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRegimes_Regime",
                table: "ApplicationRegimes",
                column: "Regime",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientScores_EncounterId",
                table: "PatientScores",
                column: "EncounterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientScores_PatientId_RecordedAt",
                table: "PatientScores",
                columns: new[] { "PatientId", "RecordedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_PatientDocuments_TherapyCycles_TherapyCycleId",
                table: "PatientDocuments",
                column: "TherapyCycleId",
                principalTable: "TherapyCycles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedicines_ApplicationRegimes_ApplicationRegimeId",
                table: "PatientMedicines",
                column: "ApplicationRegimeId",
                principalTable: "ApplicationRegimes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientDocuments_TherapyCycles_TherapyCycleId",
                table: "PatientDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedicines_ApplicationRegimes_ApplicationRegimeId",
                table: "PatientMedicines");

            migrationBuilder.DropTable(
                name: "ApplicationRegimes");

            migrationBuilder.DropTable(
                name: "PatientScores");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedicines_ApplicationRegimeId",
                table: "PatientMedicines");

            migrationBuilder.DropIndex(
                name: "IX_PatientDocuments_TherapyCycleId",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "DecisionText",
                table: "TherapyCycles");

            migrationBuilder.DropColumn(
                name: "InactiveReason",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ApplicationRegimeId",
                table: "PatientMedicines");

          

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "PatientMedicines");

            migrationBuilder.DropColumn(
                name: "TherapyCycleId",
                table: "PatientDocuments");
        }
    }
}
