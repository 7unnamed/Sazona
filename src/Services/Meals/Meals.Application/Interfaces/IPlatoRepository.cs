using Meals.Domain;

namespace Meals.Application.Interfaces;

public interface IPlatoRepository
{
    Task<List<Plato>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Plato?> GetByIdAsync(int idPlato, CancellationToken cancellationToken = default);
    Task<List<Plato>> SearchByNombreAsync(string nombre, CancellationToken cancellationToken = default);
    Task<Plato?> GetRandomAsync(CancellationToken cancellationToken = default);
    Task<List<Plato>> GetNoCocinadosAsync(int idUsuario, CancellationToken cancellationToken = default);
    Task MarcarCocinadoAsync(int idPlato, int idUsuario, CancellationToken cancellationToken = default);
    void Add(Plato plato);
    void Remove(Plato plato);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
