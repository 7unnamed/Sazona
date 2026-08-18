using Auth.Domain.Enums;

namespace Auth.Application.Contracts;

public record RegistrarUsuarioRequest(string Username, string Email, string Password);

public record LoginRequest(string Username, string Password);

public record UsuarioResponse(int IdUsuario, string Username, string Email, RolUsuario Rol);

public record LoginResponse(string Token, DateTime ExpiraEn, UsuarioResponse Usuario);
