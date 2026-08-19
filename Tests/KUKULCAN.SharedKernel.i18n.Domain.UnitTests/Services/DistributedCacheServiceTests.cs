using KUKULCAN.SharedKernel.i18n.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.i18n.Domain.UnitTests.Services;

[TestFixture]
public sealed class DistributedCacheServiceTests
{
    [Test]
    public async Task SetAsync_WritesToDistributedCacheAndL1()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);

        await service.SetAsync("key", new CacheValue("value"));

        Assert.That(distributed.Get("key"), Is.Not.Null);
        Assert.That(await service.GetAsync<CacheValue>("key"), Is.EqualTo(new CacheValue("value")));
    }

    [Test]
    public async Task GetAsync_WhenL1ContainsValue_DoesNotReadL2()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        memory.Set("key", new CacheValue("l1"));
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);

        var value = await service.GetAsync<CacheValue>("key");

        Assert.That(value, Is.EqualTo(new CacheValue("l1")));
        Assert.That(distributed.GetCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAsync_WhenL1MissAndL2Hit_DeserializesAndPopulatesL1()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);
        await distributed.SetAsync("key", System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new CacheValue("l2")), new DistributedCacheEntryOptions());

        var value = await service.GetAsync<CacheValue>("key");
        var second = await service.GetAsync<CacheValue>("key");

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(new CacheValue("l2")));
            Assert.That(second, Is.EqualTo(new CacheValue("l2")));
            Assert.That(distributed.GetCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetAsync_WhenL2Miss_ReturnsNull()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);

        var value = await service.GetAsync<CacheValue>("missing");

        Assert.That(value, Is.Null);
    }

    [Test]
    public async Task GetAsync_WhenL2Throws_ReturnsNullInsteadOfPropagating()
    {
        var distributed = new TestDistributedCache { ThrowOnGet = true };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);

        var value = await service.GetAsync<CacheValue>("key");

        Assert.That(value, Is.Null);
    }

    [Test]
    public async Task RemoveAsync_RemovesFromL1AndL2()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);
        await service.SetAsync("key", new CacheValue("value"));

        await service.RemoveAsync("key");

        Assert.Multiple(() =>
        {
            Assert.That(memory.TryGetValue("key", out _), Is.False);
            Assert.That(distributed.Get("key"), Is.Null);
        });
    }

    [Test]
    public async Task RemoveAsync_WhenL2Throws_DoesNotPropagate()
    {
        var distributed = new TestDistributedCache { ThrowOnRemove = true };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        memory.Set("key", new CacheValue("value"));
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);

        Assert.DoesNotThrowAsync(async () => await service.RemoveAsync("key"));
        Assert.That(memory.TryGetValue("key", out _), Is.False);
    }

    [Test]
    public async Task GetOrCreateAsync_UsesExistingValueWithoutCallingFactory()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);
        await service.SetAsync("key", new CacheValue("existing"));
        var calls = 0;

        var value = await service.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return Task.FromResult(new CacheValue("created"));
        });

        Assert.That(value, Is.EqualTo(new CacheValue("existing")));
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetOrCreateAsync_OnMiss_CallsFactoryAndStoresResult()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);
        var calls = 0;

        var value = await service.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return Task.FromResult(new CacheValue("created"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(new CacheValue("created")));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(distributed.Get("key"), Is.Not.Null);
        });
    }

    [Test]
    public async Task ExistsAsync_ReturnsTrueFromL1()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        memory.Set("key", "value");
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);

        Assert.That(await service.ExistsAsync("key"), Is.True);
        Assert.That(distributed.GetCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task ExistsAsync_ReadsL2WhenL1Misses()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        await distributed.SetAsync("key", [1, 2, 3], new DistributedCacheEntryOptions());
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);

        Assert.That(await service.ExistsAsync("key"), Is.True);
        Assert.That(distributed.GetCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task RemoveByPrefixAsync_DoesNotThrow()
    {
        var distributed = new TestDistributedCache();
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new DistributedCacheService(distributed, memory, NullLogger<DistributedCacheService>.Instance);

        Assert.DoesNotThrowAsync(async () => await service.RemoveByPrefixAsync("language:"));
    }

    private sealed record CacheValue(string Value);

    private sealed class TestDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

        public int GetCalls { get; private set; }
        public bool ThrowOnGet { get; init; }
        public bool ThrowOnRemove { get; init; }

        public byte[]? Get(string key)
        {
            GetCalls++;
            if (ThrowOnGet) throw new InvalidOperationException("GET failure");
            return _entries.TryGetValue(key, out var value) ? value : null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            => Task.FromResult(Get(key));

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key)
        {
            if (ThrowOnRemove) throw new InvalidOperationException("REMOVE failure");
            _entries.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            => _entries[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
