using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelKnowledgebase.Application.DTOs.Assistant;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class AssistantChatServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAssistantChatRepository _assistantChatRepository;
    private readonly ICaptureBulkActionService _captureBulkActionService;
    private readonly AssistantChatService _service;

    public AssistantChatServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _assistantChatRepository = Substitute.For<IAssistantChatRepository>();
        _captureBulkActionService = Substitute.For<ICaptureBulkActionService>();

        _unitOfWork.AssistantChat.Returns(_assistantChatRepository);
        _unitOfWork.SaveChangesAsync().Returns(1);

        _assistantChatRepository.GetResultSetsByIdsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns(new Dictionary<Guid, AssistantChatResultSet>());
        _assistantChatRepository.GetPendingActionsByIdsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns(new Dictionary<Guid, AssistantChatPendingAction>());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        _service = new AssistantChatService(
            _unitOfWork,
            _captureBulkActionService,
            configuration,
            new HttpClient(),
            Substitute.For<ILogger<AssistantChatService>>());
    }

    [Fact]
    public async Task SendMessageAsync_DeleteProposal_ShouldCreatePendingActionWithoutDeletion()
    {
        var ownerUserId = Guid.NewGuid();
        var resultSetId = Guid.NewGuid();
        var captureA = Guid.NewGuid();
        var captureB = Guid.NewGuid();
        var session = new AssistantChatSession
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            LastResultSetId = resultSetId
        };
        var resultSet = new AssistantChatResultSet
        {
            Id = resultSetId,
            SessionId = session.Id,
            OwnerUserId = ownerUserId,
            QueryType = "deleted_twitter_accounts",
            Summary = "Found 2",
            CaptureIdsJson = System.Text.Json.JsonSerializer.Serialize(new[] { captureA, captureB }),
            PreviewJson = "[]",
            TotalCount = 2
        };

        _assistantChatRepository.GetOrCreateSessionAsync(ownerUserId).Returns(session);
        _assistantChatRepository.GetMessagesAsync(ownerUserId).Returns([]);
        _assistantChatRepository.GetResultSetByIdAsync(ownerUserId, resultSetId).Returns(resultSet);
        _assistantChatRepository.GetLatestPendingActionAsync(ownerUserId).Returns((AssistantChatPendingAction?)null);

        var response = await _service.SendMessageAsync(ownerUserId, new AssistantChatMessageSendRequestDto
        {
            Message = "Now delete all these tweets"
        });

        response.AssistantMessage.Content.Should().Contain("prepared deletion");
        await _assistantChatRepository.Received(1).AddPendingActionAsync(
            Arg.Is<AssistantChatPendingAction>(action =>
                action.Status == AssistantChatActionStatus.PendingConfirmation &&
                action.ActionType == AssistantChatActionType.DeleteCaptures &&
                action.CaptureCount == 2 &&
                action.TargetResultSetId == resultSetId));
        await _captureBulkActionService.DidNotReceive().DeleteCapturesAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>());
    }

    [Fact]
    public async Task ConfirmActionAsync_ShouldCancel_WhenTargetIsOutsideCurrentResultSet()
    {
        var ownerUserId = Guid.NewGuid();
        var session = new AssistantChatSession
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            LastResultSetId = Guid.NewGuid()
        };
        var action = new AssistantChatPendingAction
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            OwnerUserId = ownerUserId,
            ActionType = AssistantChatActionType.DeleteCaptures,
            Status = AssistantChatActionStatus.PendingConfirmation,
            TargetResultSetId = Guid.NewGuid(),
            CaptureIdsJson = "[]",
            CaptureCount = 0
        };

        _assistantChatRepository.GetByOwnerAsync(ownerUserId).Returns(session);
        _assistantChatRepository.GetPendingActionAsync(ownerUserId, action.Id).Returns(action);

        var response = await _service.ConfirmActionAsync(ownerUserId, action.Id);

        response.Should().NotBeNull();
        response!.Action.Status.Should().Be(AssistantChatActionStatus.Cancelled);
        action.Status.Should().Be(AssistantChatActionStatus.Cancelled);
        await _captureBulkActionService.DidNotReceive().DeleteCapturesAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>());
    }

    [Fact]
    public async Task SendMessageAsync_GenericSearch_ShouldCreateSearchResultSetSnapshot()
    {
        var ownerUserId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        var session = new AssistantChatSession
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId
        };

        _assistantChatRepository.GetOrCreateSessionAsync(ownerUserId).Returns(session);
        _assistantChatRepository.GetMessagesAsync(ownerUserId).Returns([]);
        _captureBulkActionService.SearchCapturesAsync(
                ownerUserId,
                Arg.Any<CaptureSearchCriteria>(),
                Arg.Any<int>(),
                Arg.Any<int>())
            .Returns(new CaptureBulkQueryResult
            {
                CaptureIds = [captureId],
                TotalCount = 1,
                Summary = "Found 1 capture. Showing 1 result on page 1 (page size 20).",
                PreviewItems =
                [
                    new CaptureBulkPreviewItem
                    {
                        CaptureId = captureId,
                        SourceUrl = "https://example.com/capture",
                        ContentType = "Tweet",
                        Status = "Failed",
                        MatchReason = "text",
                        CreatedAt = DateTime.UtcNow
                    }
                ]
            });

        var response = await _service.SendMessageAsync(ownerUserId, new AssistantChatMessageSendRequestDto
        {
            Message = "Find failed captures about outage"
        });

        response.AssistantMessage.Content.Should().Contain("Found 1 capture");
        await _captureBulkActionService.Received(1).SearchCapturesAsync(
            ownerUserId,
            Arg.Is<CaptureSearchCriteria>(criteria =>
                criteria.Query == "failed captures about outage" &&
                criteria.Status == CaptureStatus.Failed &&
                criteria.Threshold == 0.6),
            Arg.Any<int>(),
            Arg.Any<int>());
        await _assistantChatRepository.Received(1).AddResultSetAsync(
            Arg.Is<AssistantChatResultSet>(resultSet =>
                resultSet.QueryType == "search_captures"
                && resultSet.TotalCount == 1
                && !string.IsNullOrWhiteSpace(resultSet.CriteriaJson)
                && resultSet.CriteriaJson != "{}"));
    }

    [Fact]
    public async Task SendMessageAsync_SearchCommandPrefix_ShouldNormalizeQueryForFallbackSearch()
    {
        var ownerUserId = Guid.NewGuid();
        var session = new AssistantChatSession
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId
        };

        _assistantChatRepository.GetOrCreateSessionAsync(ownerUserId).Returns(session);
        _assistantChatRepository.GetMessagesAsync(ownerUserId).Returns([]);
        _captureBulkActionService.SearchCapturesAsync(
                ownerUserId,
                Arg.Any<CaptureSearchCriteria>(),
                Arg.Any<int>(),
                Arg.Any<int>())
            .Returns(new CaptureBulkQueryResult());

        await _service.SendMessageAsync(ownerUserId, new AssistantChatMessageSendRequestDto
        {
            Message = "Search captures for outage investigation"
        });

        await _captureBulkActionService.Received(1).SearchCapturesAsync(
            ownerUserId,
            Arg.Is<CaptureSearchCriteria>(criteria => criteria.Query == "outage investigation"),
            Arg.Any<int>(),
            Arg.Any<int>());
    }

    [Fact]
    public async Task SendMessageAsync_SortFollowUp_ShouldReuseStoredSearchCriteria()
    {
        var ownerUserId = Guid.NewGuid();
        var session = new AssistantChatSession
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            LastResultSetId = Guid.NewGuid()
        };
        var storedCriteria = new CaptureSearchCriteria
        {
            Query = "incident timeline",
            Tags = ["ops"],
            TagMatchMode = SearchMatchModes.All,
            Page = 1,
            PageSize = 20,
            Threshold = 0.6,
            SortField = CaptureSearchSortFields.Relevance,
            SortDirection = SearchSortDirections.Desc
        };
        var previousResultSet = new AssistantChatResultSet
        {
            Id = session.LastResultSetId.Value,
            SessionId = session.Id,
            OwnerUserId = ownerUserId,
            QueryType = "search_captures",
            Summary = "Found 3 captures.",
            CaptureIdsJson = "[]",
            PreviewJson = "[]",
            CriteriaJson = System.Text.Json.JsonSerializer.Serialize(storedCriteria),
            TotalCount = 3
        };

        _assistantChatRepository.GetOrCreateSessionAsync(ownerUserId).Returns(session);
        _assistantChatRepository.GetMessagesAsync(ownerUserId).Returns([]);
        _assistantChatRepository.GetResultSetByIdAsync(ownerUserId, session.LastResultSetId.Value).Returns(previousResultSet);
        _captureBulkActionService.SearchCapturesAsync(
                ownerUserId,
                Arg.Any<CaptureSearchCriteria>(),
                Arg.Any<int>(),
                Arg.Any<int>())
            .Returns(new CaptureBulkQueryResult
            {
                CaptureIds = [Guid.NewGuid()],
                TotalCount = 1,
                Summary = "Found 1 capture.",
                PreviewItems = [],
                NormalizedCriteria = new CaptureSearchCriteria
                {
                    Query = "incident timeline",
                    Tags = ["ops"],
                    SortField = CaptureSearchSortFields.CreatedAt,
                    SortDirection = SearchSortDirections.Asc
                }
            });

        var response = await _service.SendMessageAsync(ownerUserId, new AssistantChatMessageSendRequestDto
        {
            Message = "Sort these by oldest"
        });

        response.AssistantMessage.Content.Should().Contain("Found 1 capture");
        await _captureBulkActionService.Received(1).SearchCapturesAsync(
            ownerUserId,
            Arg.Is<CaptureSearchCriteria>(criteria =>
                criteria.Query == "incident timeline"
                && criteria.Tags.SequenceEqual(new[] { "ops" })
                && criteria.SortField == CaptureSearchSortFields.CreatedAt
                && criteria.SortDirection == SearchSortDirections.Asc),
            Arg.Any<int>(),
            Arg.Any<int>());
    }

    [Fact]
    public async Task SendMessageAsync_SortFollowUpOnNonSearchResultSet_ShouldReturnGuidance()
    {
        var ownerUserId = Guid.NewGuid();
        var session = new AssistantChatSession
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            LastResultSetId = Guid.NewGuid()
        };
        var previousResultSet = new AssistantChatResultSet
        {
            Id = session.LastResultSetId.Value,
            SessionId = session.Id,
            OwnerUserId = ownerUserId,
            QueryType = "deleted_twitter_accounts",
            Summary = "Found 3 tweets.",
            CaptureIdsJson = "[]",
            PreviewJson = "[]",
            CriteriaJson = "{}",
            TotalCount = 3
        };

        _assistantChatRepository.GetOrCreateSessionAsync(ownerUserId).Returns(session);
        _assistantChatRepository.GetMessagesAsync(ownerUserId).Returns([]);
        _assistantChatRepository.GetResultSetByIdAsync(ownerUserId, session.LastResultSetId.Value).Returns(previousResultSet);

        var response = await _service.SendMessageAsync(ownerUserId, new AssistantChatMessageSendRequestDto
        {
            Message = "Sort these by newest"
        });

        response.AssistantMessage.Content.Should().Contain("Sorting is available only for results from search_captures");
        await _captureBulkActionService.DidNotReceive().SearchCapturesAsync(
            Arg.Any<Guid>(),
            Arg.Any<CaptureSearchCriteria>(),
            Arg.Any<int>(),
            Arg.Any<int>());
    }
}
