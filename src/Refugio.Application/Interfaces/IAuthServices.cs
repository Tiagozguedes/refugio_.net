using Refugio.Domain.Entities;
using Refugio.Domain.Enums;

namespace Refugio.Application.Interfaces;

public interface ISenhaHasher
{
    string Gerar(string senha);

    bool Verificar(string senhaHash, string senha);

    /// <summary>
    /// Hash válido que não pertence a ninguém. O login verifica a senha contra ele quando o e-mail não existe,
    /// para a resposta levar o mesmo tempo nos dois casos e não revelar quais e-mails estão cadastrados.
    /// </summary>
    string HashFicticio { get; }
}

public interface IGeradorDeToken
{
    /// <summary>Gera o JWT com sub, email, name e role. Devolve o token e o instante de expiração (UTC).</summary>
    (string Token, DateTimeOffset ExpiraEm) Gerar(Usuario usuario);
}

/// <summary>Usuário da requisição atual, lido das claims do token.</summary>
public interface IUsuarioAtual
{
    /// <summary>Lança NaoAutenticadoException se não houver usuário autenticado.</summary>
    long Id { get; }

    Papel Papel { get; }
}
