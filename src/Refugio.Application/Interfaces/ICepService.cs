using Refugio.Application.DTOs.Cep;

namespace Refugio.Application.Interfaces;

public interface ICepService
{
    /// <summary>
    /// Consulta o endereço de um CEP com 8 dígitos. Devolve null se o CEP não existe e
    /// lança ServicoIndisponivelException se o serviço não responder.
    /// </summary>
    Task<CepResponse?> ConsultarAsync(string cepSomenteDigitos, CancellationToken ct);
}
