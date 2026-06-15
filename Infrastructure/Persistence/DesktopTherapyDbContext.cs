using System;
using Microsoft.EntityFrameworkCore;
using EHMR.Domain.Entities;

namespace EHMR.Infrastructure.Persistence;

public class DesktopTherapyDbContext : TherapyTrackerDbContext
{
    private const string DefaultConnection =
        "Server=.\\SQLEXPRESS;Database=TherapyTrackerDesktopPoc;User Id=t24test;Password=t24test;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    public DesktopTherapyDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public static DesktopTherapyDbContext CreateManual(string? connectionString = null)
    {
        var options =
            new DbContextOptionsBuilder<DesktopTherapyDbContext>()
                .UseSqlServer(connectionString??DefaultConnection)
                .Options;

        return new DesktopTherapyDbContext(options);
    }

    public static class SeedIds
    {
        // ================= TENANT =================
        public static readonly Guid Tenant =
            Guid.Parse("00000000-0000-0000-0000-888888888888");

        // ================= USERS =================
        public static readonly Guid AdminUser =
            Guid.Parse("00000000-0000-0000-0000-000000000001");

        public static readonly Guid DocUser1 =
            Guid.Parse("00000000-0000-0000-0000-000000000002");

        public static readonly Guid DocUser2 =
            Guid.Parse("00000000-0000-0000-0000-000000000003");

        public static readonly Guid NurseUser =
            Guid.Parse("00000000-0000-0000-0000-000000000004");

        // ================= DOCTORS =================
        public static readonly Guid Doctor1 =
            Guid.Parse("10000000-0000-0000-0000-000000000001");

        public static readonly Guid Doctor2 =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        // ================= PATIENTS =================
        public static readonly Guid Patient1 =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static readonly Guid Patient2 =
            Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static readonly Guid Patient3 =
            Guid.Parse("33333333-3333-3333-3333-333333333333");

        // ================= MEDICINES =================
        public static readonly Guid Med1 =
            Guid.Parse("30000000-0000-0000-0000-000000000001");

        public static readonly Guid Med2 =
            Guid.Parse("30000000-0000-0000-0000-000000000002");

        public static readonly Guid Med3 =
            Guid.Parse("30000000-0000-0000-0000-000000000003");

        public static readonly Guid Med4 =
            Guid.Parse("30000000-0000-0000-0000-000000000004");

        // ================= TREATMENT PLANS =================
        public static readonly Guid Plan1 =
            Guid.Parse("40000000-0000-0000-0000-000000000001");

        public static readonly Guid Plan2 =
            Guid.Parse("40000000-0000-0000-0000-000000000002");

        // ================= SCHEDULES =================
        public static readonly Guid Schedule1 =
            Guid.Parse("50000000-0000-0000-0000-000000000001");

        public static readonly Guid Schedule2 =
            Guid.Parse("50000000-0000-0000-0000-000000000002");

        // ================= RULES (NEW EXTENSION) =================
        public static readonly Guid Rule1 =
            Guid.Parse("51000000-0000-0000-0000-000000000001");

        public static readonly Guid Rule2 =
            Guid.Parse("51000000-0000-0000-0000-000000000002");

        public static readonly Guid Rule3 =
            Guid.Parse("51000000-0000-0000-0000-000000000003");

        public static readonly Guid Rule4 =
            Guid.Parse("51000000-0000-0000-0000-000000000004");

        // ================= CYCLES (NEW EXTENSION) =================
        public static readonly Guid Cycle1 =
            Guid.Parse("60000000-0000-0000-0000-000000000001");

        public static readonly Guid Cycle2 =
            Guid.Parse("60000000-0000-0000-0000-000000000002");

        public static readonly Guid Cycle3 =
            Guid.Parse("60000000-0000-0000-0000-000000000003");

        public static readonly Guid Cycle4 =
            Guid.Parse("60000000-0000-0000-0000-000000000004");

        // ================= DOSES (NEW EXTENSION) =================
        public static readonly Guid Dose1 =
            Guid.Parse("61000000-0000-0000-0000-000000000001");

        public static readonly Guid Dose2 =
            Guid.Parse("61000000-0000-0000-0000-000000000002");

        public static readonly Guid Dose3 =
            Guid.Parse("61000000-0000-0000-0000-000000000003");

        public static readonly Guid Dose4 =
            Guid.Parse("61000000-0000-0000-0000-000000000004");

        public static readonly Guid Dose5 =
            Guid.Parse("61000000-0000-0000-0000-000000000005");

        // ================= INVENTORY (NEW EXTENSION) =================
        public static readonly Guid Inventory1 =
            Guid.Parse("70000000-0000-0000-0000-000000000001");

        public static readonly Guid Inventory2 =
            Guid.Parse("70000000-0000-0000-0000-000000000002");

        public static readonly Guid Inventory3 =
            Guid.Parse("70000000-0000-0000-0000-000000000003");

        public static readonly Guid Inventory4 =
            Guid.Parse("70000000-0000-0000-0000-000000000004");

        // ================= APPOINTMENTS =================
        public static readonly Guid Appt1 =
            Guid.Parse("80000000-0000-0000-0000-000000000001");

        public static readonly Guid Appt2 =
            Guid.Parse("80000000-0000-0000-0000-000000000002");

        // ================= ENCOUNTERS =================
        public static readonly Guid Encounter1 =
            Guid.Parse("90000000-0000-0000-0000-000000000001");

        public static readonly Guid Encounter2 =
            Guid.Parse("90000000-0000-0000-0000-000000000002");

        // ================= PRESCRIPTIONS =================
        public static readonly Guid Presc1 =
            Guid.Parse("A0000000-0000-0000-0000-000000000001");

        public static readonly Guid Presc2 =
            Guid.Parse("A0000000-0000-0000-0000-000000000002");

        public static readonly DateTime Appt1Start = new(2026, 01, 02, 09, 00, 00, DateTimeKind.Utc);
        public static readonly DateTime Appt1End = new(2026, 01, 02, 10, 00, 00, DateTimeKind.Utc);

        public static readonly DateTime Appt2Start = new(2026, 01, 03, 11, 00, 00, DateTimeKind.Utc);
        public static readonly DateTime Appt2End = new(2026, 01, 03, 12, 00, 00, DateTimeKind.Utc);
        public static readonly DateTime Encounter1Start = new(2026, 01, 01, 09, 00, 00, DateTimeKind.Utc);
        public static readonly DateTime Encounter1End = new(2026, 01, 01, 09, 30, 00, DateTimeKind.Utc);

        public static readonly DateTime Encounter2Start = new(2026, 01, 03, 11, 00, 00, DateTimeKind.Utc);
        public static readonly DateTime Encounter2End = new(2026, 01, 03, 11, 30, 00, DateTimeKind.Utc);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}