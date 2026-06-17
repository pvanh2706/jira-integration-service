using System.Diagnostics;

namespace JiraIntegrationService.Api.Common;

public static class TraceId
{
    public static string From(HttpContext? httpContext)
    {
        return httpContext?.TraceIdentifier
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString("N");
    }
}
