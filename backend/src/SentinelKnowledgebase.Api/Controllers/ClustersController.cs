using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using SentinelKnowledgebase.Api.Extensions;
using SentinelKnowledgebase.Application.DTOs.Clusters;
using SentinelKnowledgebase.Application.Hangfire;
using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clusters")]
public class ClustersController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IInsightClusteringService _insightClusteringService;
    private readonly ILogger<ClustersController> _logger;

    public ClustersController(
        IBackgroundJobClient backgroundJobClient,
        IInsightClusteringService insightClusteringService,
        ILogger<ClustersController> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _insightClusteringService = insightClusteringService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TopicClusterSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClusters()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var clusters = await _insightClusteringService.GetClusterSummariesAsync(userId);
        return Ok(clusters);
    }

    [HttpGet("list")]
    [ProducesResponseType(typeof(TopicClusterListPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClusterList([FromQuery] TopicClusterListQueryDto query)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var page = await _insightClusteringService.GetClusterListPageAsync(userId, query);
        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TopicClusterDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCluster(Guid id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var cluster = await _insightClusteringService.GetClusterDetailAsync(userId, id);
        return cluster == null ? NotFound() : Ok(cluster);
    }

    [HttpPost("rebuild")]
    [ProducesResponseType(typeof(ClusterRebuildAcceptedDto), StatusCodes.Status202Accepted)]
    public IActionResult RebuildClusters()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var jobId = _backgroundJobClient.Create(
            Job.FromExpression<IInsightClusteringService>(service => service.RebuildOwnerClustersAsync(userId)),
            new EnqueuedState(HangfireQueues.Clustering));
        _logger.LogInformation(
            "Cluster rebuild requested for owner {OwnerUserId}; Hangfire job {JobId} enqueued on {QueueName}",
            userId,
            jobId,
            HangfireQueues.Clustering);

        return Accepted(new ClusterRebuildAcceptedDto
        {
            JobId = jobId,
            Message = "Cluster rebuild accepted and enqueued"
        });
    }
}
