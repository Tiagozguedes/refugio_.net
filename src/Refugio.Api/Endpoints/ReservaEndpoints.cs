using Microsoft.AspNetCore.Http.HttpResults;
using Refugio.Api.Filters;
using Refugio.Application.DTOs.Reservas;
using Refugio.Application.UseCases.Reservas;
using Refugio.Infrastructure.Auth;

namespace Refugio.Api.Endpoints;

public static class ReservaEndpoints
{
    public static RouteGroupBuilder MapReservaEndpoints(this RouteGroupBuilder api)
    {
        var grupo = api.MapGroup("/reservas").WithTags("Reservas").RequireAuthorization(Politicas.Idoso);

        grupo.MapPost("/", Criar)
            .WithName("CriarReserva")
            .WithSummary("Reserva uma vaga no refúgio para a data (IDOSO)")
            .WithDescription("409 sem vaga ou com reserva no mesmo dia; 422 se o refúgio estiver inativo.")
            .ComValidacao<CriarReservaRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapGet("/minhas", ListarMinhas)
            .WithName("ListarMinhasReservas")
            .WithSummary("Reservas ativas do idoso, de hoje em diante, da mais próxima para a mais distante");

        grupo.MapGet("/{id:long}", Obter)
            .WithName("ObterReserva")
            .WithSummary("Comprovante de uma reserva do próprio idoso")
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapDelete("/{id:long}", Cancelar)
            .WithName("CancelarReserva")
            .WithSummary("Cancela uma reserva do próprio idoso")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Created<ReservaResponse>> Criar(
        CriarReservaRequest request, CriarReserva useCase, CancellationToken ct)
    {
        var reserva = await useCase.ExecutarAsync(request, Guid.NewGuid().ToString(), ct);
        return TypedResults.Created($"/api/v1/reservas/{reserva.Id}", reserva);
    }

    private static async Task<Ok<IReadOnlyList<ReservaResponse>>> ListarMinhas(ListarMinhasReservas useCase, CancellationToken ct) =>
        TypedResults.Ok(await useCase.ExecutarAsync(ct));

    private static async Task<Ok<ReservaResponse>> Obter(long id, ObterReserva useCase, CancellationToken ct) =>
        TypedResults.Ok(await useCase.ExecutarAsync(id, ct));

    private static async Task<NoContent> Cancelar(long id, CancelarReserva useCase, CancellationToken ct)
    {
        await useCase.ExecutarAsync(id, ct);
        return TypedResults.NoContent();
    }
}
