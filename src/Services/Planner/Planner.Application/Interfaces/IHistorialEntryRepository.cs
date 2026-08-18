using Planner.Domain;

namespace Planner.Application.Interfaces;

public interface IHistorialEntryRepository
{
    Task<List<HistorialEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<HistorialEntry?> GetByIdAsync(int idHistorialEntry, CancellationToken cancellationToken = default);
    void Add(HistorialEntry historialEntry);
    void Remove(HistorialEntry historialEntry);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
