using Azure.Core;
using Azure.Storage.Queues;

namespace EntraMfaPrefillinator.Lib.Azure.Services;

public partial class QueueClientService : IQueueClientService
{
    private readonly QueueClient _authUpdateQueueClient;

    public QueueClientService(string connectionString)
    {
        _authUpdateQueueClient = new(
            connectionString: connectionString,
            queueName: "authupdate-queue",
            options: new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }
        );
        _authUpdateQueueClient.CreateIfNotExists();
    }

    public QueueClientService(Uri queueUri, TokenCredential tokenCredential)
    {
        _authUpdateQueueClient = new(
            queueUri: queueUri,
            credential: tokenCredential,
            options: new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }
        );
        _authUpdateQueueClient.CreateIfNotExists();
    }

    public QueueClient AuthUpdateQueueClient => _authUpdateQueueClient;
}