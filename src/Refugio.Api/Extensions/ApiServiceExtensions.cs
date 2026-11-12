using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi;

namespace Refugio.Api.Extensions;

public static class ApiServiceExtensions
{
    /// <summary>
    /// Formatos combinados no contrato: camelCase, enums em maiúsculas (CENTRO_COMUNITARIO),
    /// datas yyyy-MM-dd e horários HH:mm.
    /// </summary>
    public static IServiceCollection AddContratoJson(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));
            options.SerializerOptions.Converters.Add(new HoraJsonConverter());
        });

        // Corpo ou parâmetro inválido vira exceção e passa pelo mesmo tratamento de erros (ProblemDetails).
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        return services;
    }

    public static IServiceCollection AddDocumentacao(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer((documento, _, _) =>
        {
            documento.Info = new OpenApiInfo
            {
                Title = "Refúgio API",
                Version = "v1",
                Description =
                    "API do Refúgio: o idoso encontra e reserva vaga em um refúgio climático durante ondas de calor, " +
                    "e o gestor mantém os refúgios. Todo erro é devolvido como ProblemDetails.",
            };
            return Task.CompletedTask;
        }));

        return services;
    }
}
