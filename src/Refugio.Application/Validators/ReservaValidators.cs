using FluentValidation;
using Refugio.Application.Common;
using Refugio.Application.DTOs.Reservas;

namespace Refugio.Application.Validators;

public sealed class CriarReservaRequestValidator : AbstractValidator<CriarReservaRequest>
{
    public CriarReservaRequestValidator(TimeProvider relogio)
    {
        RuleFor(r => r.RefugioId)
            .NotNull().WithMessage("Informe o refúgio.")
            .GreaterThan(0).WithMessage("Informe um refúgio válido.");

        RuleFor(r => r.DataReserva)
            .NotNull().WithMessage("Informe a data da reserva.")
            .Must(data => data >= relogio.HojeEmSaoPaulo())
            .WithMessage("A data da reserva não pode estar no passado.")
            .When(r => r.DataReserva is not null, ApplyConditionTo.CurrentValidator);
    }
}
