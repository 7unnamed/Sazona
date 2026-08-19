using Auth.Application.Contracts;
using Auth.Application.Interfaces;
using Auth.Domain;
using Auth.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IConfiguration configuration)
    {
        _usuarioRepository = usuarioRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _configuration = configuration;
    }

    public async Task<UsuarioResponse> RegisterAsync(RegistrarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        var yaExiste = await _usuarioRepository.ExistsByUsernameOrEmailAsync(request.Username, request.Email, cancellationToken);
        if (yaExiste)
        {
            throw new InvalidOperationException("Ya existe un usuario con ese username o email.");
        }

        var usuario = new Usuario
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Rol = RolUsuario.Cliente
        };

        _usuarioRepository.Add(usuario);
        await _usuarioRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(usuario);
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (usuario is null || !_passwordHasher.Verify(request.Password, usuario.PasswordHash))
        {
            return null;
        }

        return await IssueTokensAsync(usuario, cancellationToken);
    }

    public async Task<LoginResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.Hash(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null || refreshToken.RevokedAt is not null || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        var nuevoRefreshToken = _refreshTokenGenerator.Generate();
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByTokenHash = nuevoRefreshToken.TokenHash;

        var usuario = refreshToken.Usuario;
        return await IssueTokensAsync(usuario, cancellationToken, nuevoRefreshToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.Hash(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (refreshToken is not null && refreshToken.RevokedAt is null)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<LoginResponse> IssueTokensAsync(Usuario usuario, CancellationToken cancellationToken, GeneratedRefreshToken? refreshToken = null)
    {
        var jwt = _jwtTokenGenerator.GenerateToken(usuario);
        var generated = refreshToken ?? _refreshTokenGenerator.Generate();
        var refreshExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
        var refreshExpiraEn = DateTime.UtcNow.AddDays(refreshExpirationDays);

        _refreshTokenRepository.Add(new RefreshToken
        {
            IdUsuario = usuario.IdUsuario,
            TokenHash = generated.TokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiraEn
        });

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new LoginResponse(jwt.Token, jwt.ExpiraEn, generated.RawToken, refreshExpiraEn, ToResponse(usuario));
    }

    private static UsuarioResponse ToResponse(Usuario usuario) => new(
        usuario.IdUsuario,
        usuario.Username,
        usuario.Email,
        usuario.Rol);
}
