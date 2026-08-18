using Meals.Domain;

namespace Meals.Application.Interfaces;

public interface IIngredienteRepository
{
    Task<List<Ingrediente>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Ingrediente?> GetByIdAsync(int idIngrediente, CancellationToken cancellationToken = default);
    void Add(Ingrediente ingrediente);
    void Remove(Ingrediente ingrediente);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
