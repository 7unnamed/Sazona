using Meals.Application.Contracts;

namespace Meals.Application.Interfaces;

public interface IPlatoService
{
    Task<List<PlatoResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PlatoResponse?> GetByIdAsync(int idPlato, CancellationToken cancellationToken = default);
    Task<List<PlatoResponse>> SearchByNombreAsync(string nombre, CancellationToken cancellationToken = default);
    Task<PlatoResponse?> GetRandomAsync(CancellationToken cancellationToken = default);
    Task<List<PlatoResponse>> GetNoCocinadosAsync(int idUsuario, CancellationToken cancellationToken = default);
    Task<PlatoResponse> CreateAsync(CrearPlatoRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int idPlato, ActualizarPlatoRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int idPlato, CancellationToken cancellationToken = default);
    Task<PlatoResponse?> SetImagenAsync(int idPlato, string imagenUrl, CancellationToken cancellationToken = default);

    Task<PlatoIngredienteResponse?> AddIngredienteAsync(int idPlato, AgregarIngredienteAPlatoRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateIngredienteAsync(int idPlato, int idPlatoIngrediente, ActualizarPlatoIngredienteRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveIngredienteAsync(int idPlato, int idPlatoIngrediente, CancellationToken cancellationToken = default);

    Task<PasoPreparacionResponse?> AddPasoAsync(int idPlato, AgregarPasoPreparacionRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdatePasoAsync(int idPlato, int idPaso, ActualizarPasoPreparacionRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemovePasoAsync(int idPlato, int idPaso, CancellationToken cancellationToken = default);

    Task<bool> MarcarCocinadoAsync(int idPlato, int idUsuario, CancellationToken cancellationToken = default);
}
