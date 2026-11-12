using Refugio.Application.Common;
using Refugio.Application.DTOs.Cep;
using Refugio.Application.Interfaces;
using Refugio.Domain.Exceptions;

namespace Refugio.Application.UseCases.Cep;

public sealed class ConsultarCep(ICepService cepService)
{
    public async Task<CepResponse> ExecutarAsync(string cep, CancellationToken ct)
    {
        if (!CepFormato.TentarNormalizar(cep, out var formatado))
            throw new DadosInvalidosException("cep", "O CEP deve ter 8 dígitos, como 03601-000.");

        return await cepService.ConsultarAsync(CepFormato.SomenteDigitos(formatado), ct)
            ?? throw new RecursoNaoEncontradoException("CEP não encontrado", $"Não encontramos o CEP {formatado}.");
    }
}
