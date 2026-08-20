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
using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
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
        CreateTranslationCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<CreateTranslationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CreateTranslationCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));
        var command = new CreateTranslationCommand("CRM0001", "es-ES", "Cliente");

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).Create(command, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured, Is.SameAs(command));
    }

    [Test]
    public async Task Update_BuildsCommandFromRouteAndBody()
    {
        var mediator = new Mock<IMediator>();
        UpdateTranslationCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<UpdateTranslationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateTranslationCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).Update("CRM0001", "es-ES", new UpdateTranslationRequest("Cliente", "CRM"), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Code, Is.EqualTo("CRM0001"));
        Assert.That(captured.LanguageCode, Is.EqualTo("es-ES"));
        Assert.That(captured.NewText, Is.EqualTo("Cliente"));
        Assert.That(captured.NewContext, Is.EqualTo("CRM"));
    }

    [Test]
    public async Task SetReviewed_BuildsCommandFromRouteAndBody()
    {
        var mediator = new Mock<IMediator>();
        SetTranslationReviewedCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<SetTranslationReviewedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SetTranslationReviewedCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).SetReviewed("CRM0001", "ca-ES", new SetReviewedRequest(true), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Code, Is.EqualTo("CRM0001"));
        Assert.That(captured.LanguageCode, Is.EqualTo("ca-ES"));
        Assert.That(captured.IsReviewed, Is.True);
    }

    [Test]
    public async Task Delete_BuildsCommandFromRoute()
    {
        var mediator = new Mock<IMediator>();
        DeleteTranslationCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<DeleteTranslationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteTranslationCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).Delete("CRM0001", "es-ES", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Code, Is.EqualTo("CRM0001"));
        Assert.That(captured.LanguageCode, Is.EqualTo("es-ES"));
    }

    [Test]
    public async Task GetTranslation_BuildsQueryFromRoute()
    {
        var mediator = new Mock<IMediator>();
        GetTranslationQuery? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<GetTranslationQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetTranslationQuery, CancellationToken>((query, _) => captured = query)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).GetTranslation("CRM0001", "es-MX", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Code, Is.EqualTo("CRM0001"));
        Assert.That(captured.LanguageCode, Is.EqualTo("es-MX"));
    }

    [Test]
    public async Task GetModuleTranslations_BuildsQueryFromRoute()
    {
        var mediator = new Mock<IMediator>();
        GetTranslationsByModuleQuery? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<GetTranslationsByModuleQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetTranslationsByModuleQuery, CancellationToken>((query, _) => captured = query)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).GetModuleTranslations("CRM", "es-ES", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Module, Is.EqualTo("CRM"));
        Assert.That(captured.LanguageCode, Is.EqualTo("es-ES"));
    }

    [Test]
    public async Task GetVariants_BuildsQueryFromRoute()
    {
        var mediator = new Mock<IMediator>();
        GetTranslationVariantsQuery? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<GetTranslationVariantsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetTranslationVariantsQuery, CancellationToken>((query, _) => captured = query)
            .ThrowsAsync(new InvalidOperationException("sentinel"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).GetVariants("CRM0001", CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured!.Code, Is.EqualTo("CRM0001"));
    }

    [Test]
    public async Task BulkUpsert_ForwardsCommandUnchanged()
    {
        var mediator = new Mock<IMediator>();
        BulkUpsertTranslationsCommand? captured = null;
        mediator.Setup(x => x.Send(It.IsAny<BulkUpsertTranslationsCommand>(), It.IsAny<CancellationToken>()))
            .Callback<BulkUpsertTranslationsCommand, CancellationToken>((command, _) => captured = command)
            .ThrowsAsync(new InvalidOperationException("sentinel"));
        var command = new BulkUpsertTranslationsCommand(Array.Empty<BulkTranslationDto>());

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TranslationsController(mediator.Object).BulkUpsert(command, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("sentinel"));
        Assert.That(captured, Is.SameAs(command));
    }
}
