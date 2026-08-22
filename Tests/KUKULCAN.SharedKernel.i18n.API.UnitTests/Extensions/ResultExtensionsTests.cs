using KUKULCAN.SharedKernel.i18n.API.Extensions;
using KUKULCAN.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KUKULCAN.SharedKernel.i18n.API.UnitTests.Extensions;

[TestFixture]
public sealed class ResultExtensionsTests
{
    private sealed class TestController : ControllerBase;

    [Test]
    public void ToActionResult_Success_ReturnsOkWithValue()
    {
        Result<string> result = Result<string>.Success("value");

        IActionResult action = result.ToActionResult(new TestController());

        Assert.That(action, Is.TypeOf<OkObjectResult>());
        Assert.That(((OkObjectResult)action).Value, Is.EqualTo("value"));
    }

    [TestCase("Language.NotFound", StatusCodes.Status404NotFound)]
    [TestCase("Language.Duplicate", StatusCodes.Status409Conflict)]
    [TestCase("Language.Unauthorized", StatusCodes.Status401Unauthorized)]
    [TestCase("Language.Forbidden", StatusCodes.Status403Forbidden)]
    [TestCase("Validation.InvalidCode", StatusCodes.Status422UnprocessableEntity)]
    [TestCase("Language.Unexpected", StatusCodes.Status500InternalServerError)]
    public void ToActionResult_Failure_MapsErrorToExpectedStatus(string code, int expectedStatus)
    {
        Result<string> result = Result<string>.Failure(new Error(code, "description"));

        IActionResult action = result.ToActionResult(new TestController());

        int actualStatus = action switch
        {
            ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
            UnauthorizedResult => StatusCodes.Status401Unauthorized,
            _ => throw new AssertionException($"Unexpected action result type: {action.GetType().Name}"),
        };

        Assert.That(actualStatus, Is.EqualTo(expectedStatus));
        ObjectResult problemResult = action as ObjectResult ?? throw new AssertionException("Expected ObjectResult.");
        ProblemDetails problem = problemResult.Value as ProblemDetails ?? throw new AssertionException("Expected ProblemDetails.");
        Assert.That(problem.Title, Is.EqualTo(code));
        Assert.That(problem.Detail, Is.EqualTo("description"));
        Assert.That(problem.Extensions["errorCode"], Is.EqualTo(code));
    }

    [Test]
    public void ToCreatedResult_Success_ReturnsCreatedAtAction()
    {
        Result<string> result = Result<string>.Success("value");

        IActionResult action = result.ToCreatedResult(new TestController(), "Get", new { id = 7 });

        Assert.That(action, Is.TypeOf<CreatedAtActionResult>());
        CreatedAtActionResult created = (CreatedAtActionResult)action;
        Assert.That(created.ActionName, Is.EqualTo("Get"));
        Assert.That(created.Value, Is.EqualTo("value"));
    }

    [Test]
    public void ToNoContentResult_Success_ReturnsNoContent()
    {
        Result result = Result.Success();

        IActionResult action = result.ToNoContentResult(new TestController());

        Assert.That(action, Is.TypeOf<NoContentResult>());
    }
}
