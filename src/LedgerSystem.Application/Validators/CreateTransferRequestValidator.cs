using FluentValidation;
using LedgerSystem.Application.DTOs.Transfers;

namespace LedgerSystem.Application.Validators;

public sealed class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.SourceWalletId)
            .NotEmpty().WithMessage("Source wallet ID is required.");

        RuleFor(x => x.DestinationWalletId)
            .NotEmpty().WithMessage("Destination wallet ID is required.")
            .NotEqual(x => x.SourceWalletId)
                .WithMessage("Source and destination wallets must be different.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Transfer amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Single transfer cannot exceed 1,000,000.")
            .PrecisionScale(19, 4, false)
                .WithMessage("Amount must have at most 4 decimal places.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be exactly 3 characters.")
            .Matches("^[A-Za-z]{3}$")
                .WithMessage("Currency must be a valid ISO 4217 code (e.g. USD).");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters.")
            .When(x => x.Description is not null);
    }
}
