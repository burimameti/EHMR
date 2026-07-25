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
    public static class SequenceHelper
    {
        public static async Task<string> GenerateNumberAsync(
     DesktopTherapyDbContext db,
     string sequenceName,
     string prefix)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            for(int attempt = 0; attempt<3; attempt++)
            {
                var sequence = await db.Sequences
                    .FirstOrDefaultAsync(x => x.Name==sequenceName&&x.SequenceDate==today);

                if(sequence==null)
                {
                    sequence=new Sequence { Name=sequenceName, SequenceDate=today, Value=1 };
                    db.Sequences.Add(sequence);
                }
                else
                {
                    sequence.Value++;
                }

                try
                {
                    await db.SaveChangesAsync();
                    return $"{prefix}-{today:yyyyMMdd}-{sequence.Value:D6}";
                }
                catch(DbUpdateException)
                {
                    db.Entry(sequence).State=EntityState.Detached;
                    if(attempt==2) throw;
                }
            }

            throw new InvalidOperationException($"Could not generate sequence number for '{sequenceName}'.");
        }
    } }
