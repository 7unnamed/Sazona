using Meals.Application.Interfaces;
using Meals.Domain;
using Meals.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Meals.Infrastructure.Repositories;

public class PlatoRepository : IPlatoRepository
{
    private readonly MealsDbContext _dbContext;

    public PlatoRepository(MealsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Plato>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Platos
            .Include(p => p.PlatoIngredientes).ThenInclude(pi => pi.Ingrediente)
            .ToListAsync(cancellationToken);
    }

    public async Task<Plato?> GetByIdAsync(int idPlato, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Platos
            .Include(p => p.PlatoIngredientes).ThenInclude(pi => pi.Ingrediente)
            .FirstOrDefaultAsync(p => p.IdPlato == idPlato, cancellationToken);
    }

    public void Add(Plato plato) => _dbContext.Platos.Add(plato);

    public void Remove(Plato plato) => _dbContext.Platos.Remove(plato);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
