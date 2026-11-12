using Microsoft.AspNetCore.Http;
using Refugio.Application.Interfaces;
using Refugio.Domain.Enums;
using Refugio.Domain.Exceptions;

namespace Refugio.Infrastructure.Auth;

/// <summary>Lê o usuário das claims do JWT. O id do idoso nunca vem do corpo da requisição.</summary>
internal sealed class UsuarioAtual(IHttpContextAccessor accessor) : IUsuarioAtual
{
    public long Id =>
        long.TryParse(accessor.HttpContext?.User.FindFirst(ClaimsDoToken.Id)?.Value, out var id)
            ? id
            : throw SemSessao();

    public Papel Papel =>
        accessor.HttpContext?.User.FindFirst(ClaimsDoToken.Papel)?.Value switch
        {
            Politicas.PapelGestor => Papel.Gestor,
            Politicas.PapelIdoso => Papel.Idoso,
            _ => throw SemSessao(),
        };

    private static NaoAutenticadoException SemSessao() =>
        new("Não autenticado", "Faça login para continuar.");
}
