using Microsoft.AspNetCore.Http.HttpResults;
using Refugio.Api.Filters;
using Refugio.Application.DTOs;
using Refugio.Application.DTOs.Refugios;
using Refugio.Application.UseCases.Refugios;
using Refugio.Infrastructure.Auth;

namespace Refugio.Api.Endpoints;

public static class RefugioEndpoints
{
    public static RouteGroupBuilder MapRefugioEndpoints(this RouteGroupBuilder api)
    {
        // Leitura para qualquer usuário logado; escrita só para GESTOR.
        var grupo = api.MapGroup("/refugios").WithTags("Refúgios").RequireAuthorization();

        grupo.MapGet("/", Listar)
            .WithName("ListarRefugios")
            .WithSummary("Lista refúgios com filtro por bairro e data, paginada")
            .WithDescription("Sem `data`, as vagas disponíveis são as de hoje (fuso de São Paulo). `tamanho` vai de 1 a 50.")
            .ComValidacao<ListarRefugiosQuery>();

        grupo.MapGet("/{id:long}", Obter)
            .WithName("ObterRefugio")
            .WithSummary("Detalhe de um refúgio, com as vagas da data informada")
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPost("/", Criar)
            .WithName("CriarRefugio")
            .WithSummary("Cadastra um refúgio (GESTOR)")
            .RequireAuthorization(Politicas.Gestor)
            .ComValidacao<RefugioRequest>();

        grupo.MapPut("/{id:long}", Atualizar)
            .WithName("AtualizarRefugio")
            .WithSummary("Atualiza um refúgio (GESTOR)")
            .RequireAuthorization(Politicas.Gestor)
            .ComValidacao<RefugioRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapDelete("/{id:long}", Excluir)
            .WithName("ExcluirRefugio")
            .RequireAuthorization(Politicas.Gestor)
            .WithSummary("Exclui um refúgio sem reservas ativas futuras (GESTOR)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<Ok<PaginaResponse<RefugioResponse>>> Listar(
        [AsParameters] ListarRefugiosQuery query, ListarRefugios useCase, CancellationToken ct) =>
        TypedResults.Ok(await useCase.ExecutarAsync(query, ct));

    private static async Task<Ok<RefugioResponse>> Obter(
        long id, DateOnly? data, ObterRefugio useCase, CancellationToken ct) =>
        TypedResults.Ok(await useCase.ExecutarAsync(id, data, ct));

    private static async Task<Created<RefugioResponse>> Criar(
        RefugioRequest request, CriarRefugio useCase, CancellationToken ct)
    {
        var refugio = await useCase.ExecutarAsync(request, ct);
        return TypedResults.Created($"/api/v1/refugios/{refugio.Id}", refugio);
    }

    private static async Task<Ok<RefugioResponse>> Atualizar(
        long id, RefugioRequest request, AtualizarRefugio useCase, CancellationToken ct) =>
        TypedResults.Ok(await useCase.ExecutarAsync(id, request, ct));

    private static async Task<NoContent> Excluir(long id, ExcluirRefugio useCase, CancellationToken ct)
    {
        await useCase.ExecutarAsync(id, ct);
        return TypedResults.NoContent();
    }
}
