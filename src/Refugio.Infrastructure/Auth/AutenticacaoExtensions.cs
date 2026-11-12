using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Refugio.Application.Interfaces;

namespace Refugio.Infrastructure.Auth;

public static class Politicas
{
    public const string Gestor = "Gestor";
    public const string Idoso = "Idoso";

    public const string PapelGestor = "GESTOR";
    public const string PapelIdoso = "IDOSO";
}

public static class AutenticacaoExtensions
{
    public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Secao))
            .Validate(o => o.Chave is { Length: >= 32 }, "Configure Jwt:Chave com pelo menos 32 caracteres.")
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddSingleton<ISenhaHasher, SenhaHasher>();
        services.AddSingleton<IGeradorDeToken, GeradorDeTokenJwt>();
        services.AddScoped<IUsuarioAtual, UsuarioAtual>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Emissor,
                    ValidAudience = jwt.Value.Audiencia,
                    IssuerSigningKey = jwt.Value.ChaveDeAssinatura(),
                    NameClaimType = ClaimsDoToken.Nome,
                    RoleClaimType = ClaimsDoToken.Papel,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                bearer.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        var expirou = context.AuthenticateFailure is SecurityTokenExpiredException;
                        await EscreverProblema(context.HttpContext, StatusCodes.Status401Unauthorized, "Não autenticado",
                            expirou ? "Sua sessão expirou. Faça login novamente." : "Faça login para continuar.");
                    },
                    OnForbidden = context => EscreverProblema(context.HttpContext, StatusCodes.Status403Forbidden,
                        "Acesso negado", "Você não tem permissão para isso."),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politicas.Gestor, p => p.RequireRole(Politicas.PapelGestor))
            .AddPolicy(Politicas.Idoso, p => p.RequireRole(Politicas.PapelIdoso));

        return services;
    }

    private static async Task EscreverProblema(HttpContext http, int status, string titulo, string detalhe)
    {
        http.Response.StatusCode = status;
        var problemDetails = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails { Status = status, Title = titulo, Detail = detalhe },
        });
    }
}
