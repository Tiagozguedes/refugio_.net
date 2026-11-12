using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Refugio.Infrastructure.Data;

/// <summary>
/// O schema Oracle é compartilhado com outros trabalhos, então toda tabela, índice e restrição do Refúgio
/// leva um prefixo próprio (T_RFG_USUARIO, PK_T_RFG_USUARIO...). Os testes de integração usam outro prefixo.
/// </summary>
public sealed record ConfiguracaoBanco(string PrefixoTabelas)
{
    public const string PrefixoPadrao = "T_RFG_";

    public string TabelaHistoricoMigracoes => $"{PrefixoTabelas}EF_MIGRACOES";
}

/// <summary>O EF guarda o modelo em cache por tipo de contexto; com prefixo configurável, a chave inclui o prefixo.</summary>
internal sealed class PrefixoModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), (context as RefugioDbContext)?.Banco.PrefixoTabelas, designTime);
}
