using BuildingBlocks.Application.Interfaces;
using Meals.Application.Contracts;
using Meals.Application.Interfaces;
using Meals.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;

namespace Meals.Api.Endpoints;

public static class PlatoEndpoints
{
    public static void MapPlatoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platos").WithTags("Platos").RequireAuthorization();

        group.MapGet("/", async (IPlatoService platoService) =>
            Results.Ok(await platoService.GetAllAsync()));

        group.MapGet("/buscar", async (string nombre, IPlatoService platoService) =>
            Results.Ok(await platoService.SearchByNombreAsync(nombre)));

        group.MapGet("/aleatorio", async (IPlatoService platoService) =>
        {
            var plato = await platoService.GetRandomAsync();
            return plato is null ? Results.NotFound() : Results.Ok(plato);
        });

        group.MapGet("/descubrir", async (IPlatoService platoService, ICurrentUserService currentUserService) =>
        {
            var idUsuario = int.Parse(currentUserService.UserId!);
            return Results.Ok(await platoService.GetNoCocinadosAsync(idUsuario));
        });

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
        }).RequireAuthorization(p => p.RequireRole("Administrador"));

        group.MapPut("/{idPlato:int}", async (int idPlato, ActualizarPlatoRequest request, IPlatoService platoService) =>
        {
            var actualizado = await platoService.UpdateAsync(idPlato, request);
            return actualizado ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(p => p.RequireRole("Administrador"));

        group.MapDelete("/{idPlato:int}", async (int idPlato, IPlatoService platoService) =>
        {
            var eliminado = await platoService.DeleteAsync(idPlato);
            return eliminado ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(p => p.RequireRole("Administrador"));

        group.MapPost("/{idPlato:int}/imagen", async (int idPlato, IFormFile archivo, IPlatoService platoService, ImagenStorageService storage) =>
        {
            try
            {
                var imagenUrl = await storage.GuardarAsync(archivo, "platos");
                var plato = await platoService.SetImagenAsync(idPlato, imagenUrl);
                return plato is null ? Results.NotFound() : Results.Ok(plato);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).RequireAuthorization(p => p.RequireRole("Administrador")).DisableAntiforgery();

        group.MapPost("/{idPlato:int}/cocinado", async (int idPlato, IPlatoService platoService, ICurrentUserService currentUserService) =>
        {
            var idUsuario = int.Parse(currentUserService.UserId!);
            var marcado = await platoService.MarcarCocinadoAsync(idPlato, idUsuario);
            return marcado ? Results.NoContent() : Results.NotFound();
        });
    }
}
