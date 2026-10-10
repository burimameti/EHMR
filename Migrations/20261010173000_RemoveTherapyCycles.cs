using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHMR.Migrations;

[DbContext(typeof(DesktopTherapyDbContext))]
[Migration("20261010173000_RemoveTherapyCycles")]
public sealed class RemoveTherapyCycles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop every foreign key that points at TherapyCycles or uses a
        // TherapyCycleId column. This handles databases created from older
        // snapshots where constraint names may differ.
        migrationBuilder.Sql("""
            DECLARE @sql nvarchar(max) = N'';

            SELECT @sql += N'ALTER TABLE '
                + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.'
                + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
                + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
            FROM sys.foreign_keys fk
            WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.TherapyCycles')
               OR EXISTS (
                    SELECT 1
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns c
                      ON c.object_id = fkc.parent_object_id
                     AND c.column_id = fkc.parent_column_id
                    WHERE fkc.constraint_object_id = fk.object_id
                      AND c.name = N'TherapyCycleId'
               );

            IF LEN(@sql) > 0 EXEC sp_executesql @sql;
            """);

        migrationBuilder.Sql("""
            DECLARE @sql nvarchar(max) = N'';

            SELECT @sql += N'DROP INDEX ' + QUOTENAME(i.name) + N' ON '
                + QUOTENAME(OBJECT_SCHEMA_NAME(i.object_id)) + N'.'
                + QUOTENAME(OBJECT_NAME(i.object_id)) + N';'
            FROM sys.indexes i
            JOIN sys.index_columns ic
              ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c
              ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE c.name = N'TherapyCycleId'
              AND i.name IS NOT NULL
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0;

            IF LEN(@sql) > 0 EXEC sp_executesql @sql;
            """);

        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Appointments', N'TherapyCycleId') IS NOT NULL
                ALTER TABLE dbo.Appointments DROP COLUMN TherapyCycleId;
            IF COL_LENGTH(N'dbo.Encounters', N'TherapyCycleId') IS NOT NULL
                ALTER TABLE dbo.Encounters DROP COLUMN TherapyCycleId;
            IF COL_LENGTH(N'dbo.PatientDocuments', N'TherapyCycleId') IS NOT NULL
                ALTER TABLE dbo.PatientDocuments DROP COLUMN TherapyCycleId;
            IF OBJECT_ID(N'dbo.TherapyCycles', N'U') IS NOT NULL
                DROP TABLE dbo.TherapyCycles;
            IF COL_LENGTH(N'dbo.AuditLogs', N'CycleId') IS NOT NULL
                ALTER TABLE dbo.AuditLogs DROP COLUMN CycleId;
            IF OBJECT_ID(N'dbo.UserModules', N'U') IS NOT NULL
                DELETE FROM dbo.UserModules WHERE ModuleKey = N'Therapy';
            IF OBJECT_ID(N'dbo.UserModulePermissions', N'U') IS NOT NULL
                DELETE FROM dbo.UserModulePermissions WHERE ModuleKey = N'Therapy';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CycleId",
            table: "AuditLogs",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.CreateTable(
            name: "TherapyCycles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TherapyCyleNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DecisionText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TherapyCycles", x => x.Id);
                table.ForeignKey(
                    name: "FK_TherapyCycles_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_TherapyCycles_PatientId",
            table: "TherapyCycles",
            column: "PatientId");

        migrationBuilder.AddColumn<Guid>(
            name: "TherapyCycleId",
            table: "Appointments",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TherapyCycleId",
            table: "Encounters",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TherapyCycleId",
            table: "PatientDocuments",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(name: "IX_Appointments_TherapyCycleId", table: "Appointments", column: "TherapyCycleId");
        migrationBuilder.CreateIndex(name: "IX_Encounters_TherapyCycleId", table: "Encounters", column: "TherapyCycleId");
        migrationBuilder.CreateIndex(name: "IX_PatientDocuments_TherapyCycleId", table: "PatientDocuments", column: "TherapyCycleId");

        migrationBuilder.AddForeignKey(
            name: "FK_Appointments_TherapyCycles_TherapyCycleId",
            table: "Appointments",
            column: "TherapyCycleId",
            principalTable: "TherapyCycles",
            principalColumn: "Id",
            onDelete: ReferentialAction.NoAction);
        migrationBuilder.AddForeignKey(
            name: "FK_Encounters_TherapyCycles_TherapyCycleId",
            table: "Encounters",
            column: "TherapyCycleId",
            principalTable: "TherapyCycles",
            principalColumn: "Id");
        migrationBuilder.AddForeignKey(
            name: "FK_PatientDocuments_TherapyCycles_TherapyCycleId",
            table: "PatientDocuments",
            column: "TherapyCycleId",
            principalTable: "TherapyCycles",
            principalColumn: "Id",
            onDelete: ReferentialAction.NoAction);
    }
}
