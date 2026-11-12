using FluentValidation;
using Refugio.Application.Common;
using Refugio.Application.DTOs.Refugios;

namespace Refugio.Application.Validators;

public sealed class RefugioRequestValidator : AbstractValidator<RefugioRequest>
{
    public RefugioRequestValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().WithMessage("Informe o nome do refúgio.").MaximumLength(150);
        RuleFor(r => r.Tipo).NotNull().WithMessage("Informe o tipo do refúgio.").IsInEnum();
        RuleFor(r => r.Cep)
            .NotEmpty().WithMessage("Informe o CEP.")
            .Must(CepFormato.EhValido).WithMessage("O CEP deve ter 8 dígitos, como 03601-000.");
        RuleFor(r => r.Endereco).NotEmpty().WithMessage("Informe o endereço.").MaximumLength(200);
        RuleFor(r => r.Bairro).NotEmpty().WithMessage("Informe o bairro.").MaximumLength(100);
        RuleFor(r => r.Capacidade)
            .NotNull().WithMessage("Informe a capacidade.")
            .GreaterThan(0).WithMessage("A capacidade deve ser maior que zero.")
            .LessThanOrEqualTo(10_000);
        RuleFor(r => r.Abre).NotNull().WithMessage("Informe o horário de abertura.");
        RuleFor(r => r.Fecha)
            .NotNull().WithMessage("Informe o horário de fechamento.")
            .GreaterThan(r => r.Abre)
            .WithMessage("O horário de fechamento deve ser depois da abertura.")
            .When(r => r.Abre is not null, ApplyConditionTo.CurrentValidator);
        RuleFor(r => r.Acessivel).NotNull().WithMessage("Informe se o refúgio é acessível.");
        RuleFor(r => r.Ativo).NotNull().WithMessage("Informe se o refúgio está ativo.");
    }
}

public sealed class ListarRefugiosQueryValidator : AbstractValidator<ListarRefugiosQuery>
{
    public ListarRefugiosQueryValidator()
    {
        RuleFor(q => q.Pagina).GreaterThanOrEqualTo(1).WithMessage("A página começa em 1.");
        RuleFor(q => q.Tamanho)
            .InclusiveBetween(1, ListarRefugiosQuery.TamanhoMaximo)
            .WithMessage($"O tamanho da página deve ficar entre 1 e {ListarRefugiosQuery.TamanhoMaximo}.");
        RuleFor(q => q.Bairro).MaximumLength(100);
    }
}
