using KUKULCAN.SharedKernel.i18n.Application;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Queries.GetLanguage;
using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
using KUKULCAN.SharedKernel.i18n.Domain.Entities;
using KUKULCAN.SharedKernel.i18n.Infrastructure;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using KUKULCAN.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KUKULCAN.SharedKernel.i18n.Application.Integration;

[TestFixture]
public sealed class ApplicationIntegrationTests
{
    private ServiceProvider _provider = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        await IntegrationTestDatabase.ResetAsync();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kukulcan:Database:Provider"] = "PostgresSql",
                ["Kukulcan:Database:ConnectionString"] = IntegrationTestDatabase.ConnectionString,
                ["Kukulcan:Database:Retry:Enabled"] = "false",
                ["Kukulcan:Database:Pool:Enabled"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKukulcanI18NApplication();
        services.AddKukulcanI18NInfrastructure(configuration);

        _provider = services.BuildServiceProvider();
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _provider.DisposeAsync();
    }

    [Test]
    public async Task CreateLanguageThroughMediator_PersistsLanguageInPostgreSql()
    {
        using IServiceScope scope = _provider.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Result<LanguageDto> result = await mediator.Send(
            new CreateLanguageCommand("de-DE", "German", "Deutsch"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Code, Is.EqualTo("de-DE"));

        await using I18NDbContext context = await IntegrationTestDatabase.CreateContextAsync();
        Language? language = await context.Languages.SingleOrDefaultAsync(x => x.Code == "de-DE");

        Assert.That(language, Is.Not.Null);
        Assert.That(language!.Name, Is.EqualTo("German"));
        Assert.That(language.NativeName, Is.EqualTo("Deutsch"));
    }

    [Test]
    public async Task GetLanguageThroughMediator_ReadsLanguagePersistedInPostgreSql()
    {
        await using (I18NDbContext context = await IntegrationTestDatabase.CreateContextAsync())
        {
            context.Languages.Add(Language.Create(
                Guid.CreateVersion7(), "it-IT", "Italian", "Italiano").Value);
            await context.SaveChangesAsync();
        }

        using IServiceScope scope = _provider.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Result<LanguageDto> result = await mediator.Send(new GetLanguageQuery("it-IT"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Code, Is.EqualTo("it-IT"));
        Assert.That(result.Value.Name, Is.EqualTo("Italian"));
    }

    [Test]
    public async Task CreateLanguageThroughMediator_DuplicateCodeReturnsConflict()
    {
        await using (I18NDbContext context = await IntegrationTestDatabase.CreateContextAsync())
        {
            context.Languages.Add(Language.Create(
                Guid.CreateVersion7(), "fr-FR", "French", "Français").Value);
            await context.SaveChangesAsync();
        }

        using IServiceScope scope = _provider.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Result<LanguageDto> result = await mediator.Send(
            new CreateLanguageCommand("fr-FR", "French duplicate", "Français"));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Does.Contain("Language.Duplicate"));
    }
}
