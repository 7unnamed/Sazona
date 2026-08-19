using Meals.Application.Contracts;
using Meals.Application.Interfaces;
using Meals.Domain;

namespace Meals.Application.Services;

public class FavoritoService : IFavoritoService
{
    private readonly IFavoritoRepository _favoritoRepository;
    private readonly IPlatoRepository _platoRepository;

    public FavoritoService(IFavoritoRepository favoritoRepository, IPlatoRepository platoRepository)
    {
        _favoritoRepository = favoritoRepository;
        _platoRepository = platoRepository;
    }

    public async Task<List<FavoritoResponse>> GetAllAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        var favoritos = await _favoritoRepository.GetAllByUsuarioAsync(idUsuario, cancellationToken);
        return favoritos.Select(ToResponse).ToList();
    }

    public async Task<FavoritoResponse> AddAsync(int idUsuario, AgregarFavoritoRequest request, CancellationToken cancellationToken = default)
    {
        var existente = await _favoritoRepository.GetByUsuarioYPlatoAsync(idUsuario, request.IdPlato, cancellationToken);
        if (existente is not null)
        {
            return ToResponse(existente);
        }

        var plato = await _platoRepository.GetByIdAsync(request.IdPlato, cancellationToken)
            ?? throw new InvalidOperationException($"No existe ningún plato con Id {request.IdPlato}.");

        var favorito = new Favorito
        {
            IdUsuario = idUsuario,
            IdPlato = plato.IdPlato,
            Plato = plato
        };

        _favoritoRepository.Add(favorito);
        await _favoritoRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(favorito);
    }

    public async Task<bool> RemoveAsync(int idUsuario, int idPlato, CancellationToken cancellationToken = default)
    {
        var favorito = await _favoritoRepository.GetByUsuarioYPlatoAsync(idUsuario, idPlato, cancellationToken);
        if (favorito is null)
        {
            return false;
        }

        _favoritoRepository.Remove(favorito);
        await _favoritoRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static FavoritoResponse ToResponse(Favorito favorito) => new(
        favorito.IdFavorito,
        favorito.IdPlato,
        favorito.Plato.NombrePlato,
        favorito.Plato.TipoComida,
        favorito.Plato.PorcionesBase,
        favorito.Plato.ImagenUrl);
}
