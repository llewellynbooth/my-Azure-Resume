using Microsoft.Azure.Cosmos;

namespace Company.Function;

/// <summary>Persists contact-form submissions.</summary>
public class MessageStore
{
    private readonly Container _container;

    public MessageStore(CosmosClient cosmos) =>
        _container = cosmos.GetContainer(Db.Database, Db.MessagesContainer);

    public Task AddAsync(ContactMessage message, CancellationToken ct) =>
        _container.CreateItemAsync(message, new PartitionKey(message.Id), cancellationToken: ct);
}
