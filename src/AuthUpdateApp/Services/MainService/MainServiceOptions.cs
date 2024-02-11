namespace EntraMfaPrefillinator.AuthUpdateApp.Services;

/// <summary>
/// Config options for <see cref="MainService"/>.
/// </summary>
internal sealed class MainServiceOptions
{
    /// <summary>
    /// The maximum number of messages to process in a single run.
    /// </summary>
    /// <remarks>
    /// The max value can go no higher than 32.
    /// </remarks>
    public int MaxMessages { get; set; } = 32;
}
