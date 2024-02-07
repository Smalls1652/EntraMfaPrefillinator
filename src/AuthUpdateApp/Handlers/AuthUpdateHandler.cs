using System.Diagnostics;
using EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry.Activities;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;

namespace EntraMfaPrefillinator.AuthUpdateApp.Handlers;

public static class AuthUpdateHandler
{
    public static async Task HandleProcessUserAuthUpdate(
        UserAuthUpdateQueueItem queueItem,
        IGraphClientService graphClientService,
        ILoggerFactory loggerFactory
    )
    {
        using ActivitySource endpointsActivitySource = new("EntraMfaPrefillinator.AuthUpdateApp.Endpoints");
        using var activity = endpointsActivitySource.StartEndpointCallActivity(
            activityName: "ProcessUserAuthUpdate",
            endpointName: "/authupdate"
        );

        activity?.AddUserAuthUpdateQueueItemTags(
            queueItem: queueItem
        );

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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user, '{userName}' [{employeeId}].", queueItem.UserName, queueItem.EmployeeId);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
        else if (queueItem.UserPrincipalName is not null)
        {
            try
            {
                user = await graphClientService.GetUserAsync(queueItem.UserPrincipalName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user, '{userPrincipalName}'.", queueItem.UserPrincipalName);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
        else
        {
            Exception ex = new("'userName' and 'employeeId' or 'userPrincipalName' must be supplied in the request.");
            logger.LogError(ex, "Required parameters not supplied in the request.");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw ex;
        }

        if (queueItem.EmailAddress is not null)
        {
            activity?.AddUserEmailAuthMethodIncludedInRequestTag(true);
            EmailAuthenticationMethod[]? emailAuthMethods = await graphClientService.GetEmailAuthenticationMethodsAsync(user.Id);

            if (emailAuthMethods is not null && emailAuthMethods.Length != 0)
            {
                logger.LogWarning("'{userPrincipalName}' already has email auth methods configured. Skipping...", user.UserPrincipalName);
                activity?.AddUserHasExisitingEmailAuthMethodTag(true);
            }
            else
            {
                activity?.AddUserHasExisitingEmailAuthMethodTag(false);
                try
                {
                    await graphClientService.AddEmailAuthenticationMethodAsync(
                        userId: user.Id,
                        emailAddress: queueItem.EmailAddress
                    );

                    logger.LogInformation("Added email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                    activity?.AddUserHadEmailAuthMethodAddedTag(true);
                    activity?.AddUserEmailAuthUpdateDryRunTag(false);
                }
                catch (GraphClientDryRunException)
                {
                    logger.LogWarning("Dry run is enabled. Skipping adding email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                    activity?.AddUserEmailAuthUpdateDryRunTag(true);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error adding email auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    throw;
                }
            }
        }
        else
        {
            logger.LogWarning("'{userPrincipalName}' did not have an email address supplied in the request. Skipping...", user.UserPrincipalName);
            activity?.AddUserEmailAuthMethodIncludedInRequestTag(false);
        }

        if (queueItem.PhoneNumber is not null)
        {
            activity?.AddUserPhoneAuthMethodIncludedInRequestTag(true);
            PhoneAuthenticationMethod[]? phoneAuthMethods = await graphClientService.GetPhoneAuthenticationMethodsAsync(user.Id);

            if (phoneAuthMethods is not null && phoneAuthMethods.Length != 0)
            {
                logger.LogWarning("'{userPrincipalName}' already has phone auth methods configured. Skipping...", user.UserPrincipalName);
                activity?.AddUserHasExisitingPhoneAuthMethodTag(true);
            }
            else
            {
                activity?.AddUserHasExisitingPhoneAuthMethodTag(false);
                try
                {
                    await graphClientService.AddPhoneAuthenticationMethodAsync(
                        userId: user.Id,
                        phoneNumber: queueItem.PhoneNumber
                    );

                    logger.LogInformation("Added phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                    activity?.AddUserHadPhoneAuthMethodAddedTag(true);
                    activity?.AddUserPhoneAuthUpdateDryRunTag(false);
                }
                catch (GraphClientDryRunException)
                {
                    logger.LogWarning("Dry run is enabled. Skipping adding phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                    activity?.AddUserPhoneAuthUpdateDryRunTag(true);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error adding phone auth method for '{userPrincipalName}'.", user.UserPrincipalName);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    throw;
                }
            }
        }
        else
        {
            logger.LogWarning("'{userPrincipalName}' did not have a phone number supplied in the request. Skipping...", user.UserPrincipalName);
            activity?.AddUserPhoneAuthMethodIncludedInRequestTag(false);
        }

        stopwatch.Stop();
        logger.LogInformation("Processed request for '{userPrincipalName}' in {ElapsedMilliseconds}ms.", user.UserPrincipalName, stopwatch.ElapsedMilliseconds);
    }
}