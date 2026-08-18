using Auth.Domain;

namespace Auth.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default);
    void Add(Usuario usuario);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
