using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Interfaces;
using Refugio.Infrastructure.Data;
using Refugio.Infrastructure.External.ViaCep;
using Refugio.Infrastructure.Repositories;

namespace Refugio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Refugio")
            ?? throw new InvalidOperationException(
                "Connection string 'Refugio' não configurada. Use user-secrets ou a variável ConnectionStrings__Refugio.");

        var banco = new ConfiguracaoBanco(configuration["Database:PrefixoTabelas"] ?? ConfiguracaoBanco.PrefixoPadrao);
        var senhaUsuariosDeTeste = configuration["Seed:SenhaUsuarios"] ?? "Senha@123";

        services.AddSingleton(banco);
        services.AddDbContext<RefugioDbContext>(options => options
            .UsarOracle(connectionString, banco)
            .UseSeeding((db, _) => SeedData.Semear((RefugioDbContext)db, senhaUsuariosDeTeste))
            .UseAsyncSeeding((db, _, ct) => SeedData.SemearAsync((RefugioDbContext)db, senhaUsuariosDeTeste, ct)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRefugioRepository, RefugioRepository>();
        services.AddScoped<IReservaRepository, ReservaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        services.AddHttpClient<ICepService, ViaCepClient>(http =>
        {
            http.BaseAddress = new Uri(configuration["ViaCep:BaseUrl"] ?? "https://viacep.com.br/");
            http.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }

    /// <summary>Oracle 19c (o da FIAP), com a tabela de histórico de migrações também prefixada.</summary>
    public static DbContextOptionsBuilder UsarOracle(this DbContextOptionsBuilder options, string connectionString, ConfiguracaoBanco banco) =>
        options
            .UseOracle(connectionString, oracle => oracle
                .UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)
                .MigrationsHistoryTable(banco.TabelaHistoricoMigracoes))
            .ReplaceService<IModelCacheKeyFactory, PrefixoModelCacheKeyFactory>();

    public static DbContextOptionsBuilder<T> UsarOracle<T>(this DbContextOptionsBuilder<T> options, string connectionString, ConfiguracaoBanco banco)
        where T : DbContext
    {
        ((DbContextOptionsBuilder)options).UsarOracle(connectionString, banco);
        return options;
    }
}
