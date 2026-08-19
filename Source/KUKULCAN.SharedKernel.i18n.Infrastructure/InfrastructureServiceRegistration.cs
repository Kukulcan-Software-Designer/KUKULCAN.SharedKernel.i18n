using KUKULCAN.SharedKernel.Database.Extensions;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using KUKULCAN.SharedKernel.i18n.Domain.Services;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Repositories;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence.Seeds;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure;

/// <summary>Registers infrastructure services for the SharedKernel i18n module.</summary>
public static class InfrastructureServiceRegistration
{
    /// <summary>Registers persistence, repositories, system services and caching.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddKukulcanI18NInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKukulcanDbContext<I18NDbContext>(configuration);
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<ITranslationRepository, TranslationRepository>();
        services.AddScoped<ILocaleConfigurationRepository, LocaleConfigurationRepository>();
        services.AddScoped<ICurrencyFormatRepository, CurrencyFormatRepository>();
        services.AddScoped<KUKULCAN.SharedKernel.i18n.Application.Abstractions.IUnitOfWork>(sp =>
            new I18nUnitOfWork(sp.GetRequiredService<KUKULCAN.SharedKernel.Database.Abstractions.IUnitOfWork>()));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<ITenantContext, I18NSystemTenantContext>();
        services.AddSingleton<SystemDateTimeProvider>();
        services.AddSingleton<IClock>(sp => sp.GetRequiredService<SystemDateTimeProvider>());
        services.AddSingleton<IDateTimeProvider>(sp => sp.GetRequiredService<SystemDateTimeProvider>());
        services.AddSingleton<IDomainEventDispatcher, I18NDomainEventDispatcher>();

        RegisterCacheService(services, configuration);
        return services;
    }

    /// <summary>Applies pending EF Core migrations and seeds baseline i18n data.</summary>
    public static async Task MigrateAndSeedAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        I18NDbContext context = scope.ServiceProvider.GetRequiredService<I18NDbContext>();
        await context.Database.MigrateAsync(ct);
        await I18NSeedData.SeedAsync(context, ct);
    }

    private static void RegisterCacheService(IServiceCollection services, IConfiguration configuration)
    {
        string? redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "KUKULCAN.SharedKernel.i18n:";
            });
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, DistributedCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryOnlyCacheService>();
        }
    }
}
