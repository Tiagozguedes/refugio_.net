using FluentValidation;
using Refugio.Application.DTOs.Auth;

namespace Refugio.Application.Validators;

public sealed class RegistrarRequestValidator : AbstractValidator<RegistrarRequest>
{
    public RegistrarRequestValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().WithMessage("Informe o nome.").MaximumLength(150);
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Informe o e-mail.")
            .EmailAddress().WithMessage("Informe um e-mail válido.")
            .MaximumLength(200);
        RuleFor(r => r.Senha)
            .NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(8).WithMessage("A senha deve ter pelo menos 8 caracteres.")
            .MaximumLength(100)
            .Matches("[A-Z]").WithMessage("A senha deve ter uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve ter uma letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve ter um número.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().WithMessage("Informe o e-mail.");
        RuleFor(r => r.Senha).NotEmpty().WithMessage("Informe a senha.");
    }
}
