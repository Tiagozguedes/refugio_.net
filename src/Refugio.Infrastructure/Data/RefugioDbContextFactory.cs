using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Refugio.Infrastructure.Data;

/// <summary>
/// Usada só pelo dotnet-ef para gerar migrações. Gerar migração não abre conexão,
/// então a string abaixo não tem credencial. Para aplicar, use REFUGIO_CONNECTION.
/// </summary>
public sealed class RefugioDbContextFactory : IDesignTimeDbContextFactory<RefugioDbContext>
{
    public RefugioDbContext CreateDbContext(string[] args)
    {
        var conexao = Environment.GetEnvironmentVariable("REFUGIO_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=refugio";

        var options = new DbContextOptionsBuilder<RefugioDbContext>().UseNpgsql(conexao).Options;
        return new RefugioDbContext(options);
    }
}
