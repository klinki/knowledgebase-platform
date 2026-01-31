using FluentValidation;
using Sentinel.Application.Dtos;

namespace Sentinel.Application.Validators;

public sealed class TagSearchRequestValidator : AbstractValidator<TagSearchRequest>
{
    public TagSearchRequestValidator()
    {
        RuleFor(x => x.Tags)
            .NotEmpty();

        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50);
    }
}
