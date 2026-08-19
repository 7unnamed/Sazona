using Meals.Application.Contracts;
using Meals.Application.Interfaces;

namespace Meals.Api.Endpoints;

public static class PlatoIngredienteEndpoints
{
    public static void MapPlatoIngredienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platos/{idPlato:int}/ingredientes").WithTags("PlatoIngredientes").RequireAuthorization();

        group.MapPost("/", async (int idPlato, AgregarIngredienteAPlatoRequest request, IPlatoService platoService) =>
        {
            try
            {
                var platoIngrediente = await platoService.AddIngredienteAsync(idPlato, request);
                return platoIngrediente is null
                    ? Results.NotFound()
                    : Results.Created($"/platos/{idPlato}/ingredientes/{platoIngrediente.IdPlatoIngrediente}", platoIngrediente);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).RequireAuthorization(p => p.RequireRole("Administrador"));

        group.MapPut("/{idPlatoIngrediente:int}", async (int idPlato, int idPlatoIngrediente, ActualizarPlatoIngredienteRequest request, IPlatoService platoService) =>
        {
            var actualizado = await platoService.UpdateIngredienteAsync(idPlato, idPlatoIngrediente, request);
            return actualizado ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(p => p.RequireRole("Administrador"));

        group.MapDelete("/{idPlatoIngrediente:int}", async (int idPlato, int idPlatoIngrediente, IPlatoService platoService) =>
        {
            var eliminado = await platoService.RemoveIngredienteAsync(idPlato, idPlatoIngrediente);
            return eliminado ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(p => p.RequireRole("Administrador"));
    }
}
