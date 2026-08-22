using KUKULCAN.SharedKernel.i18n.API.Controllers;
using KUKULCAN.SharedKernel.i18n.Application.Contracts.Requests;
using KUKULCAN.SharedKernel.i18n.Application.Features.Currencies.Commands.DeleteCurrencyFormat;
using KUKULCAN.SharedKernel.i18n.Application.Features.Currencies.Commands.UpsertCurrencyFormat;
using KUKULCAN.SharedKernel.i18n.Application.Features.Currencies.Queries.GetCurrencyFormats;
using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
using KUKULCAN.SharedKernel.Results;
using MediatR;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.API.UnitTests.Controllers;

[TestFixture]
public sealed class CurrenciesControllerBehaviorTests
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
    public async Task Upsert_BuildsCommandFromRouteAndBodyAndPropagatesCancellationToken()
    {
        var mediator = new Mock<IMediator>();
        SetupException<Result<CurrencyFormatDto>>(mediator);
        var body = new UpsertCurrencyRequest(
            "Euro", "€", "After", true, ".", ",", 2, "-n");
        using var cts = new CancellationTokenSource();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CurrenciesController(mediator.Object).Upsert("es-ES", "EUR", body, cts.Token));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<UpsertCurrencyFormatCommand>(command =>
                command.LanguageCode == "es-ES" &&
                command.CurrencyCode == "EUR" &&
                command.CurrencyName == "Euro" &&
                command.Symbol == "€" &&
                command.SymbolPosition == "After" &&
                command.SpaceBetweenSymbolAndAmount &&
                command.DecimalSeparator == "." &&
                command.ThousandsSeparator == "," &&
                command.DecimalPlaces == 2 &&
                command.NegativePattern == "-n"),
            cts.Token), Times.Once);
    }

    [Test]
    public async Task Delete_BuildsCommandFromRouteAndPropagatesCancellationToken()
    {
        var mediator = new Mock<IMediator>();
        SetupException<Result>(mediator);
        using var cts = new CancellationTokenSource();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CurrenciesController(mediator.Object).Delete("es-ES", "EUR", cts.Token));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<DeleteCurrencyFormatCommand>(command =>
                command.LanguageCode == "es-ES" &&
                command.CurrencyCode == "EUR"),
            cts.Token), Times.Once);
    }

    [Test]
    public async Task GetByLanguage_BuildsQueryFromRouteAndPropagatesCancellationToken()
    {
        var mediator = new Mock<IMediator>();
        SetupException<Result<IReadOnlyList<CurrencyFormatDto>>>(mediator);
        using var cts = new CancellationTokenSource();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CurrenciesController(mediator.Object).GetByLanguage("es-ES", cts.Token));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<GetCurrencyFormatsQuery>(query => query.LanguageCode == "es-ES"),
            cts.Token), Times.Once);
    }
}
