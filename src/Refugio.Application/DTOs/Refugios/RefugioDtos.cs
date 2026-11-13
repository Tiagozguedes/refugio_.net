namespace Refugio.Application.DTOs.Refugios;

using Refugio.Domain.Entities;
using Refugio.Domain.Enums;

/// <summary>Corpo de POST e PUT /api/v1/refugios.</summary>
public sealed record RefugioRequest(
    string? Nome,
    TipoRefugio? Tipo,
    string? Cep,
    string? Endereco,
    string? Bairro,
    int? Capacidade,
    TimeOnly? Abre,
    TimeOnly? Fecha,
    bool? Acessivel,
    bool? Ativo);

public sealed record RefugioResponse(
    long Id,
    string Nome,
    TipoRefugio Tipo,
    string Cep,
    string Endereco,
    string Bairro,
    int Capacidade,
    int VagasDisponiveis,
    TimeOnly Abre,
    TimeOnly Fecha,
    bool Acessivel,
    bool Ativo)
{
    public static RefugioResponse De(Refugio r, int vagasOcupadas) => new(
        r.Id, r.Nome, r.Tipo, r.Cep, r.Endereco, r.Bairro, r.Capacidade,
        r.VagasDisponiveis(vagasOcupadas), r.Abre, r.Fecha, r.Acessivel, r.Ativo);
}

/// <summary>
/// Filtros de GET /api/v1/refugios. Sem data, as vagas são as de hoje (São Paulo).
/// Ativo é opcional: ativo=true lista só os refúgios abertos (tela Início do idoso); sem ele, vêm todos.
/// </summary>
public sealed record ListarRefugiosQuery(string? Bairro, DateOnly? Data, int? Pagina, int? Tamanho, bool? Ativo = null)
{
    public const int TamanhoPadrao = 10;
    public const int TamanhoMaximo = 50;
}
