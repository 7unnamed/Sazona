namespace Meals.Application.Contracts;

public record AgregarIngredienteAPlatoRequest(int IdIngrediente, decimal Cantidad, string Unidad);

public record ActualizarPlatoIngredienteRequest(decimal Cantidad, string Unidad);

public record PlatoIngredienteResponse(int IdPlatoIngrediente, int IdIngrediente, string NombreIngrediente, decimal Cantidad, string Unidad);
