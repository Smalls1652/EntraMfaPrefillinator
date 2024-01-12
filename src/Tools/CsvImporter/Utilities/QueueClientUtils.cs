using System.Text.Json;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Services;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

/// <summary>
/// Houses methods for sending messages to the Azure Storage Queue.
/// </summary>
public static class QueueClientUtils
{
    /// <summary>
    /// Sends a <see cref="UserAuthUpdateQueueItem"/> to the Azure Storage Queue.
    /// </summary>
    /// <param name="queueClientService">The <see cref="QueueClientService"/> to use.</param>
    /// <param name="semaphoreSlim">The <see cref="SemaphoreSlim"/> to use.</param>
    /// <param name="userAuthUpdate">The auth update to send.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static Task SendUserAuthUpdateQueueItemAsync(QueueClientService queueClientService, SemaphoreSlim semaphoreSlim, UserAuthUpdateQueueItem userAuthUpdate)
    {
        var sendToQueueTask = Task.Run(async () =>
        {
            // Wait for the semaphore to be available before sending the message.
            await semaphoreSlim.WaitAsync();

            try
            {
                // Serialize the user auth update to JSON and send it to the queue.
                string userItemJson = JsonSerializer.Serialize(
                    value: userAuthUpdate,
                    jsonTypeInfo: CoreJsonContext.Default.UserAuthUpdateQueueItem
                );

                try
                {
                    await queueClientService.AuthUpdateQueueClient.SendMessageAsync(
                        messageText: userItemJson
                    );
                }
                catch (Exception ex)
                {
                    ConsoleUtils.WriteError($"Error sending message to queue for '{userAuthUpdate.UserName}': {ex.Message}");

                }
            }
            finally
            {
                // Release the semaphore when the message has been sent.
                semaphoreSlim.Release();
            }
        });

        return sendToQueueTask;
    }
}