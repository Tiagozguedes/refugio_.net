using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Refugio.Infrastructure.Data;
using Refugio.Infrastructure.External.ViaCep;

namespace Refugio.Infrastructure;

public static class HealthChecksExtensions
{
    /// <summary>Tag dos checks que só dizem se o processo está vivo (sem dependências).</summary>
    public const string TagVivo = "vivo";

    public static IServiceCollection AddVerificacoesDeSaude(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("api", () => HealthCheckResult.Healthy("API respondendo."), tags: [TagVivo])
            .AddDbContextCheck<RefugioDbContext>("banco", HealthStatus.Unhealthy)
            // Sem o ViaCEP só o preenchimento automático do formulário para; o resto da API segue funcionando.
            .AddCheck<ViaCepHealthCheck>("viacep", HealthStatus.Degraded, timeout: TimeSpan.FromSeconds(5));

        return services;
    }
}
