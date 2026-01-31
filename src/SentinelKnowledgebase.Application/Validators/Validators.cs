using FluentValidation;

namespace SentinelKnowledgebase.Application.Validators;

public class CaptureRequestValidator : AbstractValidator<DTOs.Capture.CaptureRequestDto>
{
    public CaptureRequestValidator()
    {
        RuleFor(x => x.SourceUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeValidUrl).WithMessage("Invalid URL format");
        
        RuleFor(x => x.RawContent)
            .NotEmpty()
            .MaximumLength(10000);
        
        RuleFor(x => x.ContentType)
            .IsInEnum();
    }
    
    private bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

public class SemanticSearchRequestValidator : AbstractValidator<DTOs.Search.SemanticSearchRequestDto>
{
    public SemanticSearchRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(1000);
        
        RuleFor(x => x.TopK)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
        
        RuleFor(x => x.Threshold)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1);
    }
}

public class TagSearchRequestValidator : AbstractValidator<DTOs.Search.TagSearchRequestDto>
{
    public TagSearchRequestValidator()
    {
        RuleFor(x => x.Tags)
            .NotEmpty().WithMessage("At least one tag is required");
        
        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(100);
    }
}
