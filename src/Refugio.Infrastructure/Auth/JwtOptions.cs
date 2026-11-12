using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Refugio.Infrastructure.Auth;

/// <summary>Seção "Jwt" da configuração. A chave nunca fica no repositório: vem de user-secrets ou de Jwt__Chave.</summary>
public sealed class JwtOptions
{
    public const string Secao = "Jwt";

    public string Emissor { get; set; } = "refugio-api";
    public string Audiencia { get; set; } = "refugio-app";
    public string? Chave { get; set; }
    public int ExpiracaoMinutos { get; set; } = 120;

    public SymmetricSecurityKey ChaveDeAssinatura()
    {
        var bytes = Encoding.UTF8.GetBytes(Chave ?? string.Empty);
        if (bytes.Length < 32)
            throw new InvalidOperationException(
                "Configure Jwt:Chave com pelo menos 32 caracteres (user-secrets em desenvolvimento ou a variável Jwt__Chave).");
        return new SymmetricSecurityKey(bytes);
    }
}
