using KUKULCAN.SharedKernel.i18n.API.Controllers;
using KUKULCAN.SharedKernel.i18n.Application.Contracts.Requests;
using KUKULCAN.SharedKernel.i18n.Application.Features.Locales.Commands.UpsertLocaleConfiguration;
using KUKULCAN.SharedKernel.i18n.Application.Features.Locales.Queries.GetAllLocaleConfigurations;
using KUKULCAN.SharedKernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;
using MediatR;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.API.UnitTests.Controllers;

[TestFixture]
public sealed class LocalesControllerBehaviorTests
{
    private static UpsertLocaleRequest CreateRequest() => new(
        "dd/MM/yyyy", "dd/MM/yyyy", "HH:mm", "dd/MM/yyyy HH:mm", "Monday", ",", ".", 2, 2);

    [Test]
    public async Task Upsert_BuildsCommandFromRouteAndBody()
    {
        var mediator = new Mock<IMediator>();
        UpsertLocaleConfigurationCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<UpsertLocaleConfigurationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<UpsertLocaleConfigurationCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LocalesController(mediator.Object).Upsert("es-ES", CreateRequest(), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.LanguageCode, Is.EqualTo("es-ES"));
        Assert.That(captured.DateFormat, Is.EqualTo("dd/MM/yyyy"));
        Assert.That(captured.ShortDateFormat, Is.EqualTo("dd/MM/yyyy"));
        Assert.That(captured.TimeFormat, Is.EqualTo("HH:mm"));
        Assert.That(captured.DateTimeFormat, Is.EqualTo("dd/MM/yyyy HH:mm"));
        Assert.That(captured.DecimalPlaces, Is.EqualTo(2));
        Assert.That(captured.CurrencyDecimalPlaces, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAll_SendsQueryAndCancellationToken()
    {
        var mediator = new Mock<IMediator>();
        CancellationToken calledToken = CancellationToken.None;
        mediator.Setup(x => x.Send(It.IsAny<GetAllLocaleConfigurationsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetAllLocaleConfigurationsQuery, CancellationToken>((_, token) => calledToken = token)
            .ThrowsAsync(new InvalidOperationException("sentinel"));
        using var cts = new CancellationTokenSource();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LocalesController(mediator.Object).GetAll(cts.Token));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(calledToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetByLanguage_BuildsQueryFromRoute()
    {
        var mediator = new Mock<IMediator>();
        GetLocaleConfigurationQuery? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<GetLocaleConfigurationQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetLocaleConfigurationQuery, CancellationToken>((query, _) => captured = query)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LocalesController(mediator.Object).GetByLanguage("ca-ES", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.LanguageCode, Is.EqualTo("ca-ES"));
    }
}
