using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Refugio.Application.Common;
using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;

namespace Refugio.Infrastructure.Auth;

internal sealed class GeradorDeTokenJwt(IOptions<JwtOptions> options, TimeProvider relogio) : IGeradorDeToken
{
    private static readonly JsonWebTokenHandler Handler = new();

    public (string Token, DateTimeOffset ExpiraEm) Gerar(Usuario usuario)
    {
        var config = options.Value;
        var agora = relogio.AgoraUtcEmSegundos();
        var expiraEm = agora.AddMinutes(config.ExpiracaoMinutos);

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = config.Emissor,
            Audience = config.Audiencia,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            SigningCredentials = new SigningCredentials(config.ChaveDeAssinatura(), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
                new Claim(ClaimsDoToken.Papel, JsonNamingPolicy.SnakeCaseUpper.ConvertName(usuario.Papel.ToString())),
            ]),
        });

        return (token, expiraEm);
    }
}

public static class ClaimsDoToken
{
    /// <summary>Papel em maiúsculas (IDOSO, GESTOR), lido pelo app com um decode simples.</summary>
    public const string Papel = "role";
    public const string Nome = JwtRegisteredClaimNames.Name;
    public const string Id = JwtRegisteredClaimNames.Sub;
}
