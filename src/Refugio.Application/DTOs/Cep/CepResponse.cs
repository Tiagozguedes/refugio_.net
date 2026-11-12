namespace Refugio.Application.DTOs.Cep;

/// <summary>Endereço para o formulário de refúgio preencher sozinho.</summary>
public sealed record CepResponse(string Cep, string Endereco, string Bairro, string Cidade, string Uf);
