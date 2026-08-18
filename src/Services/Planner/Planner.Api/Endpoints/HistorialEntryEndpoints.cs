using Planner.Application.Contracts;
using Planner.Application.Interfaces;

namespace Planner.Api.Endpoints;

public static class HistorialEntryEndpoints
{
    public static void MapHistorialEntryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/historial").WithTags("HistorialEntries").RequireAuthorization();

        group.MapGet("/", async (IHistorialEntryService historialEntryService) =>
            Results.Ok(await historialEntryService.GetAllAsync()));

        group.MapGet("/{idHistorialEntry:int}", async (int idHistorialEntry, IHistorialEntryService historialEntryService) =>
        {
            var entry = await historialEntryService.GetByIdAsync(idHistorialEntry);
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        });

        group.MapPost("/", async (CrearHistorialEntryRequest request, IHistorialEntryService historialEntryService) =>
        {
            var entry = await historialEntryService.CreateAsync(request);
            return Results.Created($"/historial/{entry.IdHistorialEntry}", entry);
        });

        group.MapPut("/{idHistorialEntry:int}", async (int idHistorialEntry, ActualizarHistorialEntryRequest request, IHistorialEntryService historialEntryService) =>
        {
            var actualizado = await historialEntryService.UpdateAsync(idHistorialEntry, request);
            return actualizado ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{idHistorialEntry:int}", async (int idHistorialEntry, IHistorialEntryService historialEntryService) =>
        {
            var eliminado = await historialEntryService.DeleteAsync(idHistorialEntry);
            return eliminado ? Results.NoContent() : Results.NotFound();
        });
    }
}
