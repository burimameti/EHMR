using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{


    public class Mkb10CodeService : IMkb10CodeService
    {
        private readonly IDbContextFactory<TherapyTrackerDbContext> _factory;

        public Mkb10CodeService(IDbContextFactory<TherapyTrackerDbContext> factory)
        {
            _factory=factory;
        }

        public async Task<List<Mkb10Code>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Mkb10Codes
                .AsNoTracking()
                .OrderBy(x => x.Code)
                .ToListAsync();
        }

        public async Task<Mkb10Code?> GetByIdAsync(Guid id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.Mkb10Codes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id==id);
        }

        public async Task SaveAsync(Mkb10Code code)
        {
            await using var db = await _factory.CreateDbContextAsync();

            var exists = code.Id!=Guid.Empty
                &&await db.Mkb10Codes.AsNoTracking().AnyAsync(x => x.Id==code.Id);

            if(!exists)
            {
                if(code.Id==Guid.Empty)
                    code.Id=Guid.NewGuid();
                db.Mkb10Codes.Add(code);
            }
            else
            {
                db.Mkb10Codes.Update(code);
            }

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var entity = await db.Mkb10Codes.FirstOrDefaultAsync(x => x.Id==id);
            if(entity is null) return;

            db.Mkb10Codes.Remove(entity);
            await db.SaveChangesAsync();
        }

        public async Task<List<Mkb10Code>> SearchAsync(string query, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.Mkb10Codes
                .AsNoTracking()
                .Where(x => x.IsActive&&
                    (x.Code.Contains(query)||(x.Description!=null&&x.Description.Contains(query))))
                .OrderBy(x => x.Code)
                .Take(20)
                .ToListAsync(ct);
        }
    }
}
