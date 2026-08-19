namespace Meals.Application.Contracts;

public record AgregarPasoPreparacionRequest(string Descripcion);

public record ActualizarPasoPreparacionRequest(string Descripcion);

public record PasoPreparacionResponse(int IdPasoPreparacion, int Orden, string Descripcion);
