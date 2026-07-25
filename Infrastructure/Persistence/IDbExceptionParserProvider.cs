using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence;

public interface IDbExceptionParserProvider
{
    void ParseAndRaise(DbUpdateException exception);
}
