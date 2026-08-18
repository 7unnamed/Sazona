using Planner.Application.Contracts;

namespace Planner.Application.Interfaces;

public interface IHistorialEntryService
{
    Task<List<HistorialEntryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<HistorialEntryResponse?> GetByIdAsync(int idHistorialEntry, CancellationToken cancellationToken = default);
    Task<HistorialEntryResponse> CreateAsync(CrearHistorialEntryRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int idHistorialEntry, ActualizarHistorialEntryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int idHistorialEntry, CancellationToken cancellationToken = default);
}
