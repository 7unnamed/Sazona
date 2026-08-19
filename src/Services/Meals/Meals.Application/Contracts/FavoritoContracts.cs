using Meals.Domain.Enums;

namespace Meals.Application.Contracts;

public record AgregarFavoritoRequest(int IdPlato);

public record FavoritoResponse(
    int IdFavorito,
    int IdPlato,
    string NombrePlato,
    TipoComida TipoComida,
    int PorcionesBase,
    string? ImagenUrl);
