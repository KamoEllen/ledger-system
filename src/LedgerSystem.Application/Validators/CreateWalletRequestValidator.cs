using FluentValidation;
using LedgerSystem.Application.DTOs.Wallets;

namespace LedgerSystem.Application.Validators;

public sealed class CreateWalletRequestValidator : AbstractValidator<CreateWalletRequest>
{
    public CreateWalletRequestValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be exactly 3 characters.")
            .Matches("^[A-Za-z]{3}$")
                .WithMessage("Currency must be a 3-letter ISO 4217 code (e.g. USD, EUR, GBP).");
    }
}
