using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
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
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((documento, _, _) =>
            {
                documento.Info = new OpenApiInfo
                {
                    Title = "Refúgio API",
                    Version = "v1",
                    Description =
                        "API do Refúgio: o idoso encontra e reserva vaga em um refúgio climático durante ondas de calor, " +
                        "e o gestor mantém os refúgios. Todo erro é devolvido como ProblemDetails. " +
                        "Para testar: faça POST /auth/login, copie o token e use o botão Authorize.",
                };
                documento.Components ??= new OpenApiComponents();
                documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                documento.Components.SecuritySchemes[EsquemaBearer] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Token devolvido por POST /auth/login.",
                };
                return Task.CompletedTask;
            });

            // Só as operações protegidas pedem o token e documentam 401/403.
            options.AddOperationTransformer((operacao, contexto, _) =>
            {
                var metadados = contexto.Description.ActionDescriptor.EndpointMetadata;
                var protegida = metadados.OfType<IAuthorizeData>().Any() && !metadados.OfType<IAllowAnonymous>().Any();
                if (!protegida)
                    return Task.CompletedTask;

                operacao.Security =
                [
                    new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(EsquemaBearer, contexto.Document)] = [] },
                ];
                operacao.Responses ??= new OpenApiResponses();
                operacao.Responses.TryAdd("401", new OpenApiResponse { Description = "Token ausente, inválido ou expirado." });
                operacao.Responses.TryAdd("403", new OpenApiResponse { Description = "O papel do usuário não permite esta operação." });
                return Task.CompletedTask;
            });
        });

        return services;
    }

    private const string EsquemaBearer = "Bearer";
}
