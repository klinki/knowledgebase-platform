using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SentinelKnowledgebase.Application.DTOs.Assistant;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public partial class AssistantChatService : IAssistantChatService
{
    private const int MaxResultSetSize = 5000;
    private const int PreviewSize = 20;
    private const double DefaultAssistantSearchThreshold = 0.6;
    private const string SearchCapturesTool = "search_captures";
    private const string FindDeletedTweetsTool = "find_deleted_tweets";
    private const string DeleteCurrentResultSetTool = "delete_current_result_set";
    private const string AddTagsToCurrentResultSetTool = "add_tags_to_current_result_set";
    private const string RemoveTagsFromCurrentResultSetTool = "remove_tags_from_current_result_set";
    private const string AddLabelsToCurrentResultSetTool = "add_labels_to_current_result_set";
    private const string RemoveLabelsFromCurrentResultSetTool = "remove_labels_from_current_result_set";

    private static readonly HashSet<string> ToolAllowlist = new(StringComparer.Ordinal)
    {
        SearchCapturesTool,
        FindDeletedTweetsTool,
        DeleteCurrentResultSetTool,
        AddTagsToCurrentResultSetTool,
        RemoveTagsFromCurrentResultSetTool,
        AddLabelsToCurrentResultSetTool,
        RemoveLabelsFromCurrentResultSetTool
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICaptureBulkActionService _captureBulkActionService;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AssistantChatService> _logger;

    public AssistantChatService(
        IUnitOfWork unitOfWork,
        ICaptureBulkActionService captureBulkActionService,
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<AssistantChatService> logger)
    {
        _unitOfWork = unitOfWork;
        _captureBulkActionService = captureBulkActionService;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AssistantChatSessionDto> GetSessionAsync(Guid ownerUserId)
    {
        var session = await _unitOfWork.AssistantChat.GetOrCreateSessionAsync(ownerUserId);
        session.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.AssistantChat.UpdateSessionAsync(session);
        await _unitOfWork.SaveChangesAsync();
        return MapSession(session);
    }

    public async Task<IReadOnlyList<AssistantChatMessageDto>> GetMessagesAsync(Guid ownerUserId)
    {
        var session = await _unitOfWork.AssistantChat.GetOrCreateSessionAsync(ownerUserId);
        session.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.AssistantChat.UpdateSessionAsync(session);
        await _unitOfWork.SaveChangesAsync();

        var messages = await _unitOfWork.AssistantChat.GetMessagesAsync(ownerUserId);
        return await MapMessagesAsync(ownerUserId, messages);
    }

    public async Task<AssistantChatMessageSendResponseDto> SendMessageAsync(
        Guid ownerUserId,
        AssistantChatMessageSendRequestDto request)
    {
        var text = request.Message.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(request));
        }

        var session = await _unitOfWork.AssistantChat.GetOrCreateSessionAsync(ownerUserId);
        var history = await _unitOfWork.AssistantChat.GetMessagesAsync(ownerUserId);
        var openAiPlan = await TryPlanWithOpenAiAsync(text, history, session.LastResultSetId);
        var plan = openAiPlan ?? BuildFallbackPlan(text);

        var userMessage = new AssistantChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = AssistantChatMessageRole.User,
            Content = text,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AssistantChat.AddMessageAsync(userMessage);

        ToolExecutionOutcome? lastOutcome = null;
        foreach (var toolCall in plan.ToolCalls.Take(4))
        {
            if (!ToolAllowlist.Contains(toolCall.Name))
            {
                _logger.LogWarning("Assistant tool call {ToolName} is not in allowlist.", toolCall.Name);
                continue;
            }

            lastOutcome = await ExecuteToolAsync(ownerUserId, session, toolCall);
        }

        var assistantContent = ResolveAssistantContent(plan, lastOutcome);
        var assistantMessage = new AssistantChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = AssistantChatMessageRole.Assistant,
            Content = assistantContent,
            ResultSetId = lastOutcome?.ResultSet?.Id,
            ActionId = lastOutcome?.Action?.Id,
            CreatedAt = DateTime.UtcNow
        };

        if (lastOutcome?.ResultSet != null)
        {
            await _unitOfWork.AssistantChat.AddResultSetAsync(lastOutcome.ResultSet);
            session.LastResultSetId = lastOutcome.ResultSet.Id;
        }

        if (lastOutcome?.Action != null)
        {
            await _unitOfWork.AssistantChat.AddPendingActionAsync(lastOutcome.Action);
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.AssistantChat.UpdateSessionAsync(session);
        await _unitOfWork.AssistantChat.AddMessageAsync(assistantMessage);
        await _unitOfWork.SaveChangesAsync();

        var messageDtos = await MapMessagesAsync(ownerUserId, [userMessage, assistantMessage]);
        return new AssistantChatMessageSendResponseDto
        {
            UserMessage = messageDtos[0],
            AssistantMessage = messageDtos[1]
        };
    }

    public async Task<AssistantChatActionResponseDto?> ConfirmActionAsync(Guid ownerUserId, Guid actionId)
    {
        var session = await _unitOfWork.AssistantChat.GetByOwnerAsync(ownerUserId);
        var action = await _unitOfWork.AssistantChat.GetPendingActionAsync(ownerUserId, actionId);
        if (session == null || action == null)
        {
            return null;
        }

        if (action.Status != AssistantChatActionStatus.PendingConfirmation)
        {
            return BuildActionResponse(action, "This action is no longer pending confirmation.");
        }

        if (session.LastResultSetId != action.TargetResultSetId)
        {
            action.Status = AssistantChatActionStatus.Cancelled;
            action.CancelledAt = DateTime.UtcNow;
            await _unitOfWork.AssistantChat.UpdatePendingActionAsync(action);
            session.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.AssistantChat.UpdateSessionAsync(session);
            await _unitOfWork.SaveChangesAsync();

            return BuildActionResponse(action, "I cancelled that delete action because the active result set changed.");
        }

        var captureIds = ParseCaptureIds(action.CaptureIdsJson);
        var deletedCount = await _captureBulkActionService.DeleteCapturesAsync(ownerUserId, captureIds);
        action.Status = AssistantChatActionStatus.Executed;
        action.ConfirmedAt = DateTime.UtcNow;
        action.ExecutedAt = DateTime.UtcNow;
        action.ExecutedCount = deletedCount;
        await _unitOfWork.AssistantChat.UpdatePendingActionAsync(action);

        var assistantMessage = new AssistantChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = AssistantChatMessageRole.Assistant,
            Content = deletedCount == 1
                ? "Deleted 1 capture from the current result set."
                : $"Deleted {deletedCount} captures from the current result set.",
            ActionId = action.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.AssistantChat.AddMessageAsync(assistantMessage);
        session.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.AssistantChat.UpdateSessionAsync(session);
        await _unitOfWork.SaveChangesAsync();

        return BuildActionResponse(action, assistantMessage.Content);
    }

    public async Task<AssistantChatActionResponseDto?> CancelActionAsync(Guid ownerUserId, Guid actionId)
    {
        var session = await _unitOfWork.AssistantChat.GetByOwnerAsync(ownerUserId);
        var action = await _unitOfWork.AssistantChat.GetPendingActionAsync(ownerUserId, actionId);
        if (session == null || action == null)
        {
            return null;
        }

        if (action.Status == AssistantChatActionStatus.PendingConfirmation)
        {
            action.Status = AssistantChatActionStatus.Cancelled;
            action.CancelledAt = DateTime.UtcNow;
            await _unitOfWork.AssistantChat.UpdatePendingActionAsync(action);
        }

        var assistantMessage = new AssistantChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = AssistantChatMessageRole.Assistant,
            Content = "Cancelled. No captures were deleted.",
            ActionId = action.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.AssistantChat.AddMessageAsync(assistantMessage);
        session.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.AssistantChat.UpdateSessionAsync(session);
        await _unitOfWork.SaveChangesAsync();

        return BuildActionResponse(action, assistantMessage.Content);
    }

    private AssistantChatActionResponseDto BuildActionResponse(AssistantChatPendingAction action, string assistantMessage)
    {
        return new AssistantChatActionResponseDto
        {
            Action = MapAction(action),
            AssistantMessage = new AssistantChatMessageDto
            {
                Id = Guid.NewGuid(),
                Role = AssistantChatMessageRole.Assistant,
                Content = assistantMessage,
                CreatedAt = DateTime.UtcNow,
                Action = MapAction(action)
            }
        };
    }

    private async Task<ToolExecutionOutcome?> ExecuteToolAsync(
        Guid ownerUserId,
        AssistantChatSession session,
        PlannedToolCall toolCall)
    {
        return toolCall.Name switch
        {
            SearchCapturesTool => await ExecuteSearchCapturesAsync(ownerUserId, session, toolCall),
            FindDeletedTweetsTool => await ExecuteFindDeletedTweetsAsync(ownerUserId, session),
            DeleteCurrentResultSetTool => await ExecuteDeleteCurrentResultSetAsync(ownerUserId, session),
            AddTagsToCurrentResultSetTool => await ExecuteAddTagsAsync(ownerUserId, session, toolCall),
            RemoveTagsFromCurrentResultSetTool => await ExecuteRemoveTagsAsync(ownerUserId, session, toolCall),
            AddLabelsToCurrentResultSetTool => await ExecuteAddLabelsAsync(ownerUserId, session, toolCall),
            RemoveLabelsFromCurrentResultSetTool => await ExecuteRemoveLabelsAsync(ownerUserId, session, toolCall),
            _ => null
        };
    }

    private async Task<ToolExecutionOutcome> ExecuteSearchCapturesAsync(
        Guid ownerUserId,
        AssistantChatSession session,
        PlannedToolCall toolCall)
    {
        var criteria = new CaptureSearchCriteria
        {
            Query = toolCall.Query,
            Tags = toolCall.Tags,
            TagMatchMode = toolCall.TagMatchMode,
            Labels = toolCall.Labels,
            LabelMatchMode = toolCall.LabelMatchMode,
            Page = toolCall.Page ?? 1,
            PageSize = toolCall.PageSize ?? PreviewSize,
            Threshold = toolCall.Threshold ?? DefaultAssistantSearchThreshold,
            ContentType = toolCall.ContentType,
            Status = toolCall.Status,
            DateFrom = toolCall.DateFrom,
            DateTo = toolCall.DateTo,
            SortField = toolCall.SortField,
            SortDirection = toolCall.SortDirection
        };

        if (!HasAtLeastOneSearchCriterion(criteria) && HasSortCriterion(criteria))
        {
            var currentResultSet = await TryGetCurrentResultSetAsync(ownerUserId, session);
            if (currentResultSet == null)
            {
                return new ToolExecutionOutcome
                {
                    AssistantContent = "There is no active search result set to sort yet. Run a search first."
                };
            }

            if (!string.Equals(currentResultSet.QueryType, SearchCapturesTool, StringComparison.OrdinalIgnoreCase))
            {
                return new ToolExecutionOutcome
                {
                    AssistantContent = "Sorting is available only for results from search_captures. Run a sortable search first."
                };
            }

            var storedCriteria = ParseResultSetCriteria(currentResultSet.CriteriaJson);
            if (storedCriteria == null || !HasAtLeastOneSearchCriterion(storedCriteria))
            {
                return new ToolExecutionOutcome
                {
                    AssistantContent = "I could not read criteria for the current result set. Run a new search before sorting."
                };
            }

            criteria = storedCriteria;
            criteria.SortField = toolCall.SortField;
            criteria.SortDirection = toolCall.SortDirection;
            criteria.Page = toolCall.Page ?? criteria.Page;
            criteria.PageSize = toolCall.PageSize ?? criteria.PageSize;
        }

        if (!HasAtLeastOneSearchCriterion(criteria))
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "Provide at least one search criterion: query, tags, labels, content type, status, or date range."
            };
        }

        if (criteria.DateFrom.HasValue && criteria.DateTo.HasValue && criteria.DateFrom > criteria.DateTo)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "Invalid date range: dateFrom must be earlier than or equal to dateTo."
            };
        }

        var previewSize = Math.Clamp(criteria.PageSize, 1, 100);
        var result = await _captureBulkActionService.SearchCapturesAsync(
            ownerUserId,
            criteria,
            MaxResultSetSize,
            previewSize);

        var resultSet = new AssistantChatResultSet
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            OwnerUserId = ownerUserId,
            QueryType = "search_captures",
            Summary = result.Summary,
            CaptureIdsJson = JsonSerializer.Serialize(result.CaptureIds),
            PreviewJson = JsonSerializer.Serialize(result.PreviewItems),
            CriteriaJson = JsonSerializer.Serialize(result.NormalizedCriteria ?? criteria),
            TotalCount = result.TotalCount,
            CreatedAt = DateTime.UtcNow
        };

        var content = result.TotalCount == 0
            ? "I found no captures for the current search criteria."
            : result.Summary;

        return new ToolExecutionOutcome
        {
            AssistantContent = content,
            ResultSet = resultSet
        };
    }

    private async Task<ToolExecutionOutcome> ExecuteFindDeletedTweetsAsync(Guid ownerUserId, AssistantChatSession session)
    {
        var result = await _captureBulkActionService.FindDeletedTweetsFromUnavailableAccountsAsync(
            ownerUserId,
            MaxResultSetSize,
            PreviewSize);

        var resultSet = new AssistantChatResultSet
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            OwnerUserId = ownerUserId,
            QueryType = "deleted_twitter_accounts",
            Summary = result.Summary,
            CaptureIdsJson = JsonSerializer.Serialize(result.CaptureIds),
            PreviewJson = JsonSerializer.Serialize(result.PreviewItems),
            CriteriaJson = "{}",
            TotalCount = result.TotalCount,
            CreatedAt = DateTime.UtcNow
        };

        var content = result.TotalCount == 0
            ? "I found no tweets from deleted or unavailable accounts."
            : $"{result.Summary} I can delete these after you confirm.";

        return new ToolExecutionOutcome
        {
            AssistantContent = content,
            ResultSet = resultSet
        };
    }

    private async Task<ToolExecutionOutcome> ExecuteDeleteCurrentResultSetAsync(Guid ownerUserId, AssistantChatSession session)
    {
        if (!session.LastResultSetId.HasValue)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "There is no active result set yet. Run a search first."
            };
        }

        var resultSet = await _unitOfWork.AssistantChat.GetResultSetByIdAsync(ownerUserId, session.LastResultSetId.Value);
        if (resultSet == null)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "I could not find the current result set. Run the search again."
            };
        }

        var captureIds = ParseCaptureIds(resultSet.CaptureIdsJson);
        if (captureIds.Count == 0)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "The current result set is empty, so there is nothing to delete."
            };
        }

        var latestPending = await _unitOfWork.AssistantChat.GetLatestPendingActionAsync(ownerUserId);
        if (latestPending != null)
        {
            latestPending.Status = AssistantChatActionStatus.Cancelled;
            latestPending.CancelledAt = DateTime.UtcNow;
            await _unitOfWork.AssistantChat.UpdatePendingActionAsync(latestPending);
        }

        var action = new AssistantChatPendingAction
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            OwnerUserId = ownerUserId,
            ActionType = AssistantChatActionType.DeleteCaptures,
            Status = AssistantChatActionStatus.PendingConfirmation,
            TargetResultSetId = resultSet.Id,
            CaptureIdsJson = JsonSerializer.Serialize(captureIds),
            CaptureCount = captureIds.Count,
            CreatedAt = DateTime.UtcNow
        };

        return new ToolExecutionOutcome
        {
            AssistantContent = captureIds.Count == 1
                ? "I prepared deletion for 1 capture from the current result set. Confirm to proceed."
                : $"I prepared deletion for {captureIds.Count} captures from the current result set. Confirm to proceed.",
            Action = action
        };
    }

    private async Task<ToolExecutionOutcome> ExecuteAddTagsAsync(
        Guid ownerUserId,
        AssistantChatSession session,
        PlannedToolCall toolCall)
    {
        var currentResultSet = await TryGetCurrentResultSetAsync(ownerUserId, session);
        if (currentResultSet == null)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "There is no active result set yet. Run a search first."
            };
        }

        if (toolCall.Tags.Count == 0)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "Specify at least one tag to add."
            };
        }

        var captureIds = ParseCaptureIds(currentResultSet.CaptureIdsJson);
        var result = await _captureBulkActionService.AddTagsAsync(ownerUserId, captureIds, toolCall.Tags);
        return new ToolExecutionOutcome
        {
            AssistantContent = result.MutatedCount == 1
                ? "Added tags to 1 capture in the current result set."
                : $"Added tags to {result.MutatedCount} captures in the current result set."
        };
    }

    private async Task<ToolExecutionOutcome> ExecuteRemoveTagsAsync(
        Guid ownerUserId,
        AssistantChatSession session,
        PlannedToolCall toolCall)
    {
        var currentResultSet = await TryGetCurrentResultSetAsync(ownerUserId, session);
        if (currentResultSet == null)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "There is no active result set yet. Run a search first."
            };
        }

        if (toolCall.Tags.Count == 0)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "Specify at least one tag to remove."
            };
        }

        var captureIds = ParseCaptureIds(currentResultSet.CaptureIdsJson);
        var result = await _captureBulkActionService.RemoveTagsAsync(ownerUserId, captureIds, toolCall.Tags);
        return new ToolExecutionOutcome
        {
            AssistantContent = result.MutatedCount == 1
                ? "Removed tags from 1 capture in the current result set."
                : $"Removed tags from {result.MutatedCount} captures in the current result set."
        };
    }

    private async Task<ToolExecutionOutcome> ExecuteAddLabelsAsync(
        Guid ownerUserId,
        AssistantChatSession session,
        PlannedToolCall toolCall)
    {
        var currentResultSet = await TryGetCurrentResultSetAsync(ownerUserId, session);
        if (currentResultSet == null)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "There is no active result set yet. Run a search first."
            };
        }

        if (toolCall.Labels.Count == 0)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "Specify labels in the format category:value."
            };
        }

        var captureIds = ParseCaptureIds(currentResultSet.CaptureIdsJson);
        var result = await _captureBulkActionService.AddLabelsAsync(ownerUserId, captureIds, toolCall.Labels);
        return new ToolExecutionOutcome
        {
            AssistantContent = result.MutatedCount == 1
                ? "Updated labels on 1 capture in the current result set."
                : $"Updated labels on {result.MutatedCount} captures in the current result set."
        };
    }

    private async Task<ToolExecutionOutcome> ExecuteRemoveLabelsAsync(
        Guid ownerUserId,
        AssistantChatSession session,
        PlannedToolCall toolCall)
    {
        var currentResultSet = await TryGetCurrentResultSetAsync(ownerUserId, session);
        if (currentResultSet == null)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "There is no active result set yet. Run a search first."
            };
        }

        if (toolCall.Labels.Count == 0)
        {
            return new ToolExecutionOutcome
            {
                AssistantContent = "Specify labels in the format category:value."
            };
        }

        var captureIds = ParseCaptureIds(currentResultSet.CaptureIdsJson);
        var result = await _captureBulkActionService.RemoveLabelsAsync(ownerUserId, captureIds, toolCall.Labels);
        return new ToolExecutionOutcome
        {
            AssistantContent = result.MutatedCount == 1
                ? "Removed labels from 1 capture in the current result set."
                : $"Removed labels from {result.MutatedCount} captures in the current result set."
        };
    }

    private async Task<AssistantChatResultSet?> TryGetCurrentResultSetAsync(Guid ownerUserId, AssistantChatSession session)
    {
        if (!session.LastResultSetId.HasValue)
        {
            return null;
        }

        return await _unitOfWork.AssistantChat.GetResultSetByIdAsync(ownerUserId, session.LastResultSetId.Value);
    }

    private static string ResolveAssistantContent(AssistantPlan plan, ToolExecutionOutcome? lastOutcome)
    {
        if (!string.IsNullOrWhiteSpace(lastOutcome?.AssistantContent))
        {
            return lastOutcome.AssistantContent;
        }

        if (!string.IsNullOrWhiteSpace(plan.AssistantMessage))
        {
            return plan.AssistantMessage.Trim();
        }

        return "I can search captures, update tags or labels on the active result set, and prepare safe deletions with confirmation.";
    }

    private async Task<AssistantPlan?> TryPlanWithOpenAiAsync(
        string userMessage,
        IReadOnlyList<AssistantChatMessage> history,
        Guid? lastResultSetId)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            var model = _configuration["OpenAI:AssistantModel"]
                        ?? _configuration["OpenAI:Model"]
                        ?? "gpt-4.1-mini";
            var chatCompletionsUrl = _configuration["OpenAI:ChatCompletionsUrl"] ?? "https://api.openai.com/v1/chat/completions";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var systemPrompt = $"""
You are an assistant for capture management. Plan only from this tool allowlist:
- {SearchCapturesTool}
- {FindDeletedTweetsTool}
- {DeleteCurrentResultSetTool}
- {AddTagsToCurrentResultSetTool}
- {RemoveTagsFromCurrentResultSetTool}
- {AddLabelsToCurrentResultSetTool}
- {RemoveLabelsFromCurrentResultSetTool}

Rules:
- Use {SearchCapturesTool} for generic capture lookup. Arguments can include:
  query, tags, tagMatchMode(any|all), labels(array of category/value pairs), labelMatchMode(any|all),
  page, pageSize, threshold, contentType(Tweet|Article|Code|Note|Other),
  status(Pending|Processing|Completed|Failed), dateFrom, dateTo,
  sortField(relevance|createdAt|status|contentType|sourceUrl), sortDirection(asc|desc).
- For follow-up sort requests like "sort these by newest", call {SearchCapturesTool} with just sortField/sortDirection.
- Delete must only target the active result set and should be proposed, not executed.
- For deleted twitter accounts, always use deterministic skip-code matching.
- Output JSON object with fields:
  - assistantMessage: string
  - toolCalls: array of objects with name and arguments fields
- If no tool call is needed, return toolCalls as [].
""";

            var context = history.TakeLast(8).Select(message => $"{message.Role}: {message.Content}");
            var contextText = string.Join("\n", context);
            var userPrompt = $"LastResultSetId: {(lastResultSetId?.ToString() ?? "none")}\nRecentMessages:\n{contextText}\nUserMessage: {userMessage}";

            var request = new OpenAiPlanRequest
            {
                Model = model,
                Messages =
                [
                    new OpenAiPlanMessage("system", systemPrompt),
                    new OpenAiPlanMessage("user", userPrompt)
                ],
                Temperature = 0,
                ResponseFormat = new OpenAiResponseFormat("json_object")
            };

            var response = await _httpClient.PostAsJsonAsync(chatCompletionsUrl, request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<OpenAiPlanResponse>();
            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var parsed = JsonSerializer.Deserialize<OpenAiPlannedPayload>(content);
            if (parsed == null)
            {
                return null;
            }

            var toolCalls = parsed.ToolCalls?
                .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                .Select(item => PlannedToolCall.From(item.Name!, item.Arguments))
                .ToList() ?? [];

            return new AssistantPlan
            {
                AssistantMessage = parsed.AssistantMessage ?? string.Empty,
                ToolCalls = toolCalls
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI assistant planner failed. Falling back to deterministic planner.");
            return null;
        }
    }

    private AssistantPlan BuildFallbackPlan(string message)
    {
        var lower = message.Trim().ToLowerInvariant();
        var plan = new AssistantPlan();

        if (lower.Contains("deleted account") && lower.Contains("tweet"))
        {
            plan.ToolCalls.Add(new PlannedToolCall { Name = FindDeletedTweetsTool });
            return plan;
        }

        if (lower.Contains("delete all these") || lower.Contains("delete these") || lower.Contains("delete them"))
        {
            plan.ToolCalls.Add(new PlannedToolCall { Name = DeleteCurrentResultSetTool });
            return plan;
        }

        if (TryParseFallbackSortIntent(lower, out var sortField, out var sortDirection))
        {
            plan.ToolCalls.Add(new PlannedToolCall
            {
                Name = SearchCapturesTool,
                SortField = sortField,
                SortDirection = sortDirection
            });
            return plan;
        }

        var addTagMatch = AddTagRegex().Match(message);
        if (addTagMatch.Success)
        {
            plan.ToolCalls.Add(new PlannedToolCall
            {
                Name = AddTagsToCurrentResultSetTool,
                Tags = ParseCommaList(addTagMatch.Groups["tags"].Value)
            });
            return plan;
        }

        var removeTagMatch = RemoveTagRegex().Match(message);
        if (removeTagMatch.Success)
        {
            plan.ToolCalls.Add(new PlannedToolCall
            {
                Name = RemoveTagsFromCurrentResultSetTool,
                Tags = ParseCommaList(removeTagMatch.Groups["tags"].Value)
            });
            return plan;
        }

        var addLabelMatch = AddLabelRegex().Match(message);
        if (addLabelMatch.Success)
        {
            plan.ToolCalls.Add(new PlannedToolCall
            {
                Name = AddLabelsToCurrentResultSetTool,
                Labels =
                [
                    new LabelAssignmentDto
                    {
                        Category = addLabelMatch.Groups["category"].Value.Trim(),
                        Value = addLabelMatch.Groups["value"].Value.Trim()
                    }
                ]
            });
            return plan;
        }

        var removeLabelMatch = RemoveLabelRegex().Match(message);
        if (removeLabelMatch.Success)
        {
            plan.ToolCalls.Add(new PlannedToolCall
            {
                Name = RemoveLabelsFromCurrentResultSetTool,
                Labels =
                [
                    new LabelAssignmentDto
                    {
                        Category = removeLabelMatch.Groups["category"].Value.Trim(),
                        Value = removeLabelMatch.Groups["value"].Value.Trim()
                    }
                ]
            });
            return plan;
        }

        if (LooksLikeSearchIntent(lower))
        {
            plan.ToolCalls.Add(BuildFallbackSearchToolCall(message, lower));
            return plan;
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            plan.ToolCalls.Add(BuildFallbackSearchToolCall(message, lower));
            return plan;
        }

        return plan;
    }

    private static List<Guid> ParseCaptureIds(string captureIdsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(captureIdsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<AssistantChatMessageDto>> MapMessagesAsync(
        Guid ownerUserId,
        IReadOnlyList<AssistantChatMessage> messages)
    {
        var resultSetIds = messages
            .Where(message => message.ResultSetId.HasValue)
            .Select(message => message.ResultSetId!.Value)
            .Distinct()
            .ToList();
        var actionIds = messages
            .Where(message => message.ActionId.HasValue)
            .Select(message => message.ActionId!.Value)
            .Distinct()
            .ToList();

        var resultSets = await _unitOfWork.AssistantChat.GetResultSetsByIdsAsync(ownerUserId, resultSetIds);
        var actions = await _unitOfWork.AssistantChat.GetPendingActionsByIdsAsync(ownerUserId, actionIds);

        return messages.Select(message => new AssistantChatMessageDto
        {
            Id = message.Id,
            Role = message.Role,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            ResultSet = message.ResultSetId.HasValue && resultSets.TryGetValue(message.ResultSetId.Value, out var resultSet)
                ? MapResultSet(resultSet)
                : null,
            Action = message.ActionId.HasValue && actions.TryGetValue(message.ActionId.Value, out var action)
                ? MapAction(action)
                : null
        }).ToList();
    }

    private static AssistantChatSessionDto MapSession(AssistantChatSession session)
    {
        return new AssistantChatSessionDto
        {
            Id = session.Id,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    private static AssistantChatResultSetDto MapResultSet(AssistantChatResultSet resultSet)
    {
        var previewItems = ParsePreviewItems(resultSet.PreviewJson);
        return new AssistantChatResultSetDto
        {
            Id = resultSet.Id,
            QueryType = resultSet.QueryType,
            Summary = resultSet.Summary,
            TotalCount = resultSet.TotalCount,
            CreatedAt = resultSet.CreatedAt,
            PreviewItems = previewItems.Select(item => new AssistantChatResultSetItemDto
            {
                CaptureId = item.CaptureId,
                SourceUrl = item.SourceUrl,
                ContentType = item.ContentType,
                Status = item.Status,
                Similarity = item.Similarity,
                MatchReason = item.MatchReason,
                PreviewText = item.PreviewText,
                SkipCode = item.SkipCode,
                SkipReason = item.SkipReason,
                CreatedAt = item.CreatedAt
            }).ToList()
        };
    }

    private static AssistantChatActionDto MapAction(AssistantChatPendingAction action)
    {
        return new AssistantChatActionDto
        {
            Id = action.Id,
            ActionType = action.ActionType,
            Status = action.Status,
            TargetResultSetId = action.TargetResultSetId,
            CaptureCount = action.CaptureCount,
            ExecutedCount = action.ExecutedCount,
            CreatedAt = action.CreatedAt,
            ConfirmedAt = action.ConfirmedAt,
            CancelledAt = action.CancelledAt,
            ExecutedAt = action.ExecutedAt
        };
    }

    private static List<CaptureBulkPreviewItem> ParsePreviewItems(string previewJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<CaptureBulkPreviewItem>>(previewJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<string> ParseCommaList(string raw)
    {
        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool HasAtLeastOneSearchCriterion(CaptureSearchCriteria criteria)
    {
        return !string.IsNullOrWhiteSpace(criteria.Query)
               || criteria.Tags.Count > 0
               || criteria.Labels.Count > 0
               || criteria.ContentType.HasValue
               || criteria.Status.HasValue
               || criteria.DateFrom.HasValue
               || criteria.DateTo.HasValue;
    }

    private static bool HasSortCriterion(CaptureSearchCriteria criteria)
    {
        return !string.IsNullOrWhiteSpace(criteria.SortField)
               || !string.IsNullOrWhiteSpace(criteria.SortDirection);
    }

    private static bool LooksLikeSearchIntent(string lowerMessage)
    {
        return lowerMessage.Contains("find ")
               || lowerMessage.StartsWith("find", StringComparison.Ordinal)
               || lowerMessage.Contains("search")
               || lowerMessage.Contains("show ")
               || lowerMessage.Contains("list ");
    }

    private static PlannedToolCall BuildFallbackSearchToolCall(string message, string lowerMessage)
    {
        var normalizedQuery = NormalizeFallbackSearchQuery(message);
        return new PlannedToolCall
        {
            Name = SearchCapturesTool,
            Query = normalizedQuery,
            ContentType = InferContentType(lowerMessage),
            Status = InferStatus(lowerMessage)
        };
    }

    private static bool TryParseFallbackSortIntent(string lowerMessage, out string sortField, out string sortDirection)
    {
        sortField = string.Empty;
        sortDirection = SearchSortDirections.Desc;

        if (!lowerMessage.Contains("sort", StringComparison.Ordinal))
        {
            return false;
        }

        if (lowerMessage.Contains("newest", StringComparison.Ordinal)
            || lowerMessage.Contains("latest", StringComparison.Ordinal)
            || lowerMessage.Contains("most recent", StringComparison.Ordinal))
        {
            sortField = CaptureSearchSortFields.CreatedAt;
            sortDirection = SearchSortDirections.Desc;
            return true;
        }

        if (lowerMessage.Contains("oldest", StringComparison.Ordinal)
            || lowerMessage.Contains("earliest", StringComparison.Ordinal))
        {
            sortField = CaptureSearchSortFields.CreatedAt;
            sortDirection = SearchSortDirections.Asc;
            return true;
        }

        if (lowerMessage.Contains("relevance", StringComparison.Ordinal)
            || lowerMessage.Contains("best match", StringComparison.Ordinal)
            || lowerMessage.Contains("most relevant", StringComparison.Ordinal))
        {
            sortField = CaptureSearchSortFields.Relevance;
            sortDirection = SearchSortDirections.Desc;
            return true;
        }

        if (lowerMessage.Contains("source", StringComparison.Ordinal)
            || lowerMessage.Contains("url", StringComparison.Ordinal))
        {
            sortField = CaptureSearchSortFields.SourceUrl;
            sortDirection = lowerMessage.Contains("z-a", StringComparison.Ordinal) ? SearchSortDirections.Desc : SearchSortDirections.Asc;
            return true;
        }

        if (lowerMessage.Contains("status", StringComparison.Ordinal))
        {
            sortField = CaptureSearchSortFields.Status;
            sortDirection = SearchSortDirections.Asc;
            return true;
        }

        if (lowerMessage.Contains("type", StringComparison.Ordinal)
            || lowerMessage.Contains("content type", StringComparison.Ordinal))
        {
            sortField = CaptureSearchSortFields.ContentType;
            sortDirection = SearchSortDirections.Asc;
            return true;
        }

        if (lowerMessage.Contains("created", StringComparison.Ordinal)
            || lowerMessage.Contains("date", StringComparison.Ordinal)
            || lowerMessage.Contains("time", StringComparison.Ordinal))
        {
            sortField = CaptureSearchSortFields.CreatedAt;
            sortDirection = lowerMessage.Contains("asc", StringComparison.Ordinal) ? SearchSortDirections.Asc : SearchSortDirections.Desc;
            return true;
        }

        return false;
    }

    private static CaptureSearchCriteria? ParseResultSetCriteria(string criteriaJson)
    {
        if (string.IsNullOrWhiteSpace(criteriaJson) || criteriaJson == "{}")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CaptureSearchCriteria>(criteriaJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NormalizeFallbackSearchQuery(string message)
    {
        var normalized = message.Trim();
        if (normalized.Length == 0)
        {
            return normalized;
        }

        var prefixes = new[]
        {
            "search captures for ",
            "search capture for ",
            "search for ",
            "search captures ",
            "search capture ",
            "search ",
            "find me ",
            "find ",
            "show me ",
            "show ",
            "list me ",
            "list "
        };

        foreach (var prefix in prefixes)
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var remainder = normalized[prefix.Length..].Trim();
            if (remainder.Length > 0)
            {
                return remainder;
            }
        }

        return normalized;
    }

    private static ContentType? InferContentType(string lowerMessage)
    {
        if (lowerMessage.Contains("tweet"))
        {
            return ContentType.Tweet;
        }

        if (lowerMessage.Contains("article"))
        {
            return ContentType.Article;
        }

        if (lowerMessage.Contains("code"))
        {
            return ContentType.Code;
        }

        if (lowerMessage.Contains("note"))
        {
            return ContentType.Note;
        }

        if (lowerMessage.Contains("other"))
        {
            return ContentType.Other;
        }

        return null;
    }

    private static CaptureStatus? InferStatus(string lowerMessage)
    {
        if (lowerMessage.Contains("completed"))
        {
            return CaptureStatus.Completed;
        }

        if (lowerMessage.Contains("failed"))
        {
            return CaptureStatus.Failed;
        }

        if (lowerMessage.Contains("pending"))
        {
            return CaptureStatus.Pending;
        }

        if (lowerMessage.Contains("processing"))
        {
            return CaptureStatus.Processing;
        }

        return null;
    }

    [GeneratedRegex(@"(?i)add\s+tags?\s+(?<tags>.+)$")]
    private static partial Regex AddTagRegex();

    [GeneratedRegex(@"(?i)remove\s+tags?\s+(?<tags>.+)$")]
    private static partial Regex RemoveTagRegex();

    [GeneratedRegex(@"(?i)add\s+label\s+(?<category>[^:]+):(?<value>.+)$")]
    private static partial Regex AddLabelRegex();

    [GeneratedRegex(@"(?i)remove\s+label\s+(?<category>[^:]+):(?<value>.+)$")]
    private static partial Regex RemoveLabelRegex();

    private sealed class AssistantPlan
    {
        public string AssistantMessage { get; set; } = string.Empty;
        public List<PlannedToolCall> ToolCalls { get; set; } = new();
    }

    private sealed class PlannedToolCall
    {
        public string Name { get; set; } = string.Empty;
        public string? Query { get; set; }
        public List<string> Tags { get; set; } = new();
        public string TagMatchMode { get; set; } = SearchMatchModes.Any;
        public List<LabelAssignmentDto> Labels { get; set; } = new();
        public string LabelMatchMode { get; set; } = SearchMatchModes.All;
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public double? Threshold { get; set; }
        public ContentType? ContentType { get; set; }
        public CaptureStatus? Status { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? SortField { get; set; }
        public string? SortDirection { get; set; }

        public static PlannedToolCall From(string name, JsonElement? arguments)
        {
            var call = new PlannedToolCall { Name = name.Trim() };
            if (arguments is not { } json || json.ValueKind != JsonValueKind.Object)
            {
                return call;
            }

            call.Query = ReadString(json, "query");
            call.Tags = ReadStringArray(json, "tags");
            call.TagMatchMode = ParseMatchMode(ReadString(json, "tagMatchMode"), SearchMatchModes.Any);
            call.Labels = ReadLabels(json, "labels");
            call.LabelMatchMode = ParseMatchMode(ReadString(json, "labelMatchMode"), SearchMatchModes.All);
            call.Page = ReadInt(json, "page");
            call.PageSize = ReadInt(json, "pageSize");
            call.Threshold = ReadDouble(json, "threshold");
            call.ContentType = ReadEnum<ContentType>(json, "contentType");
            call.Status = ReadEnum<CaptureStatus>(json, "status");
            call.DateFrom = ReadDateTime(json, "dateFrom", endOfDay: false);
            call.DateTo = ReadDateTime(json, "dateTo", endOfDay: true);
            call.SortField = ParseSortField(ReadString(json, "sortField"));
            call.SortDirection = ParseSortDirection(ReadString(json, "sortDirection"));

            return call;
        }

        private static string? ReadString(JsonElement json, string propertyName)
        {
            if (!json.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var normalized = value.GetString()?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static List<string> ReadStringArray(JsonElement json, string propertyName)
        {
            if (!json.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return values.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString()!.Trim())
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<LabelAssignmentDto> ReadLabels(JsonElement json, string propertyName)
        {
            if (!json.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return values.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item =>
                {
                    item.TryGetProperty("category", out var categoryElement);
                    item.TryGetProperty("value", out var valueElement);
                    return new LabelAssignmentDto
                    {
                        Category = categoryElement.ValueKind == JsonValueKind.String ? categoryElement.GetString() ?? string.Empty : string.Empty,
                        Value = valueElement.ValueKind == JsonValueKind.String ? valueElement.GetString() ?? string.Empty : string.Empty
                    };
                })
                .Where(label => !string.IsNullOrWhiteSpace(label.Category) && !string.IsNullOrWhiteSpace(label.Value))
                .ToList();
        }

        private static string ParseMatchMode(string? value, string fallback)
        {
            return SearchMatchModes.IsValid(value) ? value!.ToLowerInvariant() : fallback;
        }

        private static string? ParseSortField(string? value)
        {
            if (!CaptureSearchSortFields.IsValid(value))
            {
                return null;
            }

            var normalized = value!.Trim();
            return normalized switch
            {
                _ when string.Equals(normalized, CaptureSearchSortFields.Relevance, StringComparison.OrdinalIgnoreCase) =>
                    CaptureSearchSortFields.Relevance,
                _ when string.Equals(normalized, CaptureSearchSortFields.CreatedAt, StringComparison.OrdinalIgnoreCase) =>
                    CaptureSearchSortFields.CreatedAt,
                _ when string.Equals(normalized, CaptureSearchSortFields.Status, StringComparison.OrdinalIgnoreCase) =>
                    CaptureSearchSortFields.Status,
                _ when string.Equals(normalized, CaptureSearchSortFields.ContentType, StringComparison.OrdinalIgnoreCase) =>
                    CaptureSearchSortFields.ContentType,
                _ => CaptureSearchSortFields.SourceUrl
            };
        }

        private static string? ParseSortDirection(string? value)
        {
            if (!SearchSortDirections.IsValid(value))
            {
                return null;
            }

            return value!.Trim().ToLowerInvariant();
        }

        private static int? ReadInt(JsonElement json, string propertyName)
        {
            if (!json.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }

            return null;
        }

        private static double? ReadDouble(JsonElement json, string propertyName)
        {
            if (!json.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }

            return null;
        }

        private static TEnum? ReadEnum<TEnum>(JsonElement json, string propertyName)
            where TEnum : struct, Enum
        {
            if (!json.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.String &&
                Enum.TryParse<TEnum>(value.GetString(), true, out var enumValue))
            {
                return enumValue;
            }

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var intValue) &&
                Enum.IsDefined(typeof(TEnum), intValue))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
            }

            return null;
        }

        private static DateTime? ReadDateTime(JsonElement json, string propertyName, bool endOfDay)
        {
            if (!json.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var raw = value.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
            {
                var time = endOfDay ? TimeOnly.MaxValue : TimeOnly.MinValue;
                return DateTime.SpecifyKind(dateOnly.ToDateTime(time), DateTimeKind.Utc);
            }

            if (DateTimeOffset.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var dateTimeOffset))
            {
                return dateTimeOffset.UtcDateTime;
            }

            return null;
        }
    }

    private sealed class ToolExecutionOutcome
    {
        public string AssistantContent { get; set; } = string.Empty;
        public AssistantChatResultSet? ResultSet { get; set; }
        public AssistantChatPendingAction? Action { get; set; }
    }

    private sealed class OpenAiPlanRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAiPlanMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("response_format")]
        public OpenAiResponseFormat ResponseFormat { get; set; } = null!;
    }

    private sealed record OpenAiPlanMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record OpenAiResponseFormat(
        [property: JsonPropertyName("type")] string Type);

    private sealed class OpenAiPlanResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiPlanChoice>? Choices { get; set; }
    }

    private sealed class OpenAiPlanChoice
    {
        [JsonPropertyName("message")]
        public OpenAiPlanChoiceMessage? Message { get; set; }
    }

    private sealed class OpenAiPlanChoiceMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private sealed class OpenAiPlannedPayload
    {
        [JsonPropertyName("assistantMessage")]
        public string? AssistantMessage { get; set; }

        [JsonPropertyName("toolCalls")]
        public List<OpenAiPlannedToolCall>? ToolCalls { get; set; }
    }

    private sealed class OpenAiPlannedToolCall
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public JsonElement? Arguments { get; set; }
    }
}
