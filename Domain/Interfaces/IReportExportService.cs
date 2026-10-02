using EHMR.Resources.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces
{
    public interface IReportExportService
    {
        Task<string> ExportToExcelAsync(string reportTitle, IReadOnlyList<SparkGridColumn> columns, IReadOnlyList<SparkGridRow> rows);
        Task<string> ExportToPdfAsync(
        string reportTitle,
        string insitutionName,
        string generatedBy,
        DateTime startDate,
        DateTime endDate,
        IReadOnlyList<SparkGridColumn> columns,
        IReadOnlyList<SparkGridRow> rows,
        string? selectedMedicine = null,
        decimal? selectedMedicineTotalQuantity = null);
    }
}
