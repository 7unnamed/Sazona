using Auth.Application.Contracts;
using Auth.Application.Interfaces;
using Auth.Domain;
using Auth.Domain.Enums;

namespace Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
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
            Rol = RolUsuario.Usuario
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

        var token = _jwtTokenGenerator.GenerateToken(usuario);
        return new LoginResponse(token.Token, token.ExpiraEn, ToResponse(usuario));
    }

    private static UsuarioResponse ToResponse(Usuario usuario) => new(
        usuario.IdUsuario,
        usuario.Username,
        usuario.Email,
        usuario.Rol);
}
