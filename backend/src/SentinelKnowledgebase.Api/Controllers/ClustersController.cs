using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelKnowledgebase.Api.Extensions;
using SentinelKnowledgebase.Application.DTOs.Clusters;
using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clusters")]
public class ClustersController : ControllerBase
{
    private readonly IInsightClusteringService _insightClusteringService;

    public ClustersController(IInsightClusteringService insightClusteringService)
    {
        _insightClusteringService = insightClusteringService;
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
}
