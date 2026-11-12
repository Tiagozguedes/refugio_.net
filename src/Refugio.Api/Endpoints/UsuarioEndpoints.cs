using Microsoft.AspNetCore.Http.HttpResults;
using Refugio.Api.Filters;
using Refugio.Application.DTOs.Usuarios;
using Refugio.Application.UseCases.Usuarios;

namespace Refugio.Api.Endpoints;

public static class UsuarioEndpoints
{
    public static RouteGroupBuilder MapUsuarioEndpoints(this RouteGroupBuilder api)
    {
        var grupo = api.MapGroup("/usuarios").WithTags("Usuário").RequireAuthorization();

        grupo.MapGet("/me", Obter)
            .WithName("ObterMeuPerfil")
            .WithSummary("Dados do usuário logado, com bairro e contato de emergência");

        grupo.MapPut("/me", Atualizar)
            .WithName("AtualizarMeuPerfil")
            .WithSummary("Atualiza nome, bairro, telefone e contato de emergência")
            .WithDescription("E-mail e papel não mudam por aqui. Campos opcionais vazios são gravados como nulos.")
            .ComValidacao<AtualizarUsuarioRequest>();

        return api;
    }

    private static async Task<Ok<UsuarioResponse>> Obter(ObterMeuPerfil useCase, CancellationToken ct) =>
        TypedResults.Ok(await useCase.ExecutarAsync(ct));

    private static async Task<Ok<UsuarioResponse>> Atualizar(
        AtualizarUsuarioRequest request, AtualizarMeuPerfil useCase, CancellationToken ct) =>
        TypedResults.Ok(await useCase.ExecutarAsync(request, ct));
}
