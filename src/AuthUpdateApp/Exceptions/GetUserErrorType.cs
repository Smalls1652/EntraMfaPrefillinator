namespace EntraMfaPrefillinator.AuthUpdateApp;

/// <summary>
/// The type of error that occurred when attempting to get a user.
/// </summary>
internal enum GetUserErrorType
{
    UserNotFound,
    MissingUsername
}