using KUKULCAN.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace KUKULCAN.SharedKernel.i18n.API.Extensions;

/// <summary>Maps SharedKernel results to RFC 7807 HTTP responses.</summary>
public static class ResultExtensions
{
    /// <summary>Maps a successful result to 200 OK or a failed result to problem details.</summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
        => result.IsSuccess ? controller.Ok(result.Value) : result.Error.ToProblemResult(controller);

    /// <summary>Maps a successful result to 201 Created or a failed result to problem details.</summary>
    public static IActionResult ToCreatedResult<T>(this Result<T> result, ControllerBase controller, string actionName, object routeValues)
        => result.IsSuccess ? controller.CreatedAtAction(actionName, routeValues, result.Value) : result.Error.ToProblemResult(controller);

    /// <summary>Maps a void result to 204 No Content or problem details.</summary>
    public static IActionResult ToNoContentResult(this Result result, ControllerBase controller)
        => result.IsSuccess ? controller.NoContent() : result.Error.ToProblemResult(controller);

    private static IActionResult ToProblemResult(this Error error, ControllerBase controller)
    {
        ProblemDetails problem = new()
        {
            Title = error.Code,
            Detail = error.Description,
            Extensions = { ["errorCode"] = error.Code },
        };

        return GetStatusCode(error.Code) switch
        {
            StatusCodes.Status422UnprocessableEntity => controller.UnprocessableEntity(problem),
            StatusCodes.Status404NotFound => controller.NotFound(problem),
            StatusCodes.Status409Conflict => controller.Conflict(problem),
            StatusCodes.Status403Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, problem),
            StatusCodes.Status401Unauthorized => controller.Unauthorized(problem),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, problem),
        };
    }

    private static int GetStatusCode(string code)
    {
        if (code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            return StatusCodes.Status404NotFound;
        if (code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("Conflict", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("Inactive", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("ProtectedDelete", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("CannotDeactivate", StringComparison.OrdinalIgnoreCase))
            return StatusCodes.Status409Conflict;
        if (code.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
            return StatusCodes.Status401Unauthorized;
        if (code.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
            return StatusCodes.Status403Forbidden;
        if (code.StartsWith("Validation", StringComparison.OrdinalIgnoreCase) ||
            code.EndsWith("Empty", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("Invalid", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("TooShort", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("TooSmall", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("ExceedsMaxLength", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("OutOfRange", StringComparison.OrdinalIgnoreCase))
            return StatusCodes.Status422UnprocessableEntity;
        return StatusCodes.Status500InternalServerError;
    }
}
