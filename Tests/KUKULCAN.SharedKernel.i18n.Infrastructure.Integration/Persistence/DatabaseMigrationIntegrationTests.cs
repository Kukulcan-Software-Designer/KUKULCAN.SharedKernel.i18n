using KUKULCAN.SharedKernel.i18n.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.Integration.Persistence;

[TestFixture]
public sealed class DatabaseMigrationIntegrationTests
{
    [Test]
    public async Task MigrateAsync_AppliesAllPendingMigrations()
    {
        await using I18NDbContext context = await IntegrationTestDatabase.CreateContextAsync();

        IReadOnlyList<string> pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
        IReadOnlyList<string> appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();

        Assert.That(pendingMigrations, Is.Empty);
        Assert.That(appliedMigrations, Does.Contain("20260823095510_InitialCreation"));
    }
}
