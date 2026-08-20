using KUKULCAN.SharedKernel.i18n.API.Controllers;
using KUKULCAN.SharedKernel.i18n.Application.Contracts.Requests;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.CreateTranslation;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.DeleteTranslation;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.SetTranslationReviewed;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslation;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslationVariants;
using KUKULCAN.SharedKernel.i18n.Application.Features.Translations.Queries.GetTranslationsByModule;
using MediatR;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.API.UnitTests.Controllers;

[TestFixture]
public sealed class TranslationsControllerBehaviorTests
{
    [Test]
    public async Task Create_ForwardsCommandUnchanged()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<CreateTranslationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));
        var command = new CreateTranslationCommand("CRM0001", "es-ES", "Cliente");

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).Create(command, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<CreateTranslationCommand>(sent => ReferenceEquals(sent, command)),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task Update_BuildsCommandFromRouteAndBody()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<UpdateTranslationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).Update("CRM0001", "es-ES", new UpdateTranslationRequest("Cliente", "CRM"), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<UpdateTranslationCommand>(command =>
                command.Code == "CRM0001" &&
                command.LanguageCode == "es-ES" &&
                command.NewText == "Cliente" &&
                command.NewContext == "CRM"),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task SetReviewed_BuildsCommandFromRouteAndBody()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<SetTranslationReviewedCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).SetReviewed("CRM0001", "ca-ES", new SetReviewedRequest(true), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<SetTranslationReviewedCommand>(command =>
                command.Code == "CRM0001" &&
                command.LanguageCode == "ca-ES" &&
                command.IsReviewed),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task Delete_BuildsCommandFromRoute()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<DeleteTranslationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).Delete("CRM0001", "es-ES", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<DeleteTranslationCommand>(command =>
                command.Code == "CRM0001" && command.LanguageCode == "es-ES"),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetTranslation_BuildsQueryFromRoute()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<GetTranslationQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).GetTranslation("CRM0001", "es-MX", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<GetTranslationQuery>(query =>
                query.Code == "CRM0001" && query.LanguageCode == "es-MX"),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetModuleTranslations_BuildsQueryFromRoute()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<GetTranslationsByModuleQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).GetModuleTranslations("CRM", "es-ES", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<GetTranslationsByModuleQuery>(query =>
                query.Module == "CRM" && query.LanguageCode == "es-ES"),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetVariants_BuildsQueryFromRoute()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<GetTranslationVariantsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).GetVariants("CRM0001", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<GetTranslationVariantsQuery>(query => query.Code == "CRM0001"),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task BulkUpsert_ForwardsCommandUnchanged()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<BulkUpsertTranslationsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sentinel"));
        var command = new BulkUpsertTranslationsCommand(Array.Empty<BulkTranslationDto>());

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).BulkUpsert(command, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        mediator.Verify(x => x.Send(
            It.Is<BulkUpsertTranslationsCommand>(sent => ReferenceEquals(sent, command)),
            CancellationToken.None), Times.Once);
    }
}
