using FluentValidation;
using Sentinel.Application.Dtos;

namespace Sentinel.Application.Validators;

public sealed class SemanticSearchRequestValidator : AbstractValidator<SemanticSearchRequest>
{
    public SemanticSearchRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50);
    }
}
