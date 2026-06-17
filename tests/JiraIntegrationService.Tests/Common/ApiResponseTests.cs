using JiraIntegrationService.Api.Common;

namespace JiraIntegrationService.Tests.Common;

public sealed class ApiResponseTests
{
    [Fact]
    public void Ok_ShouldCreateSuccessResponse()
    {
        var data = new { Status = "OK" };

        var response = ApiResponse<object>.Ok(data, "trace-1");

        Assert.True(response.Success);
        Assert.Same(data, response.Data);
        Assert.Equal("trace-1", response.TraceId);
    }

    [Fact]
    public void ApiErrorResponse_ShouldCreateErrorResponse()
    {
        var response = new ApiErrorResponse(
            ErrorCodes.ValidationError,
            "Invalid request.",
            "trace-2");

        Assert.False(response.Success);
        Assert.Equal(ErrorCodes.ValidationError, response.ErrorCode);
        Assert.Equal("Invalid request.", response.Message);
        Assert.Equal("trace-2", response.TraceId);
    }
}
