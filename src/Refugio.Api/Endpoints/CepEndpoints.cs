using Microsoft.AspNetCore.Http.HttpResults;
using Refugio.Application.DTOs.Cep;
using Refugio.Application.UseCases.Cep;
using Refugio.Infrastructure.Auth;

namespace Refugio.Api.Endpoints;

public static class CepEndpoints
{
    public static RouteGroupBuilder MapCepEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/cep/{cep}", Consultar)
            .WithTags("CEP")
            .WithName("ConsultarCep")
            .WithSummary("Endereço de um CEP pelo ViaCEP, para preencher o formulário de refúgio (GESTOR)")
            .RequireAuthorization(Politicas.Gestor)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return api;
    }

    private static async Task<Ok<CepResponse>> Consultar(string cep, ConsultarCep useCase, CancellationToken ct) =>
        TypedResults.Ok(await useCase.ExecutarAsync(cep, ct));
}
