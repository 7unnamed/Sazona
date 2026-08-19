using Meals.Application.Contracts;
using Meals.Application.Interfaces;

namespace Meals.Api.Endpoints;

public static class PasoPreparacionEndpoints
{
    public static void MapPasoPreparacionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platos/{idPlato:int}/pasos").WithTags("PasosPreparacion").RequireAuthorization();

        group.MapPost("/", async (int idPlato, AgregarPasoPreparacionRequest request, IPlatoService platoService) =>
        {
            var paso = await platoService.AddPasoAsync(idPlato, request);
            return paso is null
                ? Results.NotFound()
                : Results.Created($"/platos/{idPlato}/pasos/{paso.IdPasoPreparacion}", paso);
        }).RequireAuthorization(p => p.RequireRole("Administrador"));

        group.MapPut("/{idPaso:int}", async (int idPlato, int idPaso, ActualizarPasoPreparacionRequest request, IPlatoService platoService) =>
        {
            var actualizado = await platoService.UpdatePasoAsync(idPlato, idPaso, request);
            return actualizado ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(p => p.RequireRole("Administrador"));

        group.MapDelete("/{idPaso:int}", async (int idPlato, int idPaso, IPlatoService platoService) =>
        {
            var eliminado = await platoService.RemovePasoAsync(idPlato, idPaso);
            return eliminado ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(p => p.RequireRole("Administrador"));
    }
}
