using Microsoft.AspNetCore.Identity;
using Refugio.Application.Interfaces;

namespace Refugio.Infrastructure.Auth;

/// <summary>PBKDF2 com sal aleatório, via PasswordHasher do ASP.NET Core Identity.</summary>
internal sealed class SenhaHasher : ISenhaHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object SemUsuario = new();

    public string Gerar(string senha) => Hasher.HashPassword(SemUsuario, senha);

    public bool Verificar(string senhaHash, string senha) =>
        Hasher.VerifyHashedPassword(SemUsuario, senhaHash, senha) != PasswordVerificationResult.Failed;
}
