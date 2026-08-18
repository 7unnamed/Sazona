using Meals.Domain.Enums;

namespace Meals.Application.Contracts;

public record CrearPlatoRequest(
    string NombrePlato,
    TipoComida TipoComida,
    int PorcionesBase,
    List<IngredienteRequest> Ingredientes);

public record ActualizarPlatoRequest(
    string NombrePlato,
    TipoComida TipoComida,
    int PorcionesBase);

public record PlatoResponse(
    int IdPlato,
    string NombrePlato,
    TipoComida TipoComida,
    int PorcionesBase,
    List<IngredienteResponse> Ingredientes);
