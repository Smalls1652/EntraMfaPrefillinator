using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models;

namespace EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry.Activities;

public static class EndpointActivityExtensions
{
    public static Activity? StartEndpointCallActivity(this ActivitySource activitySource, string activityName, string endpointName) => StartEndpointCallActivity(activitySource, activityName, endpointName, null);
    public static Activity? StartEndpointCallActivity(this ActivitySource activitySource, string activityName, string endpointName, string? parentActivityId)
    {
        return activitySource.StartActivity(
            name: activityName,
            kind: ActivityKind.Server,
            tags: new ActivityTagsCollection
            {
                { "endpoint.name", endpointName }
            },
            parentId: parentActivityId
        );
    }

    public static Activity? AddUserAuthUpdateQueueItemTags(this Activity? activity, UserAuthUpdateQueueItem queueItem)
    {
        return activity?
            .AddTag("queueItem.userName", queueItem.UserName ?? queueItem.UserPrincipalName)
            .AddTag("queueItem.employeeId", queueItem.EmployeeId);
    }

    public static Activity? AddUserEmailAuthMethodIncludedInRequestTag(this Activity? activity, bool emailAuthMethodIncluded)
    {
        return activity?
            .AddTag("user.auth.email.includedInRequest", emailAuthMethodIncluded);
    }

    public static Activity? AddUserPhoneAuthMethodIncludedInRequestTag(this Activity? activity, bool phoneAuthMethodIncluded)
    {
        return activity?
            .AddTag("user.auth.phone.includedInRequest", phoneAuthMethodIncluded);
    }

    public static Activity? AddUserHasExisitingEmailAuthMethodTag(this Activity? activity, bool hasEmailAuthMethod)
    {
        return activity?
            .AddTag("user.auth.email.hasExisting", hasEmailAuthMethod);
    }

    public static Activity? AddUserHasExisitingPhoneAuthMethodTag(this Activity? activity, bool hasPhoneAuthMethod)
    {
        return activity?
            .AddTag("user.auth.phone.hasExisting", hasPhoneAuthMethod);
    }

    public static Activity? AddUserHadEmailAuthMethodAddedTag(this Activity? activity, bool emailAuthMethodAdded)
    {
        return activity?
            .AddTag("user.auth.email.added", emailAuthMethodAdded);
    }

    public static Activity? AddUserHadPhoneAuthMethodAddedTag(this Activity? activity, bool phoneAuthMethodAdded)
    {
        return activity?
            .AddTag("user.auth.phone.added", phoneAuthMethodAdded);
    }

    public static Activity? AddUserEmailAuthUpdateDryRunTag(this Activity? activity, bool isDryRun)
    {
        return activity?
            .AddTag("user.auth.email.dryRun", isDryRun);
    }

    public static Activity? AddUserPhoneAuthUpdateDryRunTag(this Activity? activity, bool isDryRun)
    {
        return activity?
            .AddTag("user.auth.phone.dryRun", isDryRun);
    }
}