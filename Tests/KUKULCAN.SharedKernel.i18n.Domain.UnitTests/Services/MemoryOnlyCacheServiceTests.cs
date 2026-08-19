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
    public void TearDown() => _cache.Dispose();

    [Test]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        var value = await _service.GetAsync<string>("missing");

        Assert.That(value, Is.Null);
    }

    [Test]
    public async Task SetAsync_StoresValueThatCanBeRetrieved()
    {
        await _service.SetAsync("key", "value");

        var value = await _service.GetAsync<string>("key");

        Assert.That(value, Is.EqualTo("value"));
    }

    [Test]
    public async Task RemoveAsync_RemovesExistingValue()
    {
        await _service.SetAsync("key", "value");
        await _service.RemoveAsync("key");

        Assert.That(await _service.GetAsync<string>("key"), Is.Null);
        Assert.That(await _service.ExistsAsync("key"), Is.False);
    }

    [Test]
    public async Task ExistsAsync_ReflectsCachePresence()
    {
        Assert.That(await _service.ExistsAsync("key"), Is.False);

        await _service.SetAsync("key", 42);

        Assert.That(await _service.ExistsAsync("key"), Is.True);
    }

    [Test]
    public async Task GetOrCreateAsync_OnMiss_InvokesFactoryAndCachesResult()
    {
        var calls = 0;

        var first = await _service.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return Task.FromResult("created");
        });
        var second = await _service.GetOrCreateAsync("key", _ =>
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
        }, cancellationToken: cts.Token);

        Assert.That(received, Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetOrCreateAsync_WhenFactoryReturnsNull_DoesNotCreatePersistentEntry()
    {
        var result = await _service.GetOrCreateAsync<string?>("key", _ => Task.FromResult<string?>(null));

        Assert.That(result, Is.Null);
        Assert.That(await _service.ExistsAsync("key"), Is.False);
    }

    [Test]
    public async Task RemoveByPrefixAsync_RemovesEntriesMatchingPrefix()
    {
        await _service.SetAsync("language:es", "Spanish");
        await _service.SetAsync("language:en", "English");
        await _service.SetAsync("currency:eur", "Euro");

        await _service.RemoveByPrefixAsync("language:");

        var esExists = await _service.ExistsAsync("language:es");
        var enExists = await _service.ExistsAsync("language:en");
        var eurExists = await _service.ExistsAsync("currency:eur");

        Assert.Multiple(() =>
        {
            Assert.That(esExists, Is.False);
            Assert.That(enExists, Is.False);
            Assert.That(eurExists, Is.True);
        });
    }
}
