using EHMR.Backups.Encryption;
using EHMR.Backups.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace EHMR.Infrastructure.Persistence;

public sealed class DesktopTherapyDbContext : TherapyTrackerDbContext
{
    
    public DesktopTherapyDbContext(
        DbContextOptions<DesktopTherapyDbContext> options,
        IDbExceptionParserProvider? exceptionParser,
        IEncryptionService encryptionService)
        : base(options, exceptionParser, encryptionService)
    {
    }
    private const string DefaultConnection =
       "Server=.\\SQLEXPRESS;Database=TherapyTrackerDesktopPoc;User Id=t24test;Password=t24test;MultipleActiveResultSets=true;TrustServerCertificate=True;";
    public static DesktopTherapyDbContext CreateManual(
    IEncryptionService encryptionService,
    IDbExceptionParserProvider? parser = null,
    string? connectionString = null)
    {
        var options = new DbContextOptionsBuilder<DesktopTherapyDbContext>()
            .UseSqlServer(connectionString??DefaultConnection)
            .Options;

        return new DesktopTherapyDbContext(
            options,
            parser,
            encryptionService);
    }

    public static class SeedIds
    {
        // ================= TENANT =================
        public static readonly Guid Tenant =
            Guid.Parse("00000000-0000-0000-0000-000000000001");

        public static readonly Guid AdminScope =
    Guid.Parse("51000000-0000-0000-0000-000000000001");

        public static readonly Guid Doctor1Scope =
            Guid.Parse("51000000-0000-0000-0000-000000000002");

        public static readonly Guid Doctor2Scope =
            Guid.Parse("51000000-0000-0000-0000-000000000003");

        // ================= USERS =================
        public static readonly Guid AdminUser = Guid.Parse("00000000-0000-0000-0000-000000000101");
        public static readonly Guid SuperAdminUser = Guid.Parse("00000000-0000-0000-0000-000000000103");
        public static readonly Guid NurseUser = Guid.Parse("00000000-0000-0000-0000-000000010020");

        public static readonly Guid DocUser1 = Guid.Parse("00000000-0000-0000-0000-000000010001");
        public static readonly Guid DocUser2 = Guid.Parse("00000000-0000-0000-0000-000000010002");
        public static readonly Guid DocUser3 = Guid.Parse("00000000-0000-0000-0000-000000010003");
        public static readonly Guid DocUser4 = Guid.Parse("00000000-0000-0000-0000-000000010004");
        public static readonly Guid DocUser5 = Guid.Parse("00000000-0000-0000-0000-000000010005");
        public static readonly Guid DocUser6 = Guid.Parse("00000000-0000-0000-0000-000000010006");
        public static readonly Guid DocUser7 = Guid.Parse("00000000-0000-0000-0000-000000010007");
        public static readonly Guid DocUser8 = Guid.Parse("00000000-0000-0000-0000-000000010008");
        public static readonly Guid DocUser9 = Guid.Parse("00000000-0000-0000-0000-000000010009");
        public static readonly Guid DocUser10 = Guid.Parse("00000000-0000-0000-0000-000000010010");

        // ================= DOCTORS =================
        public static readonly Guid Doctor1 = Guid.Parse("00000000-0000-0000-0000-000000020001");

        public static readonly Guid Doctor2 = Guid.Parse("00000000-0000-0000-0000-000000020002");
        public static readonly Guid Doctor3 = Guid.Parse("00000000-0000-0000-0000-000000020003");
        public static readonly Guid Doctor4 = Guid.Parse("00000000-0000-0000-0000-000000020004");
        public static readonly Guid Doctor5 = Guid.Parse("00000000-0000-0000-0000-000000020005");
        public static readonly Guid Doctor6 = Guid.Parse("00000000-0000-0000-0000-000000020006");
        public static readonly Guid Doctor7 = Guid.Parse("00000000-0000-0000-0000-000000020007");
        public static readonly Guid Doctor8 = Guid.Parse("00000000-0000-0000-0000-000000020008");
        public static readonly Guid Doctor9 = Guid.Parse("00000000-0000-0000-0000-000000020009");
        public static readonly Guid Doctor10 = Guid.Parse("00000000-0000-0000-0000-000000020010");

        // ================= PATIENTS (1–20 clean range) =================
        public static readonly Guid Patient1 = Guid.Parse("00000000-0000-0000-0000-000000001001");

