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

        public async Task RunAsync(CancellationToken ct = default)
        {
            var ordered = _seeders
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
                    Console.WriteLine($"[SEEDER FAIL] {seeder.GetType().Name}");
                    Console.WriteLine(ex);

                    throw;
                }
            }

            Console.WriteLine("[SEEDER DONE]");
        }
    }
}