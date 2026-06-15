using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Infrastructure.Persistence
{
    public interface IEntitySeeder
    {
        int Order
        {
            get;
        }

        Task SeedAsync(
            DesktopTherapyDbContext context,
            CancellationToken ct = default);
    }

    public class DatabaseSeeder
    {
        private readonly IEnumerable<IEntitySeeder> _seeders;

        public DatabaseSeeder(IEnumerable<IEntitySeeder> seeders)
        {
            _seeders=seeders;
        }

        public async Task SeedAsync(DesktopTherapyDbContext context)
        {
            foreach(var seeder in _seeders)
            {
                await seeder.SeedAsync(context);
            }

            await context.SaveChangesAsync();
        }
    }
}