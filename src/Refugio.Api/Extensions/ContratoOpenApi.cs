using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Refugio.Application.DTOs;
using Refugio.Application.DTOs.Auth;
using Refugio.Application.DTOs.Cep;
using Refugio.Application.DTOs.Refugios;
using Refugio.Application.DTOs.Reservas;
using Refugio.Application.DTOs.Usuarios;

namespace Refugio.Api.Extensions;

/// <summary>
/// Ajustes no documento OpenAPI para ele refletir o contrato combinado com o app:
/// campos obrigatórios das requisições sem "null" e exemplos JSON iguais aos do plano Mobile.
/// </summary>
internal static class ContratoOpenApi
{
    public static Task AjustarSchema(OpenApiSchema schema, OpenApiSchemaTransformerContext contexto, CancellationToken _)
    {
        var tipo = contexto.JsonTypeInfo.Type;

        // Os DTOs de entrada usam tipos anuláveis para o validador dizer "informe o campo" quando ele falta.
        // No contrato, porém, os campos obrigatórios não aceitam null.
        if (tipo.Name.EndsWith("Request", StringComparison.Ordinal) && schema.Properties is not null && schema.Required is not null)
        {
            foreach (var nome in schema.Required)
            {
                if (!schema.Properties.TryGetValue(nome, out var propriedade) || propriedade is not OpenApiSchema campo)
                    continue;

                if (campo.Type is { } tipoJson)
                    campo.Type = tipoJson & ~JsonSchemaType.Null;
            }
        }

        if (Exemplos.TryGetValue(tipo, out var exemplo))
            schema.Examples = [JsonNode.Parse(exemplo)!];

        // Horários usam um conversor próprio ("08:00"); sem isto o schema ficaria sem tipo.
        if (tipo == typeof(TimeOnly) || tipo == typeof(TimeOnly?))
        {
            schema.Type = JsonSchemaType.String;
            schema.Pattern = "^([01][0-9]|2[0-3]):[0-5][0-9]$";
            schema.Examples = [JsonValue.Create("08:00")];
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Enum anulável num campo obrigatório (ex.: "tipo") sai como oneOf [null, $ref]. O .NET monta esse oneOf
    /// depois dos transformadores de schema, então a troca pela referência pura é feita no documento pronto.
    /// </summary>
    public static Task AjustarDocumento(OpenApiDocument documento, OpenApiDocumentTransformerContext _, CancellationToken __)
    {
        var schemas = documento.Components?.Schemas;
        if (schemas is null)
            return Task.CompletedTask;

        foreach (var (nomeSchema, item) in schemas.Where(s => s.Key.EndsWith("Request", StringComparison.Ordinal)))
        {
            if (item is not OpenApiSchema { Properties: { } propriedades, Required: { } obrigatorios })
                continue;

            foreach (var nome in obrigatorios)
            {
                if (propriedades.TryGetValue(nome, out var propriedade)
                    && propriedade is OpenApiSchema { OneOf: { } opcoes }
                    && opcoes.OfType<OpenApiSchemaReference>().FirstOrDefault() is { } referencia)
                {
                    propriedades[nome] = referencia;
                }
            }
        }

        return Task.CompletedTask;
    }

    private static readonly Dictionary<Type, string> Exemplos = new()
    {
        [typeof(RegistrarRequest)] = """{ "nome": "Maria Souza", "email": "maria@email.com", "senha": "Senha@123" }""",
        [typeof(LoginRequest)] = """{ "email": "maria@email.com", "senha": "Senha@123" }""",
        [typeof(UsuarioResumo)] = """{ "id": 12, "nome": "Maria Souza", "email": "maria@email.com", "papel": "IDOSO" }""",
        [typeof(LoginResponse)] = """
            {
              "token": "eyJhbGciOi...",
              "expiraEm": "2026-11-15T15:30:00-03:00",
              "usuario": { "id": 12, "nome": "Maria Souza", "email": "maria@email.com", "papel": "IDOSO" }
            }
            """,
        [typeof(RefugioRequest)] = """
            {
              "nome": "Biblioteca Municipal da Penha", "tipo": "BIBLIOTECA", "cep": "03601-000",
              "endereco": "Rua Exemplo, 100", "bairro": "Penha", "capacidade": 20,
              "abre": "08:00", "fecha": "20:00", "acessivel": true, "ativo": true
            }
            """,
        [typeof(RefugioResponse)] = """
            {
              "id": 3, "nome": "Biblioteca Municipal da Penha", "tipo": "BIBLIOTECA", "cep": "03601-000",
              "endereco": "Rua Exemplo, 100", "bairro": "Penha", "capacidade": 20, "vagasDisponiveis": 7,
              "abre": "08:00", "fecha": "20:00", "acessivel": true, "ativo": true
            }
            """,
        [typeof(PaginaResponse<RefugioResponse>)] = """
            {
              "itens": [
                {
                  "id": 3, "nome": "Biblioteca Municipal da Penha", "tipo": "BIBLIOTECA", "cep": "03601-000",
                  "endereco": "Rua Exemplo, 100", "bairro": "Penha", "capacidade": 20, "vagasDisponiveis": 7,
                  "abre": "08:00", "fecha": "20:00", "acessivel": true, "ativo": true
                }
              ],
              "pagina": 1, "tamanho": 10, "totalItens": 23, "totalPaginas": 3
            }
            """,
        [typeof(CepResponse)] = """{ "cep": "03601-000", "endereco": "Rua Exemplo", "bairro": "Penha", "cidade": "São Paulo", "uf": "SP" }""",
        [typeof(CriarReservaRequest)] = """{ "refugioId": 3, "dataReserva": "2026-11-15" }""",
        [typeof(ReservaResponse)] = """
            {
              "id": 41, "refugioId": 3, "refugioNome": "Biblioteca Municipal da Penha",
              "refugioEndereco": "Rua Exemplo, 100 — Penha", "dataReserva": "2026-11-15",
              "status": "ATIVA", "criadoEm": "2026-11-14T10:02:11-03:00"
            }
            """,
        [typeof(UsuarioResponse)] = """
            {
              "id": 12, "nome": "Maria Souza", "email": "maria@email.com", "papel": "IDOSO", "bairro": "Penha",
              "telefone": "(11) 98888-0000", "contatoEmergenciaNome": "João Souza (filho)",
              "contatoEmergenciaTelefone": "(11) 97777-0000"
            }
            """,
        [typeof(AtualizarUsuarioRequest)] = """
            {
              "nome": "Maria Souza", "bairro": "Penha", "telefone": "(11) 98888-0000",
              "contatoEmergenciaNome": "João Souza (filho)", "contatoEmergenciaTelefone": "(11) 97777-0000"
            }
            """,
        [typeof(HttpValidationProblemDetails)] = """
            { "title": "Dados inválidos", "status": 400, "errors": { "capacidade": ["A capacidade deve ser maior que zero."] } }
            """,
        [typeof(ProblemDetails)] = """
            { "title": "Sem vaga", "status": 409, "detail": "Este refúgio não tem mais vagas para 15/11." }
            """,
    };
}
