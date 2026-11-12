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

        return services;
    }
}
