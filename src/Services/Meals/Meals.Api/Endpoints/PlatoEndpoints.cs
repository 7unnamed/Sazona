using Meals.Application.Contracts;
using Meals.Application.Interfaces;

namespace Meals.Api.Endpoints;

public static class PlatoEndpoints
{
    public static void MapPlatoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platos").WithTags("Platos").RequireAuthorization();

        group.MapGet("/", async (IPlatoService platoService) =>
            Results.Ok(await platoService.GetAllAsync()));

        group.MapGet("/{idPlato:int}", async (int idPlato, IPlatoService platoService) =>
        {
            var plato = await platoService.GetByIdAsync(idPlato);
            return plato is null ? Results.NotFound() : Results.Ok(plato);
        });

        group.MapPost("/", async (CrearPlatoRequest request, IPlatoService platoService) =>
        {
            try
            {
                var plato = await platoService.CreateAsync(request);
                return Results.Created($"/platos/{plato.IdPlato}", plato);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPut("/{idPlato:int}", async (int idPlato, ActualizarPlatoRequest request, IPlatoService platoService) =>
        {
            var actualizado = await platoService.UpdateAsync(idPlato, request);
            return actualizado ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{idPlato:int}", async (int idPlato, IPlatoService platoService) =>
        {
            var eliminado = await platoService.DeleteAsync(idPlato);
            return eliminado ? Results.NoContent() : Results.NotFound();
        });
    }
}
