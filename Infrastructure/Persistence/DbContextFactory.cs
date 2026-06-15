using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Infrastructure.Persistence
{
    public class DesktopTherapyDbContextFactory
     : IDesignTimeDbContextFactory<DesktopTherapyDbContext>
    {
        public DesktopTherapyDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseSqlServer(
                "Server=.\\SQLEXPRESS;Database=TherapyTrackerDesktopPoc;User Id=t24test;Password=t24test;MultipleActiveResultSets=true;TrustServerCertificate=True;"
            );

            return new DesktopTherapyDbContext(optionsBuilder.Options);
        }
    }
}