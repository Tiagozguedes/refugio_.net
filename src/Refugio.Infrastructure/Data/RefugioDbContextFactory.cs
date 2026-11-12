using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Refugio.Infrastructure.Data;

/// <summary>
/// Usada só pelo dotnet-ef. Gerar migração ou script não abre conexão, então a string abaixo não tem
/// credencial. Para aplicar migrações por linha de comando, defina REFUGIO_CONNECTION.
/// </summary>
public sealed class RefugioDbContextFactory : IDesignTimeDbContextFactory<RefugioDbContext>
{
    public RefugioDbContext CreateDbContext(string[] args)
    {
        var conexao = Environment.GetEnvironmentVariable("REFUGIO_CONNECTION")
            ?? "Data Source=oracle.fiap.com.br:1521/ORCL";
        var banco = new ConfiguracaoBanco(ConfiguracaoBanco.PrefixoPadrao);

        var options = new DbContextOptionsBuilder<RefugioDbContext>()
            .UsarOracle(conexao, banco)
            .Options;
        return new RefugioDbContext(options, banco);
    }
}
