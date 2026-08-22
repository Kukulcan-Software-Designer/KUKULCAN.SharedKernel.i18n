using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using KUKULCAN.SharedKernel.i18n.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace KUKULCAN.SharedKernel.i18n.API.UnitTests.Middleware;

[TestFixture]
public sealed class ExceptionHandlingMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_WhenNoException_InvokesNextAndPreservesResponse()
    {
        bool nextCalled = false;
        var middleware = new ExceptionHandlingMiddleware(
            context =>
            {
                nextCalled = true;
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status204NoContent));
    }

    [Test]
    public async Task InvokeAsync_WhenValidationException_WritesValidationProblem()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException(new[]
            {
                new ValidationFailure("Code", "Code is required."),
                new ValidationFailure("Code", "Code is invalid."),
                new ValidationFailure("Name", "Name is required."),
            }),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        await using var stream = new MemoryStream();
        context.Response.Body = stream;

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status422UnprocessableEntity));
        Assert.That(context.Response.ContentType, Is.EqualTo("application/problem+json"));
        stream.Position = 0;
        using JsonDocument json = await JsonDocument.ParseAsync(stream);
        Assert.That(json.RootElement.GetProperty("title").GetString(), Is.EqualTo("Validation.Failed"));
        Assert.That(json.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(422));
        Assert.That(json.RootElement.GetProperty("errors").GetProperty("Code").GetArrayLength(), Is.EqualTo(2));
        Assert.That(json.RootElement.GetProperty("errors").GetProperty("Name").GetArrayLength(), Is.EqualTo(1));
    }

    [Test]
    public async Task InvokeAsync_WhenUnexpectedException_WritesGenericInternalProblem()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("secret internal detail"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        await using var stream = new MemoryStream();
        context.Response.Body = stream;

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        Assert.That(context.Response.ContentType, Is.EqualTo("application/problem+json"));
        stream.Position = 0;
        using JsonDocument json = await JsonDocument.ParseAsync(stream);
        Assert.That(json.RootElement.GetProperty("title").GetString(), Is.EqualTo("Unexpected.Error"));
        Assert.That(json.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(500));
        Assert.That(json.RootElement.GetProperty("detail").GetString(), Does.Not.Contain("secret internal detail"));
    }
}
