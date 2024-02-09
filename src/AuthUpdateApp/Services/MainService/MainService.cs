using System.Diagnostics;
using Azure;
using Azure.Storage.Queues.Models;
using EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry.Activities;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EntraMfaPrefillinator.AuthUpdateApp.Services;

/// <summary>
/// The main service for the application.
/// </summary>
public class MainService : IHostedService, IDisposable
{
    private bool _disposed;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger _logger;
    private readonly IGraphClientService _graphClientService;
    private readonly IQueueClientService _queueClientService;
    private readonly ActivitySource _activitySource = new("EntraMfaPrefillinator.AuthUpdateApp.Services.MainService");
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly MainServiceOptions _options;

    public MainService(IHostApplicationLifetime appLifetime, ILogger<MainService> logger, IGraphClientService graphClientService, IQueueClientService queueClientService, IOptions<MainServiceOptions> options)
    {
        _appLifetime = appLifetime;
        _logger = logger;
        _graphClientService = graphClientService;
        _queueClientService = queueClientService;
        _options = options.Value;
    }

    /// <summary>
    /// Starts the operation to get and process queue messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using Activity? activity = _activitySource
            .StartActivity(
                name: "GetAndProcessQueueMessages",
                kind: ActivityKind.Internal
            );

        NullableResponse<QueueMessage[]> queueMessages = await _queueClientService.AuthUpdateQueueClient.ReceiveMessagesAsync(
            maxMessages: _options.MaxMessages,
            cancellationToken: cancellationToken
        );

        if (queueMessages.Value is null || queueMessages.Value.Length == 0)
        {
            _logger.LogWarning("No messages found in queue.");
            _appLifetime.StopApplication();
            return;
        }

        _logger.LogInformation("Processing {MessageCount} messages from queue.", queueMessages.Value.Length);

        Task[] tasks = new Task[queueMessages.Value.Length];

        for (int i = 0; i < queueMessages.Value.Length; i++)
        {
            tasks[i] = ProcessQueueMessageAsync(queueMessages.Value[i], activity?.Id!, cancellationToken);
        }

        await Task.WhenAll(tasks);

        activity?.SetStatus(ActivityStatusCode.Ok);

        _appLifetime.StopApplication();

