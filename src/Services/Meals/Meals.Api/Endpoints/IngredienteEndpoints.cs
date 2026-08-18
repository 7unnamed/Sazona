using Meals.Application.Contracts;
using Meals.Application.Interfaces;
using Meals.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;

namespace Meals.Api.Endpoints;

public static class IngredienteEndpoints
{
    public static void MapIngredienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/ingredientes").WithTags("Ingredientes").RequireAuthorization();

        group.MapGet("/", async (IIngredienteService ingredienteService) =>
            Results.Ok(await ingredienteService.GetAllAsync()));

        group.MapGet("/{idIngrediente:int}", async (int idIngrediente, IIngredienteService ingredienteService) =>
        {
            var ingrediente = await ingredienteService.GetByIdAsync(idIngrediente);
            return ingrediente is null ? Results.NotFound() : Results.Ok(ingrediente);
        });

        group.MapPost("/", async (CrearIngredienteRequest request, IIngredienteService ingredienteService) =>
        {
            var ingrediente = await ingredienteService.CreateAsync(request);
            return Results.Created($"/ingredientes/{ingrediente.IdIngrediente}", ingrediente);
        });

        group.MapPut("/{idIngrediente:int}", async (int idIngrediente, ActualizarIngredienteRequest request, IIngredienteService ingredienteService) =>
        {
            var actualizado = await ingredienteService.UpdateAsync(idIngrediente, request);
            return actualizado ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{idIngrediente:int}", async (int idIngrediente, IIngredienteService ingredienteService) =>
        {
            var eliminado = await ingredienteService.DeleteAsync(idIngrediente);
            return eliminado ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{idIngrediente:int}/imagen", async (int idIngrediente, IFormFile archivo, IIngredienteService ingredienteService, ImagenStorageService storage) =>
        {
            try
            {
                var imagenUrl = await storage.GuardarAsync(archivo, "ingredientes");
                var ingrediente = await ingredienteService.SetImagenAsync(idIngrediente, imagenUrl);
                return ingrediente is null ? Results.NotFound() : Results.Ok(ingrediente);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).DisableAntiforgery();
    }
}
