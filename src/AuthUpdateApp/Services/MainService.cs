using System.Diagnostics;
using Azure;
using Azure.Storage.Queues.Models;
using EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry.Activities;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.AuthUpdateApp.Services;

public class MainService : IHostedService, IDisposable
{
    private bool _disposed;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger _logger;
    private readonly IGraphClientService _graphClientService;
    private readonly IQueueClientService _queueClientService;
    private readonly ActivitySource _activitySource = new("EntraMfaPrefillinator.AuthUpdateApp.Services.MainService");
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public MainService(IHostApplicationLifetime appLifetime, ILogger<MainService> logger, IGraphClientService graphClientService, IQueueClientService queueClientService)
    {
        _appLifetime = appLifetime;
        _logger = logger;
        _graphClientService = graphClientService;
        _queueClientService = queueClientService;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartEndpointCallActivity(
            activityName: "ProcessUserAuthUpdate",
            endpointName: "/authupdate"
        );

        Stopwatch stopwatch = Stopwatch.StartNew();

        bool errorOccurred = false;

        _logger.LogInformation("Checking for messages in queue...");
        NullableResponse<QueueMessage> queueMessage = await _queueClientService.AuthUpdateQueueClient.ReceiveMessageAsync(
            cancellationToken: cancellationToken
        );

        if (queueMessage.Value is null)
        {
            _logger.LogWarning("No messages found in queue.");
            stopwatch.Stop();
            _appLifetime.StopApplication();
            return;
        }

        try
        {
            Stream stream = queueMessage.Value.Body.ToStream();

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

            activity?.AddUserAuthUpdateQueueItemTags(
                queueItem: queueItem
            );

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
                _logger.LogInformation("Deleting message '{messageId}' from queue.", queueMessage.Value.MessageId);
                await _queueClientService.AuthUpdateQueueClient.DeleteMessageAsync(queueMessage.Value.MessageId, queueMessage.Value.PopReceipt, cancellationToken);
            }
            else
            {
                if (queueMessage.Value.DequeueCount >= 5)
                {
                    _logger.LogWarning("Message '{messageId}' has been dequeued 5 times. Deleting message from queue.", queueMessage.Value.MessageId);
                    await _queueClientService.AuthUpdateQueueClient.DeleteMessageAsync(queueMessage.Value.MessageId, queueMessage.Value.PopReceipt, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Error occurred processing message '{messageId}'. Message will be left in queue.", queueMessage.Value.MessageId);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation("Processed request in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);

            _appLifetime.StopApplication();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(() => Task.Run(async () => await RunAsync(cancellationToken)));

        _appLifetime.ApplicationStopping.Register(() => _cancellationTokenSource.Cancel());

        _logger.LogInformation("MainService started.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MainService stopped.");

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _activitySource.Dispose();
        _cancellationTokenSource.Cancel();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}