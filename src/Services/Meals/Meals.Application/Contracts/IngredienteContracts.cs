using Meals.Domain.Enums;

namespace Meals.Application.Contracts;

public record CrearIngredienteRequest(string Nombre, string PaisProcedencia, CategoriaIngrediente Categoria, string? Descripcion);

public record ActualizarIngredienteRequest(string Nombre, string PaisProcedencia, CategoriaIngrediente Categoria, string? Descripcion);

public record IngredienteResponse(int IdIngrediente, string Nombre, string PaisProcedencia, CategoriaIngrediente Categoria, string? Descripcion);
