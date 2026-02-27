using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
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
        _processor = new ContentProcessor(config, httpClient, monitoringService);
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
    public async Task ExtractInsightsAsync_ShouldReturnInsights()
    {
        var content = "This is a test article about programming. It covers best practices.";
        
        var result = await _processor.ExtractInsightsAsync(content, ContentType.Article);
        
        result.Should().NotBeNull();
        result.Title.Should().NotBeNullOrEmpty();
        result.Summary.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task GenerateEmbeddingAsync_ShouldReturnVector()
    {
        var text = "Test text for embedding";
        
        var result = await _processor.GenerateEmbeddingAsync(text);
        
        result.Should().NotBeNull();
        result.Should().HaveCount(1536);
    }
}
