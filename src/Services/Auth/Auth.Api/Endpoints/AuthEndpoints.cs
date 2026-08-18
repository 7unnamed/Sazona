using Auth.Application.Contracts;
using Auth.Application.Interfaces;

namespace Auth.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/").WithTags("Auth");

        group.MapPost("/register", async (RegistrarUsuarioRequest request, IAuthService authService) =>
        {
            try
            {
                var usuario = await authService.RegisterAsync(request);
                return Results.Created($"/usuarios/{usuario.IdUsuario}", usuario);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        });

        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            var resultado = await authService.LoginAsync(request);
            return resultado is null ? Results.Unauthorized() : Results.Ok(resultado);
        });
    }
}
