using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models;

namespace EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry.Activities;

/// <summary>
/// Extension methods for creating and updating activities related to user auth updates.
/// </summary>
internal static class UserAuthUpdateActivityExtensions
{
    /// <summary>
    /// Starts and creates a new activity for processing a user auth update.
    /// </summary>
    /// <param name="activitySource">The <see cref="ActivitySource"/>.</param>
    /// <param name="activityName">The name of the activity.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? StartProcessUserAuthActivity(this ActivitySource activitySource, string activityName) => StartProcessUserAuthActivity(activitySource, activityName, null);

    /// <summary>
    /// Starts and creates a new activity for processing a user auth update.
    /// </summary>
    /// <param name="activitySource">The <see cref="ActivitySource"/>.</param>
    /// <param name="activityName">The name of the activity.</param>
    /// <param name="parentActivityId">The parent activity ID.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? StartProcessUserAuthActivity(this ActivitySource activitySource, string activityName, string? parentActivityId)
    {
        return activitySource.StartActivity(
            name: activityName,
            kind: ActivityKind.Server,
            parentId: parentActivityId
        );
    }

    /// <summary>
    /// Add tags to an activity for a <see cref="UserAuthUpdateQueueItem"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add tags to.</param>
    /// <param name="queueItem">The <see cref="UserAuthUpdateQueueItem"/>.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserAuthUpdateQueueItemTags(this Activity? activity, UserAuthUpdateQueueItem queueItem)
    {
        return activity?
            .AddTag("queueItem.userName", queueItem.UserName ?? queueItem.UserPrincipalName)
            .AddTag("queueItem.employeeId", queueItem.EmployeeId);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether the request
    /// includes an email.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="emailAuthMethodIncluded">Whether the request includes an email.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserEmailAuthMethodIncludedInRequestTag(this Activity? activity, bool emailAuthMethodIncluded)
    {
        return activity?
            .AddTag("user.auth.email.includedInRequest", emailAuthMethodIncluded);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether the request
    /// includes a phone number.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="phoneAuthMethodIncluded">Whether the request includes a phone number.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserPhoneAuthMethodIncludedInRequestTag(this Activity? activity, bool phoneAuthMethodIncluded)
    {
        return activity?
            .AddTag("user.auth.phone.includedInRequest", phoneAuthMethodIncluded);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether the request
    /// includes an alternative phone number.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="phoneAuthMethodIncluded">Whether the request includes a phone number.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserAlternatePhoneAuthMethodIncludedInRequestTag(this Activity? activity, bool phoneAuthMethodIncluded)
    {
        return activity?
            .AddTag("user.auth.alternatePhone.includedInRequest", phoneAuthMethodIncluded);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether the user has an existing
    /// email auth method.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="hasEmailAuthMethod">Whether the user has an existing email auth method.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserHasExisitingEmailAuthMethodTag(this Activity? activity, bool hasEmailAuthMethod)
    {
        return activity?
            .AddTag("user.auth.email.hasExisting", hasEmailAuthMethod);
    }
    
    /// <summary>
    /// Add a tag to an activity that indicates whether the user has an existing
    /// phone auth method.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="hasPhoneAuthMethod">Whether the user has an existing phone auth method.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserHasExisitingPhoneAuthMethodTag(this Activity? activity, bool hasPhoneAuthMethod)
    {
        return activity?
            .AddTag("user.auth.phone.hasExisting", hasPhoneAuthMethod);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether the user has an existing
    /// alternative phone auth method.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="hasPhoneAuthMethod">Whether the user has an existing phone auth method.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserHasExisitingAlternatePhoneAuthMethodTag(this Activity? activity, bool hasPhoneAuthMethod)
    {
        return activity?
            .AddTag("user.auth.alternatePhone.hasExisting", hasPhoneAuthMethod);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether an
    /// email auth method was added to the user during the process.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="emailAuthMethodAdded">Whether an email auth method was added.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserHadEmailAuthMethodAddedTag(this Activity? activity, bool emailAuthMethodAdded)
    {
        return activity?
            .AddTag("user.auth.email.added", emailAuthMethodAdded);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether a
    /// phone auth method was added to the user during the process.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="phoneAuthMethodAdded">Whether a phone auth method was added.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserHadPhoneAuthMethodAddedTag(this Activity? activity, bool phoneAuthMethodAdded)
    {
        return activity?
            .AddTag("user.auth.phone.added", phoneAuthMethodAdded);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether an
    /// alternative phone auth method was added to the user during the process.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="phoneAuthMethodAdded">Whether a phone auth method was added.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserHadAlternatePhoneAuthMethodAddedTag(this Activity? activity, bool phoneAuthMethodAdded)
    {
        return activity?
            .AddTag("user.auth.alternatePhone.added", phoneAuthMethodAdded);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether an
    /// email auth method wasn't added to the user during the process
    /// because the app is in "dry run" mode.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="isDryRun">Whether the app is in "dry run" mode.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserEmailAuthUpdateDryRunTag(this Activity? activity, bool isDryRun)
    {
        return activity?
            .AddTag("user.auth.email.dryRun", isDryRun);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether a
    /// phone auth method wasn't added to the user during the process
    /// because the app is in "dry run" mode.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="isDryRun">Whether the app is in "dry run" mode.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserPhoneAuthUpdateDryRunTag(this Activity? activity, bool isDryRun)
    {
        return activity?
            .AddTag("user.auth.phone.dryRun", isDryRun);
    }

    /// <summary>
    /// Add a tag to an activity that indicates whether an
    /// alternative phone auth method wasn't added to the user during the process
    /// because the app is in "dry run" mode.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add the tag to.</param>
    /// <param name="isDryRun">Whether the app is in "dry run" mode.</param>
    /// <returns>The modified <see cref="Activity"/>.</returns>
    public static Activity? AddUserAlternatePhoneAuthUpdateDryRunTag(this Activity? activity, bool isDryRun)
    {
        return activity?
            .AddTag("user.auth.alternatePhone.dryRun", isDryRun);
    }
}