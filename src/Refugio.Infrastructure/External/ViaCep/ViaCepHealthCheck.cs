using Microsoft.Extensions.Diagnostics.HealthChecks;
using Refugio.Application.Interfaces;
using Refugio.Domain.Exceptions;

namespace Refugio.Infrastructure.External.ViaCep;

/// <summary>Consulta um CEP conhecido (Praça da Sé) para saber se o ViaCEP está respondendo.</summary>
internal sealed class ViaCepHealthCheck(ICepService cepService) : IHealthCheck
{
    private const string CepConhecido = "01001000";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var endereco = await cepService.ConsultarAsync(CepConhecido, ct);
            return endereco is not null
                ? HealthCheckResult.Healthy("ViaCEP respondendo.")
                : new HealthCheckResult(context.Registration.FailureStatus, "ViaCEP respondeu, mas não encontrou um CEP conhecido.");
        }
        catch (ServicoIndisponivelException ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "ViaCEP não respondeu.", ex.InnerException);
        }
    }
}
