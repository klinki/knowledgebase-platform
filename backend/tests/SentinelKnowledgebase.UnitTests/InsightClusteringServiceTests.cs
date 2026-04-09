using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Repositories;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class InsightClusteringServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessedInsightRepository _processedInsightRepository;
    private readonly IInsightClusterRepository _insightClusterRepository;
    private readonly IContentProcessor _contentProcessor;
    private readonly ILogger<InsightClusteringService> _logger;
    private readonly InsightClusteringService _service;

    public InsightClusteringServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _processedInsightRepository = Substitute.For<IProcessedInsightRepository>();
        _insightClusterRepository = Substitute.For<IInsightClusterRepository>();
        _contentProcessor = Substitute.For<IContentProcessor>();
        _logger = Substitute.For<ILogger<InsightClusteringService>>();

        _unitOfWork.ProcessedInsights.Returns(_processedInsightRepository);
        _unitOfWork.InsightClusters.Returns(_insightClusterRepository);
        _unitOfWork.SaveChangesAsync().Returns(1);

        _service = new InsightClusteringService(_unitOfWork, _contentProcessor, _logger);
    }

    [Fact]
    public async Task RebuildOwnerClustersAsync_ShouldPersistMutualNeighborClusters_AndLeaveOutliersUnclustered()
    {
        var ownerUserId = Guid.NewGuid();
        var outlierId = Guid.NewGuid();
        var addedClusters = new List<InsightCluster>();
        var addedMemberships = new List<InsightClusterMembership>();

        _processedInsightRepository.GetEmbeddingRecordsAsync(ownerUserId).Returns(
        [
            CreateRecord(ownerUserId, "infra-1", Unit(1f, 0f, 0f), "GPU serving rollout"),
            CreateRecord(ownerUserId, "infra-2", Unit(0.98f, 0.2f, 0f), "GPU latency tuning"),
            CreateRecord(ownerUserId, "infra-3", Unit(0.98f, -0.2f, 0f), "GPU deployment guide"),
            CreateRecord(ownerUserId, "market-1", Unit(0f, 1f, 0f), "Rate cut outlook"),
            CreateRecord(ownerUserId, "market-2", Unit(0.2f, 0.98f, 0f), "Bond market repricing"),
            CreateRecord(ownerUserId, "market-3", Unit(-0.2f, 0.98f, 0f), "Inflation surprise watch"),
            CreateRecord(ownerUserId, "noise-1", Unit(-1f, 0f, 0f), "Unrelated gardening tip", outlierId)
        ]);
        _contentProcessor.GenerateClusterMetadataAsync(Arg.Any<IReadOnlyCollection<string>>())
            .Returns(call =>
            {
                var summaries = call.Arg<IReadOnlyCollection<string>>();
                var firstWord = summaries.First().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                return Task.FromResult(new ClusterMetadata
                {
                    Title = $"{firstWord} Topic",
                    Description = $"Cluster for {firstWord}",
                    Keywords = [firstWord.ToLowerInvariant(), "topic", "related"]
                });
            });
        _insightClusterRepository.AddAsync(Arg.Any<InsightCluster>())
            .Returns(call =>
            {
                var cluster = call.Arg<InsightCluster>();
                addedClusters.Add(cluster);
                return Task.FromResult(cluster);
            });
        _insightClusterRepository
            .When(repository => repository.AddMembershipsAsync(Arg.Any<IEnumerable<InsightClusterMembership>>()))
            .Do(call => addedMemberships.AddRange(call.Arg<IEnumerable<InsightClusterMembership>>()));

        await _service.RebuildOwnerClustersAsync(ownerUserId);

        addedClusters.Should().HaveCount(2);
        addedMemberships.Should().HaveCount(6);
        addedMemberships.Select(membership => membership.ProcessedInsightId).Should().OnlyHaveUniqueItems();
        addedMemberships.Should().NotContain(membership => membership.ProcessedInsightId == outlierId);
        addedClusters.Should().OnlyContain(cluster => cluster.MemberCount == 3);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RebuildOwnerClustersAsync_ShouldRankMembersByCentroidSimilarity()
    {
        var ownerUserId = Guid.NewGuid();
        var centralId = Guid.NewGuid();
        var addedClusters = new List<InsightCluster>();
        var addedMemberships = new List<InsightClusterMembership>();

        _processedInsightRepository.GetEmbeddingRecordsAsync(ownerUserId).Returns(
        [
            CreateRecord(ownerUserId, "core", Unit(1f, 0f, 0f), "Core platform direction", centralId),
            CreateRecord(ownerUserId, "peer-1", Unit(0.995f, 0.1f, 0f), "Platform roadmap update"),
            CreateRecord(ownerUserId, "peer-2", Unit(0.995f, -0.1f, 0f), "Platform roadmap planning"),
            CreateRecord(ownerUserId, "peer-3", Unit(0.98f, 0.18f, 0f), "Platform infrastructure review"),
            CreateRecord(ownerUserId, "peer-4", Unit(0.98f, -0.18f, 0f), "Platform infrastructure changes"),
            CreateRecord(ownerUserId, "peer-5", Unit(0.97f, 0.22f, 0f), "Platform infrastructure backlog")
        ]);
        _contentProcessor.GenerateClusterMetadataAsync(Arg.Any<IReadOnlyCollection<string>>())
            .Returns(Task.FromResult(new ClusterMetadata
            {
                Title = "Platform Topic",
                Description = "Cluster",
                Keywords = ["platform", "infra", "ops"]
            }));
        _insightClusterRepository.AddAsync(Arg.Any<InsightCluster>())
            .Returns(call =>
            {
                var cluster = call.Arg<InsightCluster>();
                addedClusters.Add(cluster);
                return Task.FromResult(cluster);
            });
        _insightClusterRepository
            .When(repository => repository.AddMembershipsAsync(Arg.Any<IEnumerable<InsightClusterMembership>>()))
            .Do(call => addedMemberships.AddRange(call.Arg<IEnumerable<InsightClusterMembership>>()));

        await _service.RebuildOwnerClustersAsync(ownerUserId);

        addedClusters.Should().ContainSingle();
        addedClusters[0].RepresentativeProcessedInsightId.Should().Be(centralId);
        addedMemberships.Should().HaveCount(6);
        addedMemberships.Single(membership => membership.ProcessedInsightId == centralId).Rank.Should().Be(1);
        addedMemberships.Select(membership => membership.Rank).Should().BeEquivalentTo([1, 2, 3, 4, 5, 6]);
    }

    [Fact]
    public async Task RebuildOwnerClustersAsync_ShouldFallbackToLocalKeywords_WhenMetadataGenerationFails()
    {
        var ownerUserId = Guid.NewGuid();
        var addedClusters = new List<InsightCluster>();

        _processedInsightRepository.GetEmbeddingRecordsAsync(ownerUserId).Returns(
        [
            CreateRecord(ownerUserId, "topic-1", Unit(1f, 0f, 0f), "Markets policy rates"),
            CreateRecord(ownerUserId, "topic-2", Unit(0.995f, 0.1f, 0f), "Markets policy inflation"),
            CreateRecord(ownerUserId, "topic-3", Unit(0.995f, -0.1f, 0f), "Markets rates outlook"),
            CreateRecord(ownerUserId, "topic-4", Unit(0.98f, 0.18f, 0f), "Policy outlook markets"),
            CreateRecord(ownerUserId, "topic-5", Unit(0.98f, -0.18f, 0f), "Inflation markets policy"),
            CreateRecord(ownerUserId, "topic-6", Unit(0.97f, 0.22f, 0f), "Rates markets policy")
        ]);
        _contentProcessor.GenerateClusterMetadataAsync(Arg.Any<IReadOnlyCollection<string>>())
            .Returns(Task.FromException<ClusterMetadata>(new InvalidOperationException("boom")));
        _insightClusterRepository.AddAsync(Arg.Any<InsightCluster>())
            .Returns(call =>
            {
                var cluster = call.Arg<InsightCluster>();
                addedClusters.Add(cluster);
                return Task.FromResult(cluster);
            });

        await _service.RebuildOwnerClustersAsync(ownerUserId);

        addedClusters.Should().ContainSingle();
        addedClusters[0].Title.Should().NotBeNullOrWhiteSpace();
        addedClusters[0].Description.Should().BeNull();
        addedClusters[0].KeywordsJson.Should().Contain("markets");
    }

    private static ProcessedInsightEmbeddingRecord CreateRecord(
        Guid ownerUserId,
        string idSeed,
        float[] embedding,
        string summary,
        Guid? id = null)
    {
        return new ProcessedInsightEmbeddingRecord
        {
            Id = id ?? CreateGuid(idSeed),
            OwnerUserId = ownerUserId,
            Title = summary,
            Summary = summary,
            SourceUrl = $"https://example.com/{idSeed}",
            Embedding = embedding
        };
    }

    private static Guid CreateGuid(string value)
    {
        return GuidUtility.Create(GuidUtility.UrlNamespace, value);
    }

    private static float[] Unit(float x, float y, float z)
    {
        var vector = new[] { x, y, z };
        var norm = MathF.Sqrt(vector.Sum(component => component * component));
        return vector.Select(component => component / norm).ToArray();
    }

    private static class GuidUtility
    {
        public static readonly Guid UrlNamespace = new("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

        public static Guid Create(Guid namespaceId, string name)
        {
            var namespaceBytes = namespaceId.ToByteArray();
            SwapByteOrder(namespaceBytes);

            var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            var hash = System.Security.Cryptography.SHA1.HashData([..namespaceBytes, ..nameBytes]);
            var newGuid = new byte[16];
            Array.Copy(hash, 0, newGuid, 0, 16);

            newGuid[6] = (byte)((newGuid[6] & 0x0F) | (5 << 4));
            newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);
            SwapByteOrder(newGuid);
            return new Guid(newGuid);
        }

        private static void SwapByteOrder(byte[] guid)
        {
            Swap(guid, 0, 3);
            Swap(guid, 1, 2);
            Swap(guid, 4, 5);
            Swap(guid, 6, 7);
        }

        private static void Swap(byte[] guid, int left, int right)
        {
            (guid[left], guid[right]) = (guid[right], guid[left]);
        }
    }
}
