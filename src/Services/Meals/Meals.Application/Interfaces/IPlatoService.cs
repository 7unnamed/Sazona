using Meals.Application.Contracts;

namespace Meals.Application.Interfaces;

public interface IPlatoService
{
    Task<List<PlatoResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PlatoResponse?> GetByIdAsync(int idPlato, CancellationToken cancellationToken = default);
    Task<PlatoResponse> CreateAsync(CrearPlatoRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int idPlato, ActualizarPlatoRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int idPlato, CancellationToken cancellationToken = default);

    Task<IngredienteResponse?> AddIngredienteAsync(int idPlato, IngredienteRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateIngredienteAsync(int idPlato, int idIngrediente, IngredienteRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveIngredienteAsync(int idPlato, int idIngrediente, CancellationToken cancellationToken = default);
}
