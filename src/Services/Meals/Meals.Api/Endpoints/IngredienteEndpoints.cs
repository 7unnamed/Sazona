using Meals.Application.Contracts;
using Meals.Application.Interfaces;

namespace Meals.Api.Endpoints;

public static class IngredienteEndpoints
{
    public static void MapIngredienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platos/{idPlato:int}/ingredientes").WithTags("Ingredientes").RequireAuthorization();

        group.MapPost("/", async (int idPlato, IngredienteRequest request, IPlatoService platoService) =>
        {
            var ingrediente = await platoService.AddIngredienteAsync(idPlato, request);
            return ingrediente is null
                ? Results.NotFound()
                : Results.Created($"/platos/{idPlato}/ingredientes/{ingrediente.IdIngrediente}", ingrediente);
        });

        group.MapPut("/{idIngrediente:int}", async (int idPlato, int idIngrediente, IngredienteRequest request, IPlatoService platoService) =>
        {
            var actualizado = await platoService.UpdateIngredienteAsync(idPlato, idIngrediente, request);
            return actualizado ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{idIngrediente:int}", async (int idPlato, int idIngrediente, IPlatoService platoService) =>
        {
            var eliminado = await platoService.RemoveIngredienteAsync(idPlato, idIngrediente);
            return eliminado ? Results.NoContent() : Results.NotFound();
        });
    }
}
