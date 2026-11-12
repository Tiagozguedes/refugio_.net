using Microsoft.EntityFrameworkCore;
using Refugio.Api.Extensions;
using Refugio.Api.Middlewares;
using Refugio.Application;
using Refugio.Infrastructure;
using Refugio.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddContratoJson();
builder.Services.AddDocumentacao();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.MapHealthChecks("/health");

app.Run();