        return;
    }

    /// <summary>
    /// Processes a queue message to update a user's authentication methods.
    /// </summary>
    /// <param name="queueMessage">The queue message to process.</param>
    /// <param name="parentActivityId">The ID for the parent activity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task ProcessQueueMessageAsync(QueueMessage queueMessage, string parentActivityId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartProcessUserAuthActivity(
            activityName: "ProcessUserAuthUpdate"
        );

        Stopwatch stopwatch = Stopwatch.StartNew();

        bool errorOccurred = false;

        try
        {
            // Read the contents of the queue message and
            // deserialize it into a UserAuthUpdateQueueItem.
            Stream stream = queueMessage.Body.ToStream();

            UserAuthUpdateQueueItem queueItem;
            try
            {
                queueItem = await JsonSerializer.DeserializeAsync(
                    utf8Json: stream,
                    jsonTypeInfo: QueueJsonContext.Default.UserAuthUpdateQueueItem,
                    cancellationToken: cancellationToken
                ) ?? throw new Exception("Failed to deserialize queue message.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing queue message.");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                errorOccurred = true;
                throw;
            }

            // Add tags to the activity for the queue item.
            activity?.AddUserAuthUpdateQueueItemTags(
                queueItem: queueItem
            );

            // Check if the request has a user name or user principal name.
            // If not, throw an exception.
            try
            {
                string userName = queueItem.UserName ?? queueItem.UserPrincipalName ?? throw new Exception("'userName' or 'userPrincipalName' must be supplied in the request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user, '{userName}' [{employeeId}].", queueItem.UserName, queueItem.EmployeeId);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                errorOccurred = true;
                throw;
            }

            // Get the user from the Graph API.
            User user;
            if (queueItem.UserName is not null || queueItem.EmployeeId is not null)
            {
                try
                {
                    user = await _graphClientService.GetUserByUserNameAndEmployeeNumberAsync(queueItem.UserName, queueItem.EmployeeId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting user, '{userName}' [{employeeId}].", queueItem.UserName, queueItem.EmployeeId);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    errorOccurred = true;
                    throw;
                }
            }
            else if (queueItem.UserPrincipalName is not null)
            {
                try
                {
                    user = await _graphClientService.GetUserAsync(queueItem.UserPrincipalName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting user, '{userPrincipalName}'.", queueItem.UserPrincipalName);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    errorOccurred = true;
                    throw;
                }
            }
            else
            {
                Exception ex = new("'userName' and 'employeeId' or 'userPrincipalName' must be supplied in the request.");
                _logger.LogError(ex, "Required parameters not supplied in the request.");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                errorOccurred = true;
                throw ex;
            }

            // If the request has an email address,
            // check if the user already has an email auth method and
            // add the email auth method if it doesn't exist.
            if (queueItem.EmailAddress is not null)
            {
                activity?.AddUserEmailAuthMethodIncludedInRequestTag(true);
                EmailAuthenticationMethod[]? emailAuthMethods = await _graphClientService.GetEmailAuthenticationMethodsAsync(user.Id);

                if (emailAuthMethods is not null && emailAuthMethods.Length != 0)
                {
                    _logger.LogWarning("'{userPrincipalName}' already has email auth methods configured. Skipping...", user.UserPrincipalName);
                    activity?.AddUserHasExisitingEmailAuthMethodTag(true);
                }
                else
                {
                    activity?.AddUserHasExisitingEmailAuthMethodTag(false);
                    try
                    {
                        await _graphClientService.AddEmailAuthenticationMethodAsync(
                            userId: user.Id,
                            emailAddress: queueItem.EmailAddress
                        );

                        _logger.LogInformation("Added email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        activity?.AddUserHadEmailAuthMethodAddedTag(true);
                        activity?.AddUserEmailAuthUpdateDryRunTag(false);
                    }
                    catch (GraphClientDryRunException)
                    {
                        _logger.LogWarning("Dry run is enabled. Skipping adding email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        activity?.AddUserEmailAuthUpdateDryRunTag(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        errorOccurred = true;
                        throw;
                    }
                }
            }
            else
            {
                _logger.LogWarning("'{userPrincipalName}' did not have an email address supplied in the request. Skipping...", user.UserPrincipalName);
                activity?.AddUserEmailAuthMethodIncludedInRequestTag(false);
            }

            // If the request has a phone number,
            // check if the user already has a phone auth method and
            // add the phone auth method if it doesn't exist.
            if (queueItem.PhoneNumber is not null)
            {
                activity?.AddUserPhoneAuthMethodIncludedInRequestTag(true);
                PhoneAuthenticationMethod[]? phoneAuthMethods = await _graphClientService.GetPhoneAuthenticationMethodsAsync(user.Id);

                if (phoneAuthMethods is not null && phoneAuthMethods.Length != 0)
                {
                    _logger.LogWarning("'{userPrincipalName}' already has phone auth methods configured. Skipping...", user.UserPrincipalName);
                    activity?.AddUserHasExisitingPhoneAuthMethodTag(true);
                }
                else
                {
                    activity?.AddUserHasExisitingPhoneAuthMethodTag(false);
                    try
                    {
                        await _graphClientService.AddPhoneAuthenticationMethodAsync(
                            userId: user.Id,
                            phoneNumber: queueItem.PhoneNumber
                        );

                        _logger.LogInformation("Added phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        activity?.AddUserHadPhoneAuthMethodAddedTag(true);
                        activity?.AddUserPhoneAuthUpdateDryRunTag(false);
                    }
                    catch (GraphClientDryRunException)
                    {
                        _logger.LogWarning("Dry run is enabled. Skipping adding phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        activity?.AddUserPhoneAuthUpdateDryRunTag(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        errorOccurred = true;
                        throw;
                    }
                }
            }
            else
            {
                _logger.LogWarning("'{userPrincipalName}' did not have a phone number supplied in the request. Skipping...", user.UserPrincipalName);
                activity?.AddUserPhoneAuthMethodIncludedInRequestTag(false);
            }

            return;
        }
        finally
        {
            if (!errorOccurred)
            {
                // Delete the message from the queue if no error occurred.
                _logger.LogInformation("Deleting message '{messageId}' from queue.", queueMessage.MessageId);
                await _queueClientService.AuthUpdateQueueClient.DeleteMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt, cancellationToken);
            }
            else
            {
                // If an error occurred processing the message,
                // check if the message has been dequeued 5 times.
                if (queueMessage.DequeueCount >= 5)
                {
                    // If the message has been dequeued 5 times, delete the message from the queue.
                    _logger.LogWarning("Message '{messageId}' has been dequeued 5 times. Deleting message from queue.", queueMessage.MessageId);
                    await _queueClientService.AuthUpdateQueueClient.DeleteMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt, cancellationToken);
                }
                else
                {
                    // Otherwise, log a warning and leave the message in the queue.
                    _logger.LogWarning("Error occurred processing message '{messageId}'. Message will be left in queue.", queueMessage.MessageId);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation("Processed request in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(() => Task.Run(async () => await RunAsync(cancellationToken)));

        _appLifetime.ApplicationStopping.Register(() => _cancellationTokenSource.Cancel());

        _logger.LogInformation("MainService started.");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MainService stopped.");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _activitySource.Dispose();
        _cancellationTokenSource.Cancel();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}