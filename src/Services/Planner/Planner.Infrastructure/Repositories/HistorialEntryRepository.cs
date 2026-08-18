using Planner.Application.Interfaces;
using Planner.Domain;
using Planner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Planner.Infrastructure.Repositories;

public class HistorialEntryRepository : IHistorialEntryRepository
{
    private readonly PlannerDbContext _dbContext;

    public HistorialEntryRepository(PlannerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<HistorialEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.HistorialEntries.ToListAsync(cancellationToken);
    }

    public async Task<HistorialEntry?> GetByIdAsync(int idHistorialEntry, CancellationToken cancellationToken = default)
    {
        return await _dbContext.HistorialEntries
            .FirstOrDefaultAsync(h => h.IdHistorialEntry == idHistorialEntry, cancellationToken);
    }

    public void Add(HistorialEntry historialEntry) => _dbContext.HistorialEntries.Add(historialEntry);

    public void Remove(HistorialEntry historialEntry) => _dbContext.HistorialEntries.Remove(historialEntry);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
