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

    public async Task<List<Plato>> SearchByNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Platos
            .Include(p => p.PlatoIngredientes).ThenInclude(pi => pi.Ingrediente)
            .Where(p => EF.Functions.ILike(p.NombrePlato, $"%{nombre}%"))
            .ToListAsync(cancellationToken);
    }

    public async Task<Plato?> GetRandomAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Platos
            .Include(p => p.PlatoIngredientes).ThenInclude(pi => pi.Ingrediente)
            .OrderBy(p => EF.Functions.Random())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Plato>> GetNoCocinadosAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        var idsCocinados = _dbContext.Set<PlatoCocinado>()
            .Where(pc => pc.IdUsuario == idUsuario)
            .Select(pc => pc.IdPlato);

        return await _dbContext.Platos
            .Include(p => p.PlatoIngredientes).ThenInclude(pi => pi.Ingrediente)
            .Where(p => !idsCocinados.Contains(p.IdPlato))
            .ToListAsync(cancellationToken);
    }

    public void Add(Plato plato) => _dbContext.Platos.Add(plato);

    public void Remove(Plato plato) => _dbContext.Platos.Remove(plato);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
