namespace EntraMfaPrefillinator.AuthUpdateApp;

/// <summary>
/// An exception that is thrown when an error occurs while attempting to get a user.
/// </summary>
internal class GetUserException : Exception
{
    public GetUserException(GetUserErrorType errorType)
        : this(errorType, null, null)
    {
    }

    public GetUserException(GetUserErrorType errorType, string? message)
        : this(errorType, message, null)
    {
    }

    public GetUserException(GetUserErrorType errorType, string? message = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorType = errorType;
    }

    /// <summary>
    /// The type of error that occurred when attempting to get a user.
    /// </summary>
    public GetUserErrorType ErrorType { get; }
}