using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Refugio")
            ?? throw new InvalidOperationException("Connection string 'Refugio' is not configured.");

        services.AddDbContext<RefugioDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
