using Microsoft.EntityFrameworkCore;
using Refugio.Api.Endpoints;
using Refugio.Api.Extensions;
using Refugio.Api.Middlewares;
using Refugio.Application;
using Refugio.Infrastructure;
using Refugio.Infrastructure.Auth;
using Refugio.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddContratoJson();
builder.Services.AddDocumentacao();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAutenticacaoJwt(builder.Configuration);
builder.Services.AddLimitesDeRequisicao(builder.Configuration);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:AplicarMigracoesAoIniciar"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<RefugioDbContext>().Database.MigrateAsync();
}

app.UseMiddleware<TratamentoDeErrosMiddleware>();
app.UseStatusCodePages();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Refúgio API v1");
    options.DocumentTitle = "Refúgio API";
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
// Depois da autenticação, para o limite contar por usuário do token.
app.UseRateLimiter();

app.MapHealthChecks("/health").DisableRateLimiting();
app.MapAuthEndpoints();

var api = app.MapGroup("/api/v1");
api.MapRefugioEndpoints();
api.MapCepEndpoints();
api.MapReservaEndpoints();
api.MapUsuarioEndpoints();

app.Run();
