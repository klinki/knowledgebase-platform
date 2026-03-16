using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Enums;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class ContentProcessorTests
{
    private readonly ContentProcessor _processor;
    
    public ContentProcessorTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "" },
                { "OpenAI:Model", "gpt-4" },
                { "OpenAI:EmbeddingModel", "text-embedding-3-small" }
            })
            .Build();
        
        var httpClient = new HttpClient();
        var monitoringService = Substitute.For<IMonitoringService>();
        var logger = Substitute.For<ILogger<ContentProcessor>>();
        _processor = new ContentProcessor(config, httpClient, monitoringService, logger);
    }
    
    [Fact]
    public void DenoiseContent_ShouldRemoveNoiseLines()
    {
        var content = @"
This is important content.
http://example.com
More important content.
#hashtag
Final important content.
";
        
        var result = _processor.DenoiseContent(content);
        
        result.Should().NotContain("http://example.com");
        result.Should().NotContain("#hashtag");
        result.Should().Contain("This is important content.");
        result.Should().Contain("Final important content.");
    }
    
    [Fact]
    public void DenoiseContent_ShouldPreservePunctuationLines()
    {
        var content = @"Important content with punctuation! Another line.";
        
        var result = _processor.DenoiseContent(content);
        
        result.Should().Contain("Important content");
    }
    
    [Fact]
    public async Task ExtractInsightsAsync_ShouldThrow_WhenApiKeyIsMissing()
    {
        var content = "This is a test article about programming. It covers best practices.";
        
        var action = async () => await _processor.ExtractInsightsAsync(content, ContentType.Article);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpenAI API key is not configured.");
    }
    
    [Fact]
    public async Task GenerateEmbeddingAsync_ShouldThrow_WhenApiKeyIsMissing()
    {
        var text = "Test text for embedding";
        
        var action = async () => await _processor.GenerateEmbeddingAsync(text);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpenAI API key is not configured.");
    }
}
