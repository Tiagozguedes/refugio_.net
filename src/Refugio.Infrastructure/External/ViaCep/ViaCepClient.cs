using System.Net.Http.Json;
using System.Text.Json;
using Refugio.Application.DTOs.Cep;
using Refugio.Application.Interfaces;
using Refugio.Domain.Exceptions;

namespace Refugio.Infrastructure.External.ViaCep;

/// <summary>Cliente HTTP tipado do ViaCEP (https://viacep.com.br).</summary>
internal sealed class ViaCepClient(HttpClient http) : ICepService
{
    public async Task<CepResponse?> ConsultarAsync(string cepSomenteDigitos, CancellationToken ct)
    {
        ViaCepResposta? resposta;
        try
        {
            resposta = await http.GetFromJsonAsync<ViaCepResposta>($"ws/{cepSomenteDigitos}/json/", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            throw new ServicoIndisponivelException("Não foi possível consultar o CEP agora. Tente de novo em instantes.", ex);
        }

        // CEP inexistente: o ViaCEP responde 200 com { "erro": "true" } (ou true, nas versões antigas).
        if (resposta is null || resposta.CepInexistente)
            return null;

        return new CepResponse(resposta.Cep, resposta.Logradouro, resposta.Bairro, resposta.Localidade, resposta.Uf);
    }

    private sealed record ViaCepResposta(
        string Cep, string Logradouro, string Bairro, string Localidade, string Uf, JsonElement? Erro)
    {
        public bool CepInexistente =>
            Erro is { ValueKind: JsonValueKind.True }
            || Erro is { ValueKind: JsonValueKind.String } e && e.GetString() == "true";
    }
}
