using Microsoft.AspNetCore.Http.HttpResults;
using Refugio.Api.Filters;
using Refugio.Application.DTOs.Auth;
using Refugio.Application.UseCases.Auth;

namespace Refugio.Api.Endpoints;

public static class AuthEndpoints
{
    /// <summary>Rotas públicas em /auth (sem /api/v1), como no contrato.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/auth").WithTags("Autenticação").AllowAnonymous();

        grupo.MapPost("/registrar", RegistrarUsuario)
            .WithName("Registrar")
            .WithSummary("Cria a conta de um idoso")
            .WithDescription("O cadastro cria sempre o papel IDOSO. Contas de gestor vêm do seed.")
            .ComValidacao<RegistrarRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/login", Entrar)
            .WithName("Login")
            .WithSummary("Autentica e devolve o JWT e os dados do usuário")
            .ComValidacao<LoginRequest>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<Created<UsuarioResumo>> RegistrarUsuario(
        RegistrarRequest request, Registrar useCase, CancellationToken ct) =>
        TypedResults.Created("/api/v1/usuarios/me", await useCase.ExecutarAsync(request, ct));

    private static async Task<Ok<LoginResponse>> Entrar(LoginRequest request, Login useCase, CancellationToken ct) =>
        TypedResults.Ok(await useCase.ExecutarAsync(request, ct));
}
