using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.DTOs.Cep;
using Refugio.Application.Interfaces;
using Refugio.Infrastructure;
using Refugio.Infrastructure.Data;

namespace Refugio.IntegrationTests.Infra;

/// <summary>
/// Sobe a API real contra o mesmo Oracle da aplicação, mas com tabelas próprias (prefixo T_RFGTST_),
/// criadas no início e apagadas no fim da execução. As tabelas do app (T_RFG_*) e as demais do schema
/// não são tocadas. O ViaCEP é trocado por um dublê para os testes não dependerem da internet.
///
/// A connection string vem dos user-secrets da API (desenvolvimento) ou da variável
/// ConnectionStrings__Refugio (CI).
/// </summary>
public sealed class RefugioApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SenhaUsuarios = "Senha@123";
    private const string PrefixoDosTestes = "T_RFGTST_";

    private readonly string _connectionString = LerConnectionString();
    private readonly ConfiguracaoBanco _banco = new(PrefixoDosTestes);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Refugio", _connectionString);
        builder.UseSetting("Database:PrefixoTabelas", PrefixoDosTestes);
        builder.UseSetting("Database:AplicarMigracoesAoIniciar", "false");
        builder.UseSetting("Jwt:Chave", "chave-dos-testes-de-integracao-com-mais-de-32-bytes");
        builder.UseSetting("Seed:SenhaUsuarios", SenhaUsuarios);
        // Limites altos: os testes fazem muitas chamadas seguidas. O 429 tem teste próprio com limite baixo.
        builder.UseSetting("RateLimit:Global:Requisicoes", "100000");
        builder.UseSetting("RateLimit:Autenticacao:Requisicoes", "100000");
        builder.UseSetting("RateLimit:Reservas:Requisicoes", "100000");

        builder.ConfigureTestServices(services => services.AddSingleton<ICepService, CepFake>());
    }

    public async Task InitializeAsync()
    {
        await using var db = NovoContexto();
        await ApagarTabelasDeTesteAsync(db); // sobras de uma execução interrompida
        await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
        await SeedData.SemearAsync(db, SenhaUsuarios, CancellationToken.None);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await using var db = NovoContexto();
        await ApagarTabelasDeTesteAsync(db);
    }

    private RefugioDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<RefugioDbContext>().UsarOracle(_connectionString, _banco).Options, _banco);

    /// <summary>Apaga só tabelas com o prefixo exato dos testes.</summary>
    private static async Task ApagarTabelasDeTesteAsync(RefugioDbContext db)
    {
        if (db.Banco.PrefixoTabelas != PrefixoDosTestes)
            throw new InvalidOperationException("Proteção: só tabelas de teste podem ser apagadas.");

        var tabelas = await db.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS \"Value\" FROM USER_TABLES WHERE TABLE_NAME LIKE 'T\\_RFGTST\\_%' ESCAPE '\\'")
            .ToListAsync();

        foreach (var tabela in tabelas.Where(t => t.StartsWith(PrefixoDosTestes, StringComparison.Ordinal)))
            await db.Database.ExecuteSqlRawAsync($"DROP TABLE \"{tabela}\" CASCADE CONSTRAINTS PURGE");
    }

    private static string LerConnectionString() =>
        new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build()
            .GetConnectionString("Refugio")
        ?? throw new InvalidOperationException(
            "Testes de integração precisam do Oracle: configure os user-secrets da API (ConnectionStrings:Refugio) " +
            "ou a variável de ambiente ConnectionStrings__Refugio.");
}

/// <summary>ViaCEP de mentira: conhece o CEP do exemplo do contrato e o da Praça da Sé (usado pelo health check).</summary>
internal sealed class CepFake : ICepService
{
    public Task<CepResponse?> ConsultarAsync(string cepSomenteDigitos, CancellationToken ct) =>
        Task.FromResult(cepSomenteDigitos switch
        {
            "03601000" => new CepResponse("03601-000", "Rua Exemplo", "Penha", "São Paulo", "SP"),
            "01001000" => new CepResponse("01001-000", "Praça da Sé", "Sé", "São Paulo", "SP"),
            _ => null,
        });
}

[CollectionDefinition(Nome)]
public sealed class ApiCollection : ICollectionFixture<RefugioApiFactory>
{
    public const string Nome = "api";
}
