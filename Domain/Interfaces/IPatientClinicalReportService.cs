using EHMR.Resources.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces;

public interface IPatientClinicalReportService
{
    Task<string> GeneratePdfAsync(
        Guid patientId,
        Guid? encounterId = null,
        Guid? appointmentId = null,
        string? title = null,
        string? generatedBy = null);
}
