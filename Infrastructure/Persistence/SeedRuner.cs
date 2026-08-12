using EHMR.Infrastructure.Persistence.Seeders;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Infrastructure.Persistence
{
    public class SeederRunner
    {
        private readonly IEnumerable<IEntitySeeder> _seeders;
        private readonly DesktopTherapyDbContext _context;

        public SeederRunner(
            IEnumerable<IEntitySeeder> seeders,
            DesktopTherapyDbContext context)
        {
            _seeders=seeders;
            _context=context;
        }

        /// <summary>
        /// Seeder-и што внесуваат само примероци. Се извршуваат само кога
        /// корисникот побарал демо податоци. Каталозите и системските записи
        /// (МКБ-10, корисници, доктори) одат секогаш.
        /// </summary>
        private static readonly HashSet<string> DemoOnlySeeders = new(StringComparer.Ordinal)
        {
            "PatientSeeder",
            "AppointmentSeeder",
            "EncounterSeeder",
            "ClinicalScenarioSeeder",
            "DiagnosisSeeder",
            "PrescriptionSeeder",
            "TherapyCycleSeeder",
            "PatientMedicineSeeder",
            "DocumentSeeder",
            "AlertSeeder",
            "NotificationSeeder",
            "AuditLogSeeder",
            "InventorySeeder"
        };

        /// <param name="includeDemoData">
        /// Кога е false, демо seeder-ите се прескокнуваат — базата останува
        /// празна освен каталозите и системските корисници.
        /// </param>
        public async Task RunAsync(bool includeDemoData, CancellationToken ct = default)
        {
            var ordered = _seeders
                .Where(x => includeDemoData||!DemoOnlySeeders.Contains(x.GetType().Name))
                .OrderBy(x => x.Order)
                .ToList();

            foreach(var seeder in ordered)
            {
                try
                {
                    var name = seeder.GetType().Name;

                    Console.WriteLine($"[SEEDER START] {name}");

                    await seeder.SeedAsync(_context, ct);

                    Console.WriteLine($"[SEEDER SAVE] {name}");

                    await _context.SaveChangesAsync(ct);
                    await Task.Delay(500, ct); // give some breathing room for the console output
                    Console.WriteLine($"[SEEDER END] {name}");
                }
                catch(Exception ex)
                {
                    var name = seeder.GetType().Name;

                    Console.WriteLine($"[SEEDER FAIL] {name}");
                    Console.WriteLine(ex);

                    // Демо податоците се само примероци — нивниот пад не смее да го
                    // спречи подигањето на апликацијата. Порано DiagnosisSeeder
                    // фрлаше кога каталогот на МКБ-10 е празен и целата апликација
                    // не се подигаше.
                    if(DemoOnlySeeders.Contains(name))
                    {
                        // Неуспешниот seeder може да остави недовршени измени
                        // во контекстот; тие се фрлаат за да не му пречат на следниот.
                        _context.ChangeTracker.Clear();
                        continue;
                    }

                    throw;
                }
            }

            Console.WriteLine("[SEEDER DONE]");
        }

        /// <summary>Задржано за повикувачи што не одлучуваат за демо — без демо податоци.</summary>
        public Task RunAsync(CancellationToken ct = default)
            => RunAsync(includeDemoData: false, ct);
    }
}