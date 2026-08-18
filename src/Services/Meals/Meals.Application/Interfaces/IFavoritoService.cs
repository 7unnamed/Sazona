using Meals.Application.Contracts;

namespace Meals.Application.Interfaces;

public interface IFavoritoService
{
    Task<List<FavoritoResponse>> GetAllAsync(int idUsuario, CancellationToken cancellationToken = default);
    Task<FavoritoResponse> AddAsync(int idUsuario, AgregarFavoritoRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(int idUsuario, int idPlato, CancellationToken cancellationToken = default);
}
