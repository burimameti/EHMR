using EHMR.Domain.Entities;
using EHMR.Services;
using EHMR.Services.Dto;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Domain.Interfaces;

public interface IPatientService
{
    Task<List<PatientDto>> GetAllAsync(CancellationToken ct = default);
      Task<List<Patient>> GetAllBaseAsync(CancellationToken ct = default);

    Task<(List<PatientDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null, CancellationToken ct = default);

    Task<List<PatientDto>> SearchAsync(string term, CancellationToken ct = default);

    Task<PatientDto?> GetByIdAsync(Guid id, bool includeChildren = false, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    Task<int> GetCountAsync(CancellationToken ct = default);

    Task AddAsync(PatientEditDto dto, CancellationToken ct = default);

    Task UpdatePatientAsync(PatientEditDto dto, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<List<DoctorDto>> SearchDoctorsAsync(string term, CancellationToken ct = default);

    Task<List<Mkb10CodeDto>> SearchMkb10CodesAsync(string term, CancellationToken ct = default);

    Task<List<MedicineDto>> SearchMedicinesAsync(string term, CancellationToken ct = default);

    Task SavePatientAsync(PatientService.PatientSaveModel model, CancellationToken ct = default);
}