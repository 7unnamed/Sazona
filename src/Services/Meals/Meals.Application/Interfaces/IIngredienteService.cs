using Meals.Application.Contracts;

namespace Meals.Application.Interfaces;

public interface IIngredienteService
{
    Task<List<IngredienteResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IngredienteResponse?> GetByIdAsync(int idIngrediente, CancellationToken cancellationToken = default);
    Task<IngredienteResponse> CreateAsync(CrearIngredienteRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int idIngrediente, ActualizarIngredienteRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int idIngrediente, CancellationToken cancellationToken = default);
    Task<IngredienteResponse?> SetImagenAsync(int idIngrediente, string imagenUrl, CancellationToken cancellationToken = default);
}
