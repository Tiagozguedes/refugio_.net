namespace Refugio.Application.UseCases.Refugios;

using Refugio.Application.Common;
using Refugio.Application.DTOs;
using Refugio.Application.DTOs.Refugios;
using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;
using Refugio.Domain.Exceptions;

internal static class RefugioErros
{
    public static RecursoNaoEncontradoException NaoEncontrado() =>
        new("Refúgio não encontrado", "Este refúgio não está mais disponível.");
}

public sealed class ListarRefugios(IRefugioRepository refugios, TimeProvider relogio)
{
    public async Task<PaginaResponse<RefugioResponse>> ExecutarAsync(ListarRefugiosQuery query, CancellationToken ct)
    {
        var pagina = query.Pagina ?? 1;
        var tamanho = query.Tamanho ?? ListarRefugiosQuery.TamanhoPadrao;
        var data = query.Data ?? relogio.HojeEmSaoPaulo();

        var (itens, total) = await refugios.ListarAsync(query.Bairro, query.Ativo, data, pagina, tamanho, ct);

        var respostas = itens.Select(i => RefugioResponse.De(i.Refugio, i.VagasOcupadas)).ToList();
        return PaginaResponse<RefugioResponse>.Criar(respostas, pagina, tamanho, total);
    }
}

public sealed class ObterRefugio(IRefugioRepository refugios, IReservaRepository reservas, TimeProvider relogio)
{
    public async Task<RefugioResponse> ExecutarAsync(long id, DateOnly? data, CancellationToken ct)
    {
        var refugio = await refugios.ObterPorIdAsync(id, ct) ?? throw RefugioErros.NaoEncontrado();
        var ocupadas = await reservas.ContarAtivasAsync(id, data ?? relogio.HojeEmSaoPaulo(), ct);
        return RefugioResponse.De(refugio, ocupadas);
    }
}

public sealed class CriarRefugio(IRefugioRepository refugios, IUnitOfWork unitOfWork)
{
    public async Task<RefugioResponse> ExecutarAsync(RefugioRequest request, CancellationToken ct)
    {
        CepFormato.TentarNormalizar(request.Cep, out var cep);

        var refugio = new Refugio(
            request.Nome!, request.Tipo!.Value, cep, request.Endereco!, request.Bairro!,
            request.Capacidade!.Value, request.Abre!.Value, request.Fecha!.Value,
            request.Acessivel!.Value, request.Ativo!.Value);

        refugios.Adicionar(refugio);
        await unitOfWork.SalvarAsync(ct);

        return RefugioResponse.De(refugio, vagasOcupadas: 0);
    }
}

public sealed class AtualizarRefugio(IRefugioRepository refugios, IReservaRepository reservas, IUnitOfWork unitOfWork, TimeProvider relogio)
{
    public async Task<RefugioResponse> ExecutarAsync(long id, RefugioRequest request, CancellationToken ct)
    {
        var refugio = await refugios.ObterPorIdAsync(id, ct) ?? throw RefugioErros.NaoEncontrado();
        CepFormato.TentarNormalizar(request.Cep, out var cep);

        refugio.Atualizar(
            request.Nome!, request.Tipo!.Value, cep, request.Endereco!, request.Bairro!,
            request.Capacidade!.Value, request.Abre!.Value, request.Fecha!.Value,
            request.Acessivel!.Value, request.Ativo!.Value);

        await unitOfWork.SalvarAsync(ct);

        var ocupadas = await reservas.ContarAtivasAsync(id, relogio.HojeEmSaoPaulo(), ct);
        return RefugioResponse.De(refugio, ocupadas);
    }
}

public sealed class ExcluirRefugio(IRefugioRepository refugios, IReservaRepository reservas, IUnitOfWork unitOfWork, TimeProvider relogio)
{
    public async Task ExecutarAsync(long id, CancellationToken ct)
    {
        var refugio = await refugios.ObterPorIdAsync(id, ct) ?? throw RefugioErros.NaoEncontrado();

        // Excluir apagaria reservas que idosos ainda vão usar. Nesse caso o gestor deve desativar o refúgio.
        if (await reservas.RefugioTemReservasAtivasAPartirDeAsync(id, relogio.HojeEmSaoPaulo(), ct))
            throw new ConflitoException(
                "Refúgio com reservas",
                "Este refúgio tem reservas ativas para hoje ou datas futuras. Desative-o em vez de excluir.");

        refugios.Remover(refugio);
        await unitOfWork.SalvarAsync(ct);
    }
}
