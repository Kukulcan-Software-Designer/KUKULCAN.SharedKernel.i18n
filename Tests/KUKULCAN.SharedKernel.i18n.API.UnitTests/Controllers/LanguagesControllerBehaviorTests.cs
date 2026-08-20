using KUKULCAN.SharedKernel.i18n.API.Controllers;
using KUKULCAN.SharedKernel.i18n.Application.Contracts.Requests;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.SetLanguageActive;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Queries.GetLanguage;
using MediatR;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.API.UnitTests.Controllers;

[TestFixture]
public sealed class LanguagesControllerBehaviorTests
{
    [Test]
    public async Task Create_SendsCommandAndCancellationToken()
    {
        var mediator = new Mock<IMediator>();
        CreateLanguageCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<CreateLanguageCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CreateLanguageCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var command = new CreateLanguageCommand("es-ES", "Spanish", "Español");
        CancellationToken token = new CancellationTokenSource().Token;

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => new LanguagesController(mediator.Object).Create(command, token));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured, Is.SameAs(command));
    }

    [Test]
    public async Task Update_BuildsCommandFromRouteAndBody()
    {
        var mediator = new Mock<IMediator>();
        UpdateLanguageCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<UpdateLanguageCommand>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateLanguageCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var body = new UpdateLanguageRequest("Spanish", "Español");
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => new LanguagesController(mediator.Object).Update("es-ES", body, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Code, Is.EqualTo("es-ES"));
        Assert.That(captured.Name, Is.EqualTo("Spanish"));
        Assert.That(captured.NativeName, Is.EqualTo("Español"));
    }

    [Test]
    public async Task SetActive_BuildsCommandFromRouteAndBody()
    {
        var mediator = new Mock<IMediator>();
        SetLanguageActiveCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<SetLanguageActiveCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SetLanguageActiveCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LanguagesController(mediator.Object).SetActive("ca-ES", new SetActiveRequest(false), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Code, Is.EqualTo("ca-ES"));
        Assert.That(captured.IsActive, Is.False);
    }

    [Test]
    public async Task SetDefault_SendsCommandWithRouteCode()
    {
        var mediator = new Mock<IMediator>();
        SetDefaultLanguageCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<SetDefaultLanguageCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SetDefaultLanguageCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => new LanguagesController(mediator.Object).SetDefault("en", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Code, Is.EqualTo("en"));
    }

    [Test]
    public async Task GetAll_SendsQueryWithActiveOnly()
    {
        var mediator = new Mock<IMediator>();
        GetAllLanguagesQuery? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<GetAllLanguagesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetAllLanguagesQuery, CancellationToken>((query, _) => captured = query)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => new LanguagesController(mediator.Object).GetAll(false, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.ActiveOnly, Is.False);
    }

    [Test]
    public async Task GetByCode_SendsQueryWithRouteCode()
    {
        var mediator = new Mock<IMediator>();
        GetLanguageQuery? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<GetLanguageQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetLanguageQuery, CancellationToken>((query, _) => captured = query)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => new LanguagesController(mediator.Object).GetByCode("es-MX", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Code, Is.EqualTo("es-MX"));
    }
}
