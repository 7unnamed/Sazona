using Auth.Application.Interfaces;
using Auth.Domain;
using Auth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AuthDbContext _dbContext;

    public UsuarioRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Usuario?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Usuarios.AnyAsync(u => u.Username == username || u.Email == email, cancellationToken);
    }

    public void Add(Usuario usuario) => _dbContext.Usuarios.Add(usuario);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
