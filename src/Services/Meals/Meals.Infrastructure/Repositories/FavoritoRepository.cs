using Meals.Application.Interfaces;
using Meals.Domain;
using Meals.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Meals.Infrastructure.Repositories;

public class FavoritoRepository : IFavoritoRepository
{
    private readonly MealsDbContext _dbContext;

    public FavoritoRepository(MealsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Favorito>> GetAllByUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Favoritos
            .Include(f => f.Plato)
            .Where(f => f.IdUsuario == idUsuario)
            .ToListAsync(cancellationToken);
    }

    public async Task<Favorito?> GetByUsuarioYPlatoAsync(int idUsuario, int idPlato, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Favoritos
            .Include(f => f.Plato)
            .FirstOrDefaultAsync(f => f.IdUsuario == idUsuario && f.IdPlato == idPlato, cancellationToken);
    }

    public void Add(Favorito favorito) => _dbContext.Favoritos.Add(favorito);

    public void Remove(Favorito favorito) => _dbContext.Favoritos.Remove(favorito);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
