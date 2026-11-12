using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Refugio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Mensagens padrão do FluentValidation em português.
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSingleton(TimeProvider.System);
        AdicionarCasosDeUso(services);

        return services;
    }

    /// <summary>Registra cada classe pública de UseCases/ (ListarRefugios, CriarReserva...) como scoped.</summary>
    private static void AdicionarCasosDeUso(IServiceCollection services)
    {
        var casosDeUso = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true }
                && t.Namespace?.StartsWith("Refugio.Application.UseCases", StringComparison.Ordinal) == true);

        foreach (var tipo in casosDeUso)
            services.AddScoped(tipo);
    }
}
