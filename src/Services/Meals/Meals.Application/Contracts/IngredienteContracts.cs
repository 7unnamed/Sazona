namespace Meals.Application.Contracts;

public record IngredienteRequest(string NombreIngrediente, decimal Cantidad, string Unidad);

public record IngredienteResponse(int IdIngrediente, string NombreIngrediente, decimal Cantidad, string Unidad);
