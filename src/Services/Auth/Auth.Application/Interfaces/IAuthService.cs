using Auth.Application.Contracts;

namespace Auth.Application.Interfaces;

public interface IAuthService
{
    Task<UsuarioResponse> RegisterAsync(RegistrarUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
}
