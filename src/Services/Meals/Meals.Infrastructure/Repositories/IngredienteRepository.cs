using Meals.Application.Interfaces;
using Meals.Domain;
using Meals.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Meals.Infrastructure.Repositories;

public class IngredienteRepository : IIngredienteRepository
{
    private readonly MealsDbContext _dbContext;

    public IngredienteRepository(MealsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Ingrediente>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Ingredientes.ToListAsync(cancellationToken);
    }

    public async Task<Ingrediente?> GetByIdAsync(int idIngrediente, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Ingredientes.FirstOrDefaultAsync(i => i.IdIngrediente == idIngrediente, cancellationToken);
    }

    public void Add(Ingrediente ingrediente) => _dbContext.Ingredientes.Add(ingrediente);

    public void Remove(Ingrediente ingrediente) => _dbContext.Ingredientes.Remove(ingrediente);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
