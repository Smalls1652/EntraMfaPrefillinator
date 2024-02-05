using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;

namespace EntraMfaPrefillinator.AuthUpdateApp.Handlers;

public static class AuthUpdateHandler
{
    public static async Task HandleProcessUserAuthUpdate(UserAuthUpdateQueueItem queueItem, IGraphClientService graphClientService, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AuthUpdateHandler");
        
        Stopwatch stopwatch = Stopwatch.StartNew();

        string userName = queueItem.UserName ?? queueItem.UserPrincipalName ?? throw new Exception("'userName' or 'userPrincipalName' must be supplied in the request.");

        User user;
        if (queueItem.UserName is not null || queueItem.EmployeeId is not null)
        {
            try
            {
                user = await graphClientService.GetUserByUserNameAndEmployeeNumberAsync(queueItem.UserName, queueItem.EmployeeId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getting user, '{userName}' [{employeeId}].", queueItem.UserName, queueItem.EmployeeId);
                throw;
            }
        }
        else if (queueItem.UserPrincipalName is not null)
        {
            try
            {
                user = await graphClientService.GetUserAsync(queueItem.UserPrincipalName);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getting user, '{userPrincipalName}'.", queueItem.UserPrincipalName);
                throw;
            }
        }
        else
        {
            throw new Exception("'userName' and 'employeeId' or 'userPrincipalName' must be supplied in the request.");
        }

        if (queueItem.EmailAddress is not null)
        {
            EmailAuthenticationMethod[]? emailAuthMethods = await graphClientService.GetEmailAuthenticationMethodsAsync(user.Id);

            if (emailAuthMethods is not null && emailAuthMethods.Length != 0)
            {
                logger.LogWarning("'{userPrincipalName}' already has email auth methods configured. Skipping...", user.UserPrincipalName);
            }
            else
            {
                try
                {
                    await graphClientService.AddEmailAuthenticationMethodAsync(
                        userId: user.Id,
                        emailAddress: queueItem.EmailAddress
                    );

                    logger.LogInformation("Added email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                }
                catch (GraphClientDryRunException)
                {
                    logger.LogWarning("Dry run is enabled. Skipping adding email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error adding email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                    throw;
                }
            }
        }
        else
        {
            logger.LogWarning("'{userPrincipalName}' did not have an email address supplied in the request. Skipping...", user.UserPrincipalName);
        }

        if (queueItem.PhoneNumber is not null)
        {
            PhoneAuthenticationMethod[]? phoneAuthMethods = await graphClientService.GetPhoneAuthenticationMethodsAsync(user.Id);

            if (phoneAuthMethods is not null && phoneAuthMethods.Length != 0)
            {
                logger.LogWarning("'{userPrincipalName}' already has phone auth methods configured. Skipping...", user.UserPrincipalName);
            }
            else
            {
                try
                {
                    await graphClientService.AddPhoneAuthenticationMethodAsync(
                        userId: user.Id,
                        phoneNumber: queueItem.PhoneNumber
                    );

                    logger.LogInformation("Added phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                }
                catch (GraphClientDryRunException)
                {
                    logger.LogWarning("Dry run is enabled. Skipping adding phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error adding phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                    throw;
                }
            }
        }
        else
        {
            logger.LogWarning("'{userPrincipalName}' did not have a phone number supplied in the request. Skipping...", user.UserPrincipalName);
        }

        stopwatch.Stop();
        logger.LogInformation("Processed request for '{userPrincipalName}' in {ElapsedMilliseconds}ms.", user.UserPrincipalName, stopwatch.ElapsedMilliseconds);
    }
}