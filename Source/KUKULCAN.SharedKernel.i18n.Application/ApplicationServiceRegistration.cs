using System.Reflection;
using FluentValidation;
using KUKULCAN.SharedKernel.i18n.Application.Behaviors;
using KUKULCAN.SharedKernel.i18n.Domain.Interfaces.Services;
using KUKULCAN.SharedKernel.i18n.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KUKULCAN.SharedKernel.i18n.Application;

/// <summary>Registers application-layer services for the i18n module.</summary>
public static class ApplicationServiceRegistration
{
    /// <summary>Registers MediatR, validation, caching, logging and domain services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddKukulcanI18NApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(ApplicationServiceRegistration).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ITranslationLookupService, TranslationLookupService>();
        services.AddScoped<ILanguageDomainService, LanguageDomainService>();
        return services;
    }
}
