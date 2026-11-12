using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Interfaces;
using Refugio.Infrastructure.Data;
using Refugio.Infrastructure.Repositories;

namespace Refugio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Refugio")
            ?? throw new InvalidOperationException(
                "Connection string 'Refugio' não configurada. Use user-secrets ou a variável ConnectionStrings__Refugio.");

        services.AddDbContext<RefugioDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSeeding((db, _) => SeedData.Semear((RefugioDbContext)db))
            .UseAsyncSeeding((db, _, ct) => SeedData.SemearAsync((RefugioDbContext)db, ct)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRefugioRepository, RefugioRepository>();
        services.AddScoped<IReservaRepository, ReservaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        return services;
    }
}
