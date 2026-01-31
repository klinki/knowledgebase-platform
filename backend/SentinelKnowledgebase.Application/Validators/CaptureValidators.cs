using FluentValidation;
using SentinelKnowledgebase.Application.DTOs;

namespace SentinelKnowledgebase.Application.Validators;

public class CaptureRequestValidator : AbstractValidator<CaptureRequest>
{
    public CaptureRequestValidator()
    {
        RuleFor(x => x.SourceUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeAValidUrl).WithMessage("SourceUrl must be a valid URL");

        RuleFor(x => x.RawContent)
            .NotEmpty();

        RuleFor(x => x.Source)
            .IsInEnum();
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}

public class SemanticSearchRequestValidator : AbstractValidator<SemanticSearchRequest>
{
    public SemanticSearchRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100);
    }
}

public class TagSearchRequestValidator : AbstractValidator<TagSearchRequest>
{
    public TagSearchRequestValidator()
    {
        RuleFor(x => x.Tag)
            .NotEmpty()
            .MaximumLength(100);
    }
}
