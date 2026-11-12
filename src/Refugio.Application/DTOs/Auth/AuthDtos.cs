using Refugio.Domain.Entities;
using Refugio.Domain.Enums;

namespace Refugio.Application.DTOs.Auth;

/// <summary>Corpo de POST /auth/registrar. O cadastro cria sempre um IDOSO; gestores vêm do seed.</summary>
public sealed record RegistrarRequest(string? Nome, string? Email, string? Senha);

public sealed record LoginRequest(string? Email, string? Senha);

public sealed record UsuarioResumo(long Id, string Nome, string Email, Papel Papel)
{
    public static UsuarioResumo De(Usuario u) => new(u.Id, u.Nome, u.Email, u.Papel);
}

public sealed record LoginResponse(string Token, DateTimeOffset ExpiraEm, UsuarioResumo Usuario);
