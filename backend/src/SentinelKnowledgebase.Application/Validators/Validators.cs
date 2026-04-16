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

public class SearchRequestValidator : AbstractValidator<DTOs.Search.SearchRequestDto>
{
    public SearchRequestValidator()
    {
        RuleFor(x => x.Query)
            .MaximumLength(1000)
            .When(x => x.Query is not null);

        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TagMatchMode)
            .Must(DTOs.Search.SearchMatchModes.IsValid)
            .WithMessage("Tag match mode must be any or all.");

        RuleForEach(x => x.Labels)
            .SetValidator(new LabelAssignmentValidator());

        RuleFor(x => x.LabelMatchMode)
            .Must(DTOs.Search.SearchMatchModes.IsValid)
            .WithMessage("Label match mode must be any or all.");

        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(x => x.Threshold)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1);

        RuleFor(x => x.SortField)
            .Must(value => value is null || DTOs.Search.ProcessedInsightSearchSortFields.IsValid(value))
            .WithMessage("Sort field must be relevance, processedAt, title, or sourceUrl.");

        RuleFor(x => x.SortDirection)
            .Must(value => value is null || DTOs.Search.SearchSortDirections.IsValid(value))
            .WithMessage("Sort direction must be asc or desc.");

        RuleFor(x => x)
            .Must(HasAtLeastOneCriterion)
            .WithMessage("At least one search criterion is required.");
    }

    private static bool HasAtLeastOneCriterion(DTOs.Search.SearchRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            return true;
        }

        if (request.Tags.Any(tag => !string.IsNullOrWhiteSpace(tag)))
        {
            return true;
        }

        return request.Labels.Any(label =>
            !string.IsNullOrWhiteSpace(label.Category) &&
            !string.IsNullOrWhiteSpace(label.Value));
    }
}
