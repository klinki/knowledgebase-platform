using FluentValidation;
using SentinelKnowledgebase.Application.DTOs.Labels;

namespace SentinelKnowledgebase.Application.Validators;

public class CaptureRequestValidator : AbstractValidator<DTOs.Capture.CaptureRequestDto>
{
    public CaptureRequestValidator()
    {
        RuleFor(x => x.SourceUrl)
            .MaximumLength(2048)
            .Must(BeEmptyOrValidUrl).WithMessage("Invalid URL format");
        
        RuleFor(x => x.RawContent)
            .NotEmpty()
            .MaximumLength(10000);
        
        RuleFor(x => x.ContentType)
            .IsInEnum();

        RuleForEach(x => x.Labels)
            .SetValidator(new LabelAssignmentValidator());

        RuleFor(x => x.Labels)
            .Must(NotContainDuplicateCategories)
            .When(x => x.Labels != null)
            .WithMessage("Each label category may appear only once per capture.");
    }
    
    private bool BeEmptyOrValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool NotContainDuplicateCategories(List<LabelAssignmentDto>? labels)
    {
        if (labels == null)
        {
            return true;
        }

        return labels
            .Where(label => !string.IsNullOrWhiteSpace(label.Category))
            .Select(label => label.Category.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == labels.Count(label => !string.IsNullOrWhiteSpace(label.Category));
    }
}

public class BulkCaptureRequestValidator : AbstractValidator<List<DTOs.Capture.CaptureRequestDto>>
{
    public const int MaxBatchSize = 500;

    public BulkCaptureRequestValidator()
    {
        RuleFor(requests => requests)
            .NotEmpty()
            .WithMessage("At least one capture is required.");

        RuleFor(requests => requests.Count)
            .LessThanOrEqualTo(MaxBatchSize)
            .WithMessage($"A maximum of {MaxBatchSize} captures is allowed per request.");

        RuleForEach(requests => requests)
            .SetValidator(new CaptureRequestValidator());
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

public class LabelAssignmentValidator : AbstractValidator<LabelAssignmentDto>
{
    public LabelAssignmentValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public class LabelSearchRequestValidator : AbstractValidator<DTOs.Search.LabelSearchRequestDto>
{
    public LabelSearchRequestValidator()
    {
        RuleFor(x => x.Labels)
            .NotEmpty().WithMessage("At least one label is required");

        RuleForEach(x => x.Labels)
            .SetValidator(new LabelAssignmentValidator());
    }
}
