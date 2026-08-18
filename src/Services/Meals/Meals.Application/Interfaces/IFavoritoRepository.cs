using Meals.Domain;

namespace Meals.Application.Interfaces;

public interface IFavoritoRepository
{
    Task<List<Favorito>> GetAllByUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default);
    Task<Favorito?> GetByUsuarioYPlatoAsync(int idUsuario, int idPlato, CancellationToken cancellationToken = default);
    void Add(Favorito favorito);
    void Remove(Favorito favorito);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
