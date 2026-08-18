using Auth.Domain;

namespace Auth.Application.Interfaces;

public record JwtToken(string Token, DateTime ExpiraEn);

public interface IJwtTokenGenerator
{
    JwtToken GenerateToken(Usuario usuario);
}
