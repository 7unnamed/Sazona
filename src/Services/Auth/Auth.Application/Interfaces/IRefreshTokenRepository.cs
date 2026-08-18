using Auth.Domain;

namespace Auth.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    void Add(RefreshToken refreshToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
