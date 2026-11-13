using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Refugio.Domain.Exceptions;

namespace Refugio.Api.Middlewares;

/// <summary>
/// Converte toda exceção em ProblemDetails, como combinado com o app:
/// "title" e "detail" para a mensagem, "errors" para marcar os campos.
/// </summary>
public sealed partial class TratamentoDeErrosMiddleware(RequestDelegate next, IProblemDetailsService problemDetails, ILogger<TratamentoDeErrosMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var problema = Converter(ex);
            if (problema.Status >= StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Erro ao processar {Metodo} {Caminho}", context.Request.Method, context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = problema.Status!.Value;
            await problemDetails.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problema, Exception = ex });
        }
    }

    private static ProblemDetails Converter(Exception ex) => ex switch
    {
        DadosInvalidosException e => Validacao(e.Campo, e.Message),
        RecursoNaoEncontradoException e => Problema(StatusCodes.Status404NotFound, e),
        ConflitoException e => Problema(StatusCodes.Status409Conflict, e),
        RegraDeNegocioException e => Problema(StatusCodes.Status422UnprocessableEntity, e),
        AcessoNegadoException e => Problema(StatusCodes.Status403Forbidden, e),
        NaoAutenticadoException e => Problema(StatusCodes.Status401Unauthorized, e),
        ServicoIndisponivelException e => Problema(StatusCodes.Status503ServiceUnavailable, e),
        BadHttpRequestException { InnerException: JsonException json } => JsonInvalido(json),
        BadHttpRequestException e when ParametroInvalido().Match(e.Message) is { Success: true } m =>
            // Com [AsParameters] o nome vem como a propriedade do record ("Data"); o app espera "data".
            Validacao(JsonNamingPolicy.CamelCase.ConvertName(m.Groups["nome"].Value), MensagemDoFormato(m.Groups["tipo"].Value)),
        BadHttpRequestException e when e.Message.StartsWith("Required parameter", StringComparison.Ordinal) =>
            Validacao("corpo", "Envie o corpo da requisição em JSON."),
        BadHttpRequestException e => new ProblemDetails
        {
            Status = e.StatusCode,
            Title = "Requisição inválida",
            Detail = e.StatusCode == StatusCodes.Status415UnsupportedMediaType
                ? "Envie o corpo com Content-Type: application/json."
                : "A requisição não pôde ser lida.",
        },
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro interno",
            Detail = "Ocorreu um erro inesperado. Tente novamente em instantes.",
        },
    };

    private static ProblemDetails Problema(int status, DomainException e) =>
        new() { Status = status, Title = e.Titulo, Detail = e.Message };

    private static HttpValidationProblemDetails Validacao(string campo, string mensagem) =>
        new(new Dictionary<string, string[]> { [campo] = [mensagem] })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Dados inválidos",
        };

    /// <summary>
    /// Parâmetro de rota, query ou header que não converte: o framework escreve
    /// 'Failed to bind parameter "Nullable&lt;DateOnly&gt; data" from "15-11-2026".' e daqui sai o campo "data".
    /// </summary>
    [GeneratedRegex("""^Failed to bind parameter "(?<tipo>.+) (?<nome>[\w-]+)" from""")]
    private static partial Regex ParametroInvalido();

    private static string MensagemDoFormato(string tipo) => tipo switch
    {
        _ when tipo.Contains("DateOnly") => "Data em formato inválido. Use yyyy-MM-dd, como 2026-11-15.",
        _ when tipo.Contains("Int") => "Informe um número inteiro.",
        _ when tipo.Contains("Boolean") => "Use true ou false.",
        _ => "Valor em formato inválido.",
    };

    /// <summary>JSON malformado: aponta o campo pelo caminho do erro ("$.abre" vira "abre").</summary>
    private static ProblemDetails JsonInvalido(JsonException json)
    {
        var campo = json.Path is { Length: > 2 } caminho ? caminho.TrimStart('$', '.') : "corpo";
        var mensagem = json.Path is { Length: > 2 } ? "Valor em formato inválido." : "O corpo da requisição não é um JSON válido.";
        return Validacao(campo, mensagem);
    }
}
