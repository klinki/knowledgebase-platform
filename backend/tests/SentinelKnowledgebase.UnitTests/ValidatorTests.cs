using AwesomeAssertions;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Validators;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class ValidatorTests
{
    private readonly CaptureRequestValidator _captureValidator;
    private readonly BulkCaptureRequestValidator _bulkCaptureValidator;
    private readonly SemanticSearchRequestValidator _semanticSearchValidator;
    private readonly TagSearchRequestValidator _tagSearchValidator;
    private readonly LabelSearchRequestValidator _labelSearchValidator;
    private readonly SearchRequestValidator _searchRequestValidator;
    
    public ValidatorTests()
    {
        _captureValidator = new CaptureRequestValidator();
        _bulkCaptureValidator = new BulkCaptureRequestValidator();
        _semanticSearchValidator = new SemanticSearchRequestValidator();
        _tagSearchValidator = new TagSearchRequestValidator();
        _labelSearchValidator = new LabelSearchRequestValidator();
        _searchRequestValidator = new SearchRequestValidator();
    }
    
    [Fact]
    public void CaptureRequest_WithValidData_ShouldPassValidation()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com/article",
            ContentType = Domain.Enums.ContentType.Article,
            RawContent = "This is test content"
        };
        
        var result = _captureValidator.Validate(request);
        
        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void CaptureRequest_WithInvalidUrl_ShouldFailValidation()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "not-a-valid-url",
            ContentType = Domain.Enums.ContentType.Article,
            RawContent = "Test content"
        };
        
        var result = _captureValidator.Validate(request);
        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SourceUrl");
    }

    [Fact]
    public void CaptureRequest_WithUrlOnlyPayload_ShouldPassValidation()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com/article",
            ContentType = Domain.Enums.ContentType.Article,
            RawContent = "https://example.com/article"
        };

        var result = _captureValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CaptureRequest_WithDirectContentAndNoUrl_ShouldPassValidation()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "",
            ContentType = Domain.Enums.ContentType.Note,
            RawContent = "Captured manually from the frontend."
        };

        var result = _captureValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void CaptureRequest_WithEmptyContent_ShouldFailValidation()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com",
            ContentType = Domain.Enums.ContentType.Article,
            RawContent = ""
        };
        
        var result = _captureValidator.Validate(request);
        
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BulkCaptureRequest_WithEmptyList_ShouldFailValidation()
    {
        var result = _bulkCaptureValidator.Validate([]);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("At least one capture"));
    }

    [Fact]
    public void BulkCaptureRequest_WithMoreThan500Items_ShouldFailValidation()
    {
        var requests = Enumerable.Range(0, BulkCaptureRequestValidator.MaxBatchSize + 1)
            .Select(index => new CaptureRequestDto
            {
                SourceUrl = $"https://example.com/{index}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = $"Content {index}"
            })
            .ToList();

        var result = _bulkCaptureValidator.Validate(requests);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("maximum of 500 captures"));
    }

    [Fact]
    public void BulkCaptureRequest_WithInvalidItem_ShouldFailValidation()
    {
        var requests = new List<CaptureRequestDto>
        {
            new()
            {
                SourceUrl = "https://example.com/valid",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Valid"
            },
            new()
            {
                SourceUrl = "not-a-valid-url",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Invalid"
            }
        };

        var result = _bulkCaptureValidator.Validate(requests);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName.Contains("[1].SourceUrl"));
    }

    [Fact]
    public void CaptureRequest_WithValidLabels_ShouldPassValidation()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com",
            ContentType = Domain.Enums.ContentType.Article,
            RawContent = "Test content",
            Labels =
            [
                new LabelAssignmentDto { Category = "Language", Value = "English" },
                new LabelAssignmentDto { Category = "Source", Value = "Web" }
            ]
        };

        var result = _captureValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CaptureRequest_WithDuplicateLabelCategories_ShouldFailValidation()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com",
            ContentType = Domain.Enums.ContentType.Article,
            RawContent = "Test content",
            Labels =
            [
                new LabelAssignmentDto { Category = "Language", Value = "English" },
                new LabelAssignmentDto { Category = " language ", Value = "German" }
            ]
        };

        var result = _captureValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Labels");
    }
    
    [Fact]
    public void SemanticSearchRequest_WithValidQuery_ShouldPassValidation()
    {
        var request = new SemanticSearchRequestDto
        {
            Query = "test search query",
            TopK = 10,
            Threshold = 0.7
        };
        
        var result = _semanticSearchValidator.Validate(request);
        
        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void SemanticSearchRequest_WithEmptyQuery_ShouldFailValidation()
    {
        var request = new SemanticSearchRequestDto
        {
            Query = "",
            TopK = 10
        };
        
        var result = _semanticSearchValidator.Validate(request);
        
        result.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public void SemanticSearchRequest_WithInvalidThreshold_ShouldFailValidation()
    {
        var request = new SemanticSearchRequestDto
        {
            Query = "test query",
            Threshold = 1.5
        };
        
        var result = _semanticSearchValidator.Validate(request);
        
        result.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public void TagSearchRequest_WithValidTags_ShouldPassValidation()
    {
        var request = new TagSearchRequestDto
        {
            Tags = new List<string> { "tag1", "tag2" }
        };
        
        var result = _tagSearchValidator.Validate(request);
        
        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void TagSearchRequest_WithEmptyTags_ShouldFailValidation()
    {
        var request = new TagSearchRequestDto
        {
            Tags = new List<string>()
        };
        
        var result = _tagSearchValidator.Validate(request);
        
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LabelSearchRequest_WithValidLabels_ShouldPassValidation()
    {
        var request = new LabelSearchRequestDto
        {
            Labels =
            [
                new LabelAssignmentDto { Category = "Language", Value = "English" }
            ]
        };

        var result = _labelSearchValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LabelSearchRequest_WithEmptyLabels_ShouldFailValidation()
    {
        var request = new LabelSearchRequestDto
        {
            Labels = []
        };

        var result = _labelSearchValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SearchRequest_WithQueryOnly_ShouldPassValidation()
    {
        var request = new SearchRequestDto
        {
            Query = "semantic query"
        };

        var result = _searchRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SearchRequest_WithTopicClusterOnly_ShouldPassValidation()
    {
        var request = new SearchRequestDto
        {
            TopicClusterId = Guid.NewGuid()
        };

        var result = _searchRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SearchRequest_WithEmptyCriteria_ShouldFailValidation()
    {
        var request = new SearchRequestDto
        {
            Query = "   ",
            Tags = [],
            Labels = []
        };

        var result = _searchRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("At least one search criterion"));
    }

    [Fact]
    public void SearchRequest_WithInvalidMatchMode_ShouldFailValidation()
    {
        var request = new SearchRequestDto
        {
            Tags = ["alpha"],
            TagMatchMode = "partial"
        };

        var result = _searchRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "TagMatchMode");
    }
}
