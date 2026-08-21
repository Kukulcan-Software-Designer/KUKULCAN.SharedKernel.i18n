using FluentValidation;
using KUKULCAN.SharedKernel.i18n.Application.Abstractions;
using KUKULCAN.SharedKernel.i18n.Application.Behaviors;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Services;
using KUKULCAN.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests.Common;

[TestFixture]
public sealed class ApplicationBehaviorTests
{
    private sealed record PlainRequest(string Value) : IRequest<Result>;
    private sealed record GenericRequest(string Value) : IRequest<Result<string>>;
    private sealed record CacheRequest(string Value) : IRequest<Result<string>>, ICacheableRequest
    {
        public string CacheKey => "test:key";
        public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    }

    [Test]
    public async Task CachingBehavior_NonCacheable_ForwardsToNext()
    {
        var cache = new Mock<ICacheService>(); var logger = new Mock<ILogger<CachingBehavior<PlainRequest, Result>>>(); var nextCalled = false;
        var sut = new CachingBehavior<PlainRequest, Result>(cache.Object, logger.Object);
        var result = await sut.Handle(new PlainRequest("x"), _ => { nextCalled = true; return Task.FromResult(Result.Success()); }, CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(nextCalled, Is.True); cache.VerifyNoOtherCalls();
    }

    [Test]
    public async Task CachingBehavior_CacheHit_DoesNotCallNext()
    {
        var cache = new Mock<ICacheService>(); var logger = new Mock<ILogger<CachingBehavior<CacheRequest, Result<string>>>>(); var cached = Result<string>.Success("cached");
        cache.Setup(x => x.GetAsync<Result<string>>("test:key", It.IsAny<CancellationToken>())).ReturnsAsync(cached);
        var sut = new CachingBehavior<CacheRequest, Result<string>>(cache.Object, logger.Object); var nextCalled = false;
        var result = await sut.Handle(new CacheRequest("x"), _ => { nextCalled = true; return Task.FromResult(Result<string>.Success("next")); }, CancellationToken.None);
        Assert.That(result.Value, Is.EqualTo("cached")); Assert.That(nextCalled, Is.False); cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Result<string>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CachingBehavior_CacheMiss_CallsNextAndStoresResponse()
    {
        var cache = new Mock<ICacheService>(); var logger = new Mock<ILogger<CachingBehavior<CacheRequest, Result<string>>>>();
        cache.Setup(x => x.GetAsync<Result<string>>("test:key", It.IsAny<CancellationToken>())).ReturnsAsync((Result<string>?)null);
        var sut = new CachingBehavior<CacheRequest, Result<string>>(cache.Object, logger.Object);
        var result = await sut.Handle(new CacheRequest("x"), _ => Task.FromResult(Result<string>.Success("next")), CancellationToken.None);
        Assert.That(result.Value, Is.EqualTo("next")); cache.Verify(x => x.SetAsync("test:key", It.IsAny<Result<string>>(), TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ValidationBehavior_NoValidators_ForwardsRequest()
    {
        var sut = new ValidationBehavior<PlainRequest, Result>(Array.Empty<IValidator<PlainRequest>>()); var called = false;
        var result = await sut.Handle(new PlainRequest("x"), _ => { called = true; return Task.FromResult(Result.Success()); }, CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(called, Is.True);
    }

    [Test]
    public async Task ValidationBehavior_ValidRequest_ForwardsRequest()
    {
        var validator = new InlineValidator<PlainRequest>(); validator.RuleFor(x => x.Value).NotEmpty();
        var sut = new ValidationBehavior<PlainRequest, Result>(new[] { validator }); var called = false;
        var result = await sut.Handle(new PlainRequest("ok"), _ => { called = true; return Task.FromResult(Result.Success()); }, CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True); Assert.That(called, Is.True);
    }

    [Test]
    public async Task ValidationBehavior_InvalidResult_ReturnsValidationFailure()
    {
        var validator = new InlineValidator<PlainRequest>(); validator.RuleFor(x => x.Value).NotEmpty();
        var sut = new ValidationBehavior<PlainRequest, Result>(new[] { validator }); var called = false;
        var result = await sut.Handle(new PlainRequest(""), _ => { called = true; return Task.FromResult(Result.Success()); }, CancellationToken.None);
        Assert.That(result.IsFailure, Is.True); Assert.That(called, Is.False); Assert.That(result.Error.Code, Does.Contain("Validation"));
    }

    [Test]
    public async Task ValidationBehavior_GenericResult_InvalidRequest_ReturnsGenericFailure()
    {
        var validator = new InlineValidator<GenericRequest>(); validator.RuleFor(x => x.Value).NotEmpty();
        var sut = new ValidationBehavior<GenericRequest, Result<string>>(new[] { validator });
        var result = await sut.Handle(new GenericRequest(""), _ => Task.FromResult(Result<string>.Success("ok")), CancellationToken.None);
        Assert.That(result.IsFailure, Is.True); Assert.That(result.Error.Code, Is.EqualTo("Validation.Failed"));
    }

    [Test]
    public async Task ValidationBehavior_NonResultResponse_ThrowsValidationException()
    {
        var validator = new InlineValidator<NonResultRequest>(); validator.RuleFor(x => x.Value).NotEmpty();
        var sut = new ValidationBehavior<NonResultRequest, string>(new[] { validator });
        Assert.That(async () => await sut.Handle(new NonResultRequest(""), _ => Task.FromResult("ok"), CancellationToken.None), Throws.TypeOf<ValidationException>());
    }

    private sealed record NonResultRequest(string Value) : IRequest<string>;

    [Test]
    public async Task LoggingBehavior_Success_ReturnsResponse()
    {
        var logger = new Mock<ILogger<LoggingBehavior<PlainRequest, Result>>>(); var sut = new LoggingBehavior<PlainRequest, Result>(logger.Object);
        var result = await sut.Handle(new PlainRequest("x"), _ => Task.FromResult(Result.Success()), CancellationToken.None);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void LoggingBehavior_Exception_IsPropagated()
    {
        var logger = new Mock<ILogger<LoggingBehavior<PlainRequest, Result>>>(); var sut = new LoggingBehavior<PlainRequest, Result>(logger.Object);
        Assert.That(async () => await sut.Handle(new PlainRequest("x"), _ => throw new InvalidOperationException("boom"), CancellationToken.None), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task LoggingBehavior_SlowRequest_ReturnsResponse()
    {
        var original = LoggingBehavior<PlainRequest, Result>.SlowRequestThresholdMs; LoggingBehavior<PlainRequest, Result>.SlowRequestThresholdMs = -1;
        try
        {
            var logger = new Mock<ILogger<LoggingBehavior<PlainRequest, Result>>>(); var sut = new LoggingBehavior<PlainRequest, Result>(logger.Object);
            var result = await sut.Handle(new PlainRequest("x"), _ => Task.FromResult(Result.Success()), CancellationToken.None);
            Assert.That(result.IsSuccess, Is.True);
        }
        finally { LoggingBehavior<PlainRequest, Result>.SlowRequestThresholdMs = original; }
    }
}
