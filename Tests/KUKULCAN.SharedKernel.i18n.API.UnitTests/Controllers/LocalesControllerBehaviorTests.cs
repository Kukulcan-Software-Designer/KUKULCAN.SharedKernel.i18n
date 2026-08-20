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
        mediator.Setup(x => x.Send(It.IsAny<UpsertLocaleConfigurationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var request = CreateRequest();
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LocalesController(mediator.Object).Upsert("es-ES", request, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<UpsertLocaleConfigurationCommand>(command =>
                command.LanguageCode == "es-ES" &&
                command.DateFormat == "dd/MM/yyyy" &&
                command.ShortDateFormat == "dd/MM/yyyy" &&
                command.TimeFormat == "HH:mm" &&
                command.DateTimeFormat == "dd/MM/yyyy HH:mm" &&
                command.DecimalPlaces == 2 &&
                command.CurrencyDecimalPlaces == 2),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetAll_SendsQueryAndCancellationToken()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<GetAllLocaleConfigurationsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));
        using var cts = new CancellationTokenSource();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LocalesController(mediator.Object).GetAll(cts.Token));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.IsAny<GetAllLocaleConfigurationsQuery>(),
            cts.Token), Times.Once);
    }

    [Test]
    public async Task GetByLanguage_BuildsQueryFromRoute()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<GetLocaleConfigurationQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LocalesController(mediator.Object).GetByLanguage("ca-ES", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<GetLocaleConfigurationQuery>(query => query.LanguageCode == "ca-ES"),
            CancellationToken.None), Times.Once);
    }
}