        public static readonly Guid Patient2 = Guid.Parse("00000000-0000-0000-0000-000000001002");
        public static readonly Guid Patient3 = Guid.Parse("00000000-0000-0000-0000-000000001003");
        public static readonly Guid Patient4 = Guid.Parse("00000000-0000-0000-0000-000000001004");
        public static readonly Guid Patient5 = Guid.Parse("00000000-0000-0000-0000-000000001005");
        public static readonly Guid Patient6 = Guid.Parse("00000000-0000-0000-0000-000000001006");
        public static readonly Guid Patient7 = Guid.Parse("00000000-0000-0000-0000-000000001007");
        public static readonly Guid Patient8 = Guid.Parse("00000000-0000-0000-0000-000000001008");
        public static readonly Guid Patient9 = Guid.Parse("00000000-0000-0000-0000-000000001009");
        public static readonly Guid Patient10 = Guid.Parse("00000000-0000-0000-0000-000000001010");
        public static readonly Guid Patient11 = Guid.Parse("00000000-0000-0000-0000-000000001011");
        public static readonly Guid Patient12 = Guid.Parse("00000000-0000-0000-0000-000000001012");
        public static readonly Guid Patient13 = Guid.Parse("00000000-0000-0000-0000-000000001013");
        public static readonly Guid Patient14 = Guid.Parse("00000000-0000-0000-0000-000000001014");
        public static readonly Guid Patient15 = Guid.Parse("00000000-0000-0000-0000-000000001015");
        public static readonly Guid Patient16 = Guid.Parse("00000000-0000-0000-0000-000000001016");
        public static readonly Guid Patient17 = Guid.Parse("00000000-0000-0000-0000-000000001017");
        public static readonly Guid Patient18 = Guid.Parse("00000000-0000-0000-0000-000000001018");
        public static readonly Guid Patient19 = Guid.Parse("00000000-0000-0000-0000-000000001019");
        public static readonly Guid Patient20 = Guid.Parse("00000000-0000-0000-0000-000000001020");

        // ================= MEDICINES =================
        public static readonly Guid Med1 = Guid.Parse("00000000-0000-0000-0000-000000002001");

        public static readonly Guid Med2 = Guid.Parse("00000000-0000-0000-0000-000000002002");
        public static readonly Guid Med3 = Guid.Parse("00000000-0000-0000-0000-000000002003");
        public static readonly Guid Med4 = Guid.Parse("00000000-0000-0000-0000-000000002004");
        public static readonly Guid Med5 = Guid.Parse("00000000-0000-0000-0000-000000002005");
        public static readonly Guid Med6 = Guid.Parse("00000000-0000-0000-0000-000000002006");
        public static readonly Guid Med7 = Guid.Parse("00000000-0000-0000-0000-000000002007");
        public static readonly Guid Med8 = Guid.Parse("00000000-0000-0000-0000-000000002008");
        public static readonly Guid Med9 = Guid.Parse("00000000-0000-0000-0000-000000002009");
        public static readonly Guid Med10 = Guid.Parse("00000000-0000-0000-0000-000000002010");
        // ================= CYCLES =================
        public static readonly Guid Cycle1 = Guid.Parse("00000000-0000-0000-0000-000000003001");

        public static readonly Guid Cycle2 = Guid.Parse("00000000-0000-0000-0000-000000003002");
        public static readonly Guid Cycle3 = Guid.Parse("00000000-0000-0000-0000-000000003003");
        public static readonly Guid Cycle4 = Guid.Parse("00000000-0000-0000-0000-000000003004");
        public static readonly Guid Cycle5 = Guid.Parse("00000000-0000-0000-0000-000000003005");
        public static readonly Guid Cycle6 = Guid.Parse("00000000-0000-0000-0000-000000003006");
        public static readonly Guid Cycle7 = Guid.Parse("00000000-0000-0000-0000-000000003007");
        public static readonly Guid Cycle8 = Guid.Parse("00000000-0000-0000-0000-000000003008");
        public static readonly Guid Cycle9 = Guid.Parse("00000000-0000-0000-0000-000000003009");
        public static readonly Guid Cycle10 = Guid.Parse("00000000-0000-0000-0000-000000003010");
        public static readonly Guid Cycle11 = Guid.Parse("00000000-0000-0000-0000-000000003011");

        // ================= APPOINTMENTS =================
        public static readonly Guid Appt1 = Guid.Parse("00000000-0000-0000-0000-000000004001");

        public static readonly Guid Appt2 = Guid.Parse("00000000-0000-0000-0000-000000004002");

        // ================= ENCOUNTERS =================
        public static readonly Guid Encounter1 = Guid.Parse("00000000-0000-0000-0000-000000005001");

        public static readonly Guid Encounter2 = Guid.Parse("00000000-0000-0000-0000-000000005002");
        public static readonly Guid Inv1 = Guid.Parse("90000000-0000-0000-0000-000000005002");

        // ================= PRESCRIPTIONS =================
       // public static readonly Guid Presc1 = Guid.Parse("00000000-0000-0000-0000-000000006001");

        //public static readonly Guid Presc2 = Guid.Parse("00000000-0000-0000-0000-000000006002");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}