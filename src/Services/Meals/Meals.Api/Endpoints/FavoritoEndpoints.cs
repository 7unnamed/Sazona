using BuildingBlocks.Application.Interfaces;
using Meals.Application.Contracts;
using Meals.Application.Interfaces;

namespace Meals.Api.Endpoints;

public static class FavoritoEndpoints
{
    public static void MapFavoritoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/favoritos").WithTags("Favoritos").RequireAuthorization();

        group.MapGet("/", async (IFavoritoService favoritoService, ICurrentUserService currentUserService) =>
        {
            var idUsuario = int.Parse(currentUserService.UserId!);
            return Results.Ok(await favoritoService.GetAllAsync(idUsuario));
        });

        group.MapPost("/", async (AgregarFavoritoRequest request, IFavoritoService favoritoService, ICurrentUserService currentUserService) =>
        {
            var idUsuario = int.Parse(currentUserService.UserId!);
            try
            {
                var favorito = await favoritoService.AddAsync(idUsuario, request);
                return Results.Created($"/favoritos/{favorito.IdPlato}", favorito);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapDelete("/{idPlato:int}", async (int idPlato, IFavoritoService favoritoService, ICurrentUserService currentUserService) =>
        {
            var idUsuario = int.Parse(currentUserService.UserId!);
            var eliminado = await favoritoService.RemoveAsync(idUsuario, idPlato);
            return eliminado ? Results.NoContent() : Results.NotFound();
        });
    }
}
