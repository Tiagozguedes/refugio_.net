namespace Refugio.Application.DTOs;

/// <summary>Metadados de página com os nomes fixados no contrato.</summary>
public sealed record PaginaResponse<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, int TotalItens, int TotalPaginas)
{
    public static PaginaResponse<T> Criar(IReadOnlyList<T> itens, int pagina, int tamanho, int totalItens) =>
        new(itens, pagina, tamanho, totalItens, (int)Math.Ceiling(totalItens / (double)tamanho));
}
