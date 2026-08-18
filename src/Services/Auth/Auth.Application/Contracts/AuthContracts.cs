using Auth.Domain.Enums;

namespace Auth.Application.Contracts;

public record RegistrarUsuarioRequest(string Username, string Email, string Password);

public record LoginRequest(string Username, string Password);

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record UsuarioResponse(int IdUsuario, string Username, string Email, RolUsuario Rol);

public record LoginResponse(
    string Token,
    DateTime ExpiraEn,
    string RefreshToken,
    DateTime RefreshTokenExpiraEn,
    UsuarioResponse Usuario);
