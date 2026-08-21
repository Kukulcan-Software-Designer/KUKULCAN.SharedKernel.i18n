using KUKULCAN.SharedKernel.i18n.Domain.Services;
using Microsoft.Extensions.Caching.Memory;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Services;

[TestFixture]
public sealed class MemoryOnlyCacheServiceTests
{
    private MemoryCache _cache = null!;
    private MemoryOnlyCacheService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new MemoryOnlyCacheService(_cache);
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
    }

    [Test]
    public async Task GetAsync_WhenKeyMissing_ReturnsNull()
    {
        string? result = await _service.GetAsync<string>("missing");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        await _service.SetAsync("key", "value");

        string? result = await _service.GetAsync<string>("key");

        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public async Task SetAsync_WithExpiry_StoresValue()
    {
        await _service.SetAsync("key", "value", TimeSpan.FromMinutes(1));

        Assert.That(await _service.GetAsync<string>("key"), Is.EqualTo("value"));
    }

    [Test]
    public async Task RemoveAsync_RemovesValue()
    {
        await _service.SetAsync("key", "value");

        await _service.RemoveAsync("key");

        Assert.That(await _service.GetAsync<string>("key"), Is.Null);
    }

    [Test]
    public async Task ExistsAsync_ReturnsTrueForExistingKey()
    {
        await _service.SetAsync("key", "value");

        Assert.That(await _service.ExistsAsync("key"), Is.True);
    }

    [Test]
    public async Task ExistsAsync_ReturnsFalseForMissingKey()
    {
        Assert.That(await _service.ExistsAsync("missing"), Is.False);
    }

    [Test]
    public async Task GetOrCreateAsync_WhenValueMissing_CreatesAndCachesValue()
    {
        int calls = 0;
        string first = await _service.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return Task.FromResult("created");
        });
        string second = await _service.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return Task.FromResult("created-again");
        });

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("created"));
            Assert.That(second, Is.EqualTo("created"));
            Assert.That(calls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetOrCreateAsync_PassesCancellationTokenToFactory()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken received = default;

        await _service.GetOrCreateAsync("key", token =>
        {
            received = token;
            return Task.FromResult(123);
        }, ct: cts.Token);

        Assert.That(received, Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetOrCreateAsync_WhenFactoryReturnsNull_DoesNotCreatePersistentEntry()
    {
        string? result = await _service.GetOrCreateAsync<string?>("key", _ => Task.FromResult<string?>(null));

        Assert.That(result, Is.Null);
        Assert.That(await _service.ExistsAsync("key"), Is.False);
    }

    [Test]
    public async Task RemoveByPrefixAsync_CurrentImplementationDoesNotRemoveEntries()
    {
        await _service.SetAsync("prefix:one", "one");
        await _service.SetAsync("prefix:two", "two");
        await _service.SetAsync("other", "other");

        await _service.RemoveByPrefixAsync("prefix:");

        Assert.Multiple(async () =>
        {
            Assert.That(await _service.ExistsAsync("prefix:one"), Is.True);
            Assert.That(await _service.ExistsAsync("prefix:two"), Is.True);
            Assert.That(await _service.ExistsAsync("other"), Is.True);
        });
    }
}
