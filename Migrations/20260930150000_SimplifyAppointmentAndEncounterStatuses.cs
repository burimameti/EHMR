using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations;

public partial class SimplifyAppointmentAndEncounterStatuses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preserve the meaning of existing integer enum values while removing
        // CheckedIn and ReScheduled from the application model.
        migrationBuilder.Sql("""
            UPDATE [Encounters]
            SET [Status] = CASE [Status]
                WHEN 2 THEN 1 -- old InProgress -> new InProgress
                WHEN 3 THEN 2 -- old Completed -> new Completed
                WHEN 4 THEN 3 -- old Cancelled -> new Cancelled
                WHEN 5 THEN 4 -- old NoShow -> new NoShow
                ELSE [Status] -- old Scheduled / CheckedIn
            END
            WHERE [Status] IN (2, 3, 4, 5);
            """);

        migrationBuilder.Sql("""
            UPDATE [Appointments]
            SET [Status] = CASE [Status]
                WHEN 5 THEN 1 -- old InProgress -> new InProgress
                WHEN 6 THEN 0 -- old ReScheduled -> Scheduled
                ELSE [Status] -- Scheduled / CheckedIn / Completed / Cancelled / Missed
            END
            WHERE [Status] IN (5, 6);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restore the previous integer representation.
        migrationBuilder.Sql("""
            UPDATE [Encounters]
            SET [Status] = CASE [Status]
                WHEN 4 THEN 5
                WHEN 3 THEN 4
                WHEN 2 THEN 3
                WHEN 1 THEN 2
                ELSE [Status]
            END
            WHERE [Status] IN (1, 2, 3, 4);
            """);

        migrationBuilder.Sql("""
            UPDATE [Appointments]
            SET [Status] = CASE [Status]
                WHEN 1 THEN 5
                ELSE [Status]
            END
            WHERE [Status] = 1;
            """);
    }
}
