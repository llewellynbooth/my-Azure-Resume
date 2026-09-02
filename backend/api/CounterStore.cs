using System.Net;
using Microsoft.Azure.Cosmos;

namespace Company.Function;

/// <summary>Reads and increments the single visitor-counter document.</summary>
public class CounterStore
{
    private readonly Container _container;

    public CounterStore(CosmosClient cosmos) =>
        _container = cosmos.GetContainer(Db.Database, Db.CounterContainer);

    private static PartitionKey Key => new(Db.CounterId);

    public async Task<int> GetAsync(CancellationToken ct)
    {
        try
        {
            var read = await _container.ReadItemAsync<Counter>(Db.CounterId, Key, cancellationToken: ct);
            return read.Resource.Count;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return 0;
        }
    }

    /// <summary>Atomic server-side increment — no read-modify-write race.</summary>
    public async Task<int> IncrementAsync(CancellationToken ct)
    {
        try
        {
            var patched = await _container.PatchItemAsync<Counter>(
                Db.CounterId, Key,
                new[] { PatchOperation.Increment("/count", 1) },
                cancellationToken: ct);
            return patched.Resource.Count;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            await _container.CreateItemAsync(
                new Counter { Id = Db.CounterId, Count = 1 }, Key, cancellationToken: ct);
            return 1;
        }
    }

    /// <summary>True if the container is reachable (a missing document still counts as reachable).</summary>
    public async Task<bool> PingAsync(CancellationToken ct)
    {
        try
        {
            await _container.ReadItemAsync<Counter>(Db.CounterId, Key, cancellationToken: ct);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }
}
