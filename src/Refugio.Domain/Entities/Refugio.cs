using Refugio.Domain.Enums;
using Refugio.Domain.Exceptions;

namespace Refugio.Domain.Entities;

public class Refugio
{
    private Refugio() { } // EF Core

    public Refugio(
        string nome, TipoRefugio tipo, string cep, string endereco, string bairro,
        int capacidade, TimeOnly abre, TimeOnly fecha, bool acessivel, bool ativo)
    {
        Atualizar(nome, tipo, cep, endereco, bairro, capacidade, abre, fecha, acessivel, ativo);
    }

    public long Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public TipoRefugio Tipo { get; private set; }
    public string Cep { get; private set; } = string.Empty;
    public string Endereco { get; private set; } = string.Empty;
    public string Bairro { get; private set; } = string.Empty;
    public int Capacidade { get; private set; }
    public TimeOnly Abre { get; private set; }
    public TimeOnly Fecha { get; private set; }
    public bool Acessivel { get; private set; }
    public bool Ativo { get; private set; }

    public string EnderecoCompleto => $"{Endereco} — {Bairro}";

    public void Atualizar(
        string nome, TipoRefugio tipo, string cep, string endereco, string bairro,
        int capacidade, TimeOnly abre, TimeOnly fecha, bool acessivel, bool ativo)
    {
        if (capacidade <= 0)
            throw new DadosInvalidosException("capacidade", "A capacidade deve ser maior que zero.");
        if (fecha <= abre)
            throw new DadosInvalidosException("fecha", "O horário de fechamento deve ser depois da abertura.");

        Nome = nome.Trim();
        Tipo = tipo;
        Cep = cep;
        Endereco = endereco.Trim();
        Bairro = bairro.Trim();
        Capacidade = capacidade;
        Abre = abre;
        Fecha = fecha;
        Acessivel = acessivel;
        Ativo = ativo;
    }

    public int VagasDisponiveis(int vagasOcupadas) => Math.Max(0, Capacidade - vagasOcupadas);

    /// <summary>
    /// Regras de uma nova reserva neste refúgio. A ordem define qual erro o idoso vê primeiro.
    /// </summary>
    public void GarantirQueAceitaReserva(DateOnly dataReserva, DateOnly hoje, int vagasOcupadas, bool idosoJaTemReservaNoDia)
    {
        if (dataReserva < hoje)
            throw new DadosInvalidosException("dataReserva", "A data da reserva não pode estar no passado.");
        if (!Ativo)
            throw new RegraDeNegocioException("Refúgio inativo", "Este refúgio está fechado e não aceita reservas.");
        if (idosoJaTemReservaNoDia)
            throw new ConflitoException("Reserva já existe", $"Você já tem uma reserva para {dataReserva:dd/MM}.");
        if (VagasDisponiveis(vagasOcupadas) == 0)
            throw new ConflitoException("Sem vaga", $"Este refúgio não tem mais vagas para {dataReserva:dd/MM}.");
    }
}
