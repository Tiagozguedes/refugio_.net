using FluentValidation;
using Refugio.Application.DTOs.Usuarios;

namespace Refugio.Application.Validators;

public sealed class AtualizarUsuarioRequestValidator : AbstractValidator<AtualizarUsuarioRequest>
{
    private const string PadraoTelefone = @"^[0-9()+\-\s]{8,20}$";
    private const string MensagemTelefone = "Informe um telefone válido, como (11) 98888-0000.";

    public AtualizarUsuarioRequestValidator()
    {
        RuleFor(u => u.Nome).NotEmpty().WithMessage("Informe o nome.").MaximumLength(150);
        RuleFor(u => u.Bairro).MaximumLength(100);
        RuleFor(u => u.ContatoEmergenciaNome).MaximumLength(150);

        RuleFor(u => u.Telefone)
            .Matches(PadraoTelefone).WithMessage(MensagemTelefone)
            .When(u => !string.IsNullOrWhiteSpace(u.Telefone));
        RuleFor(u => u.ContatoEmergenciaTelefone)
            .Matches(PadraoTelefone).WithMessage(MensagemTelefone)
            .When(u => !string.IsNullOrWhiteSpace(u.ContatoEmergenciaTelefone));
    }
}
