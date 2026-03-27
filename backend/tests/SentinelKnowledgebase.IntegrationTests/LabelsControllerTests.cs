using AwesomeAssertions;
using SentinelKnowledgebase.Application.DTOs.Labels;
using System.Net.Http.Json;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class LabelsControllerTests
{
    private readonly IntegrationTestFixture _fixture;

    public LabelsControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetLabels_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/labels");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LabelCategoryCrud_ShouldCreateRenameAndDelete()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var categoryName = $"Language-{Guid.NewGuid():N}";
        var renamedName = $"Locale-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/labels/categories", new { name = categoryName });
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<LabelCategorySummaryDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be(categoryName);

        var renameResponse = await client.PatchAsJsonAsync($"/api/v1/labels/categories/{created.Id}", new { name = renamedName });
        renameResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var renamed = await renameResponse.Content.ReadFromJsonAsync<LabelCategorySummaryDto>();
        renamed.Should().NotBeNull();
        renamed!.Name.Should().Be(renamedName);

        var deleteResponse = await client.DeleteAsync($"/api/v1/labels/categories/{created.Id}");
        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LabelValueCrud_ShouldCreateRenameAndDelete()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var categoryName = $"Language-{Guid.NewGuid():N}";
        var initialValue = $"English-{Guid.NewGuid():N}";
        var renamedValue = $"German-{Guid.NewGuid():N}";

        var createCategoryResponse = await client.PostAsJsonAsync("/api/v1/labels/categories", new { name = categoryName });
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<LabelCategorySummaryDto>();
        category.Should().NotBeNull();

        var createValueResponse = await client.PostAsJsonAsync(
            $"/api/v1/labels/categories/{category!.Id}/values",
            new { value = initialValue });
        createValueResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var created = await createValueResponse.Content.ReadFromJsonAsync<LabelValueSummaryDto>();
        created.Should().NotBeNull();
        created!.Value.Should().Be(initialValue);

        var renameValueResponse = await client.PatchAsJsonAsync(
            $"/api/v1/labels/values/{created.Id}",
            new { value = renamedValue });
        renameValueResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var renamed = await renameValueResponse.Content.ReadFromJsonAsync<LabelValueSummaryDto>();
        renamed.Should().NotBeNull();
        renamed!.Value.Should().Be(renamedValue);

        var deleteValueResponse = await client.DeleteAsync($"/api/v1/labels/values/{created.Id}");
        deleteValueResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }
}
