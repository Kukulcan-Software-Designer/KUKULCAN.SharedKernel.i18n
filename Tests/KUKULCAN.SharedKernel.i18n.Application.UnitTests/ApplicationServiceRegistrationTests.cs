using KUKULCAN.SharedKernel.i18n.Application.Behaviors;
using KUKULCAN.SharedKernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using KUKULCAN.SharedKernel.i18n.Domain.DTOs;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Services;
using KUKULCAN.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KUKULCAN.SharedKernel.i18n.Application.UnitTests;

[TestFixture]
public sealed class ApplicationServiceRegistrationTests
{
    [Test]
    public void AddKukulcanI18NApplication_RegistersApplicationServicesAndPipelineBehaviors()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<ITranslationRepository>().Object);
        IServiceCollection returned = services.AddKukulcanI18NApplication();

        Assert.That(returned, Is.SameAs(services));

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.That(provider.GetService<ITranslationLookupService>(), Is.Not.Null);
        Assert.That(provider.GetService<ILanguageDomainService>(), Is.Not.Null);
        Assert.That(provider.GetServices<IPipelineBehavior<CreateLanguageCommand, Result<LanguageDto>>>(), Has.Count.EqualTo(3));
    }
}
