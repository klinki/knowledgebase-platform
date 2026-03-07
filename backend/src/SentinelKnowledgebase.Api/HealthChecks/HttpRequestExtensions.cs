namespace SentinelKnowledgebase.Api.HealthChecks;

public static class HttpRequestExtensions
{
    public static bool IsLocalRequest(this HttpRequest request)
    {
        var connection = request.HttpContext.Connection;
        if (connection.RemoteIpAddress == null || connection.LocalIpAddress == null)
        {
            return true;
        }

        return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
    }
}
