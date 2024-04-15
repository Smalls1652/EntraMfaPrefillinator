using System.Diagnostics;

using Azure;
using Azure.Storage.Queues.Models;

using EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry.Activities;
using EntraMfaPrefillinator.Lib.Azure.Services;
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
internal sealed class MainService : IHostedService, IDisposable
{
    private bool _disposed;
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger _logger;
    private readonly IGraphClientService _graphClientService;
    private readonly IQueueClientService _queueClientService;
    private readonly ActivitySource _activitySource = new("EntraMfaPrefillinator.AuthUpdateApp.Services.MainService");
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

        _logger.LogInformation("Getting messages from queue...");

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

        _logger.LogInformation("Waiting for all tasks to complete...");

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing queue messages.");
        }

        activity?.SetStatus(ActivityStatusCode.Ok);

        _appLifetime.StopApplication();
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
            activityName: "ProcessUserAuthUpdate",
            parentActivityId: parentActivityId
        );

        Stopwatch stopwatch = Stopwatch.StartNew();

        bool errorOccurred = false;

        try
        {
            // Read the contents of the queue message and
            // deserialize it into a UserAuthUpdateQueueItem.
            Stream stream = queueMessage.Body.ToStream();

            UserAuthUpdateQueueItem queueItem = await JsonSerializer.DeserializeAsync(
                utf8Json: stream,
                jsonTypeInfo: QueueJsonContext.Default.UserAuthUpdateQueueItem,
                cancellationToken: cancellationToken
            ) ?? throw new Exception("Failed to deserialize queue message.");

            // Add tags to the activity for the queue item.
            activity?.AddUserAuthUpdateQueueItemTags(
                queueItem: queueItem
            );

            // Check if the request has a user name or user principal name.
            // If not, throw an exception.
            string userName = queueItem.UserName ?? queueItem.UserPrincipalName ?? throw new Exception("'userName' or 'userPrincipalName' must be supplied in the request.");

            // Get the user from the Graph API.
            User user;
            try
            {
                user = await GetUserAsync(queueItem, activity);
            }
            catch (GetUserException)
            {
                //_logger.LogError(ex, "Error getting user, '{userName}' [{employeeId}].", queueItem.UserName, queueItem.EmployeeId);
                //activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                //errorOccurred = true;
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            // If the request has an email address,
            // check if the user already has an email auth method and
            // add the email auth method if it doesn't exist.
            if (queueItem.EmailAddress is not null)
            {
                activity?.AddUserEmailAuthMethodIncludedInRequestTag(true);
                EmailAuthenticationMethod[]? emailAuthMethods = await _graphClientService.GetEmailAuthenticationMethodsAsync(user.Id, activity?.Id);

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
                    catch (Exception)
                    {
                        //_logger.LogError(ex, "Error adding email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        //activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        //errorOccurred = true;
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
                PhoneAuthenticationMethod[]? phoneAuthMethods = await _graphClientService.GetPhoneAuthenticationMethodsAsync(user.Id, activity?.Id);

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
                    catch (Exception)
                    {
                        //_logger.LogError(ex, "Error adding phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        //activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        //errorOccurred = true;
                        throw;
                    }
                }
            }
            else if (queueItem.PhoneNumber is null && queueItem.HomePhone is not null)
            {
                activity?.AddUserPhoneAuthMethodIncludedInRequestTag(true);
                PhoneAuthenticationMethod[]? phoneAuthMethods = await _graphClientService.GetPhoneAuthenticationMethodsAsync(user.Id, activity?.Id);

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
                            phoneNumber: queueItem.HomePhone
                        );

                        _logger.LogInformation("Added home phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        activity?.AddUserHadPhoneAuthMethodAddedTag(true);
                        activity?.AddUserPhoneAuthUpdateDryRunTag(false);
                    }
                    catch (GraphClientDryRunException)
                    {
                        _logger.LogWarning("Dry run is enabled. Skipping adding home phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        activity?.AddUserPhoneAuthUpdateDryRunTag(true);
                    }
                    catch (Exception)
                    {
                        //_logger.LogError(ex, "Error adding phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                        //activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        //errorOccurred = true;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            errorOccurred = true;
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

    /// <summary>
    /// Gets a user from the Graph API.
    /// </summary>
    /// <param name="queueItem">The queue item containing the user's information.</param>
    /// <returns>The user from the Graph API.</returns>
    /// <exception cref="GetUserException">An error occurred while getting the user.</exception>
    private async Task<User> GetUserAsync(UserAuthUpdateQueueItem queueItem, Activity? activity)
    {
        User user;
        if (queueItem.UserName is not null || queueItem.EmployeeId is not null)
        {
            try
            {
                user = await _graphClientService.GetUserByUserNameAndEmployeeNumberAsync(queueItem.UserName, queueItem.EmployeeId, activity?.Id) ?? throw new NullReferenceException("Returned user was null.");
            }
            catch (Exception ex)
            {
                throw new GetUserException(
                    errorType: GetUserErrorType.UserNotFound,
                    message: "An error occurred while getting the user.",
                    innerException: ex
                );
            }
        }
        else if (queueItem.UserPrincipalName is not null)
        {
            try
            {
                user = await _graphClientService.GetUserAsync(queueItem.UserPrincipalName, activity?.Id) ?? throw new NullReferenceException("Returned user was null.");
            }
            catch (Exception ex)
            {
                throw new GetUserException(
                    errorType: GetUserErrorType.UserNotFound,
                    message: "An error occurred while getting the user.",
                    innerException: ex
                );
            }
        }
        else
        {
            throw new GetUserException(
                errorType: GetUserErrorType.MissingUsername,
                message: "A username or user principal name must be supplied in the request."
            );
        }

        return user;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _runTask = RunAsync(_cts.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_runTask is not null)
        {
            try
            {
                _cts?.Cancel();
            }
            finally
            {
                await _runTask
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _activitySource.Dispose();
        _cts?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}