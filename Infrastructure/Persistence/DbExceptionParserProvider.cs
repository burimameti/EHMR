using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Infrastructure.Persistence
{
    public sealed class DbExceptionParserProvider
     : IDbExceptionParserProvider
    {
        public void ParseAndRaise(DbUpdateException exception)
        {
            var message =
                exception.InnerException?.Message
                ??exception.Message;

            // later replace with logging service
            Console.WriteLine(message);
        }
    }
}
