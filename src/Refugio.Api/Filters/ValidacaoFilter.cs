using System.Text.Json;
using FluentValidation;

namespace Refugio.Api.Filters;

/// <summary>
/// Valida o argumento do tipo <typeparamref name="T"/> antes do endpoint.
/// Falha vira 400 ValidationProblemDetails com os campos em camelCase, como o app lê de "errors".
/// </summary>
public sealed class ValidacaoFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argumento = context.Arguments.OfType<T>().FirstOrDefault();
        if (argumento is null)
            return await next(context);

        var resultado = await validator.ValidateAsync(argumento, context.HttpContext.RequestAborted);
        if (resultado.IsValid)
            return await next(context);

        var erros = resultado.Errors
            .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return TypedResults.ValidationProblem(erros, title: "Dados inválidos");
    }
}

public static class ValidacaoFilterExtensions
{
    public static RouteHandlerBuilder ComValidacao<T>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidacaoFilter<T>>().ProducesValidationProblem();
}
