using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.FunctionApp.Functions;

public class ProcessUserAuthUpdateQueue
{
    private readonly IGraphClientService _graphClientService;
    private readonly IQueueClientService _queueClientService;

    public ProcessUserAuthUpdateQueue(IGraphClientService graphClientService, IQueueClientService queueClientService)
    {
        _graphClientService = graphClientService;
        _queueClientService = queueClientService;
    }

    [Function("ProcessUserAuthUpdateQueue")]
    public async Task Run(
        [QueueTrigger("authupdate-queue")] string queueItemContents,
        FunctionContext executionContext
    )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var logger = executionContext.GetLogger(nameof(ProcessUserAuthUpdateQueue));

        UserAuthUpdateQueueItem queueItem = JsonSerializer.Deserialize(
            json: queueItemContents,
            jsonTypeInfo: QueueJsonContext.Default.UserAuthUpdateQueueItem
        )!;

        logger.LogInformation("Received request for {UserPrincipalName}.", queueItem.UserPrincipalName);

        User user;
        try
        {
            user = await _graphClientService.GetUserAsync(queueItem.UserPrincipalName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting user, {UserPrincipalName}.", queueItem.UserPrincipalName);
            throw;
        }

        if (queueItem.EmailAddress is not null)
        {
            EmailAuthenticationMethod[]? emailAuthMethods = await _graphClientService.GetEmailAuthenticationMethodsAsync(user.Id);

            if (emailAuthMethods is not null && emailAuthMethods.Length != 0)
            {
                logger.LogWarning("'{UserPrincipalName}' already has email auth methods configured. Skipping...", queueItem.UserPrincipalName);
            }
            else
            {
                try
                {
                    await _graphClientService.AddEmailAuthenticationMethodAsync(
                        userId: user.Id,
                        emailAddress: queueItem.EmailAddress
                    );

                    logger.LogInformation("Added email auth method for {UserPrincipalName}.", queueItem.UserPrincipalName);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error adding email auth method for {UserPrincipalName}.", queueItem.UserPrincipalName);
                    throw;
                }
            }
        }
        else
        {
            logger.LogWarning("'{UserPrincipalName}' did not have an email address supplied in the request. Skipping...", queueItem.UserPrincipalName);
        }

        if (queueItem.PhoneNumber is not null)
        {
            PhoneAuthenticationMethod[]? phoneAuthMethods = await _graphClientService.GetPhoneAuthenticationMethodsAsync(user.Id);

            if (phoneAuthMethods is not null && phoneAuthMethods.Length != 0)
            {
                logger.LogWarning("'{UserPrincipalName}' already has phone auth methods configured. Skipping...", queueItem.UserPrincipalName);
            }
            else
            {
                try
                {
                    await _graphClientService.AddPhoneAuthenticationMethodAsync(
                        userId: user.Id,
                        phoneNumber: queueItem.PhoneNumber
                    );

                    logger.LogInformation("Added phone auth method for {UserPrincipalName}.", queueItem.UserPrincipalName);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error adding phone auth method for {UserPrincipalName}.", queueItem.UserPrincipalName);
                    throw;
                }
            }
        }
        else
        {
            logger.LogWarning("'{UserPrincipalName}' did not have a phone number supplied in the request. Skipping...", queueItem.UserPrincipalName);
        }

        stopwatch.Stop();
        logger.LogInformation("Processed request for {UserPrincipalName} in {ElapsedMilliseconds}ms.", queueItem.UserPrincipalName, stopwatch.ElapsedMilliseconds);
    }
}