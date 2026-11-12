using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.DTOs.Cep;
using Refugio.Application.Interfaces;
using Testcontainers.PostgreSql;

namespace Refugio.IntegrationTests.Infra;

/// <summary>
/// Sobe a API real contra um PostgreSQL descartável (Testcontainers). As migrações e o seed rodam na
/// inicialização, como em Development. O ViaCEP é trocado por um dublê para os testes não dependerem da internet.
/// </summary>
public sealed class RefugioApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SenhaUsuarios = "Senha@123";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Refugio", _postgres.GetConnectionString());
        builder.UseSetting("Database:AplicarMigracoesAoIniciar", "true");
        builder.UseSetting("Jwt:Chave", "chave-dos-testes-de-integracao-com-mais-de-32-bytes");
        builder.UseSetting("Seed:SenhaUsuarios", SenhaUsuarios);
        // Limites altos: os testes fazem muitas chamadas seguidas. O 429 tem teste próprio com limite baixo.
        builder.UseSetting("RateLimit:Global:Requisicoes", "100000");
        builder.UseSetting("RateLimit:Autenticacao:Requisicoes", "100000");
        builder.UseSetting("RateLimit:Reservas:Requisicoes", "100000");

        builder.ConfigureTestServices(services => services.AddSingleton<ICepService, CepFake>());
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
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
