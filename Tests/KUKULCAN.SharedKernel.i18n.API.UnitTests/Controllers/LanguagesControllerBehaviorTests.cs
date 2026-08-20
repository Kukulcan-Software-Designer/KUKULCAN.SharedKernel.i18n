using KUKULCAN.SharedKernel.i18n.API.Controllers;
using KUKULCAN.SharedKernel.i18n.Application.Contracts.Requests;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.SetLanguageActive;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Queries.GetLanguage;
using KUKULCAN.SharedKernel.Results;
using MediatR;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.API.UnitTests.Controllers;

[TestFixture]
public sealed class LanguagesControllerBehaviorTests
{
    private static void SetupException<TResponse>(Mock<IMediator> mediator)
    {
        mediator.Setup(x => x.Send(
                It.IsAny<IRequest<TResponse>>(),
                It.IsAny<CancellationToken>()))
            .Returns((IRequest<TResponse> _, CancellationToken _) =>
                Task.FromException<TResponse>(new InvalidOperationException("sentinel")));
    }

    [Test]
    public async Task Create_SendsCommandAndCancellationToken()
    {
        var mediator = new Mock<IMediator>();
        SetupException<Result<LanguageDto>>(mediator);
        var command = new CreateLanguageCommand("es-ES", "Spanish", "Español");
        using var cts = new CancellationTokenSource();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LanguagesController(mediator.Object).Create(command, cts.Token));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<CreateLanguageCommand>(sent => ReferenceEquals(sent, command)),
            cts.Token), Times.Once);
    }

    [Test]
    public async Task Update_BuildsCommandFromRouteAndBody()
    {
        var mediator = new Mock<IMediator>();
        SetupException<Result<LanguageDto>>(mediator);
        var body = new UpdateLanguageRequest("Spanish", "Español");

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LanguagesController(mediator.Object).Update("es-ES", body, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<UpdateLanguageCommand>(command =>
                command.Code == "es-ES" &&
                command.Name == "Spanish" &&
                command.NativeName == "Español"),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task SetActive_BuildsCommandFromRouteAndBody()
    {
        var mediator = new Mock<IMediator>();
        SetupException<Result>(mediator);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LanguagesController(mediator.Object).SetActive(
                "ca-ES", new SetActiveRequest(false), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<SetLanguageActiveCommand>(command =>
                command.Code == "ca-ES" && command.IsActive == false),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task SetDefault_SendsCommandWithRouteCode()
    {
        var mediator = new Mock<IMediator>();
        SetupException<Result>(mediator);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LanguagesController(mediator.Object).SetDefault("en", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<SetDefaultLanguageCommand>(command => command.Code == "en"),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetAll_SendsQueryWithActiveOnly()
    {
        var mediator = new Mock<IMediator>();
        SetupException<Result<IReadOnlyList<LanguageDto>>>(mediator);
        using var cts = new CancellationTokenSource();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LanguagesController(mediator.Object).GetAll(false, cts.Token));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<GetAllLanguagesQuery>(query => query.ActiveOnly == false),
            cts.Token), Times.Once);
    }

    [Test]
    public async Task GetByCode_SendsQueryWithRouteCode()
    {
        var mediator = new Mock<IMediator>();
        SetupException<Result<LanguageDto>>(mediator);
        using var cts = new CancellationTokenSource();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LanguagesController(mediator.Object).GetByCode("es-MX", cts.Token));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<GetLanguageQuery>(query => query.Code == "es-MX"),
            cts.Token), Times.Once);
    }
}
