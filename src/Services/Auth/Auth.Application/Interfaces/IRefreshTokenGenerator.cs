namespace Auth.Application.Interfaces;

public record GeneratedRefreshToken(string RawToken, string TokenHash);

public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate();
    string Hash(string rawToken);
}
