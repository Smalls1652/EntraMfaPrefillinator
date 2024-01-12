namespace EntraMfaPrefillinator.Lib.Services;

public class GraphClientDryRunException : Exception
{
    public GraphClientDryRunException() { }
    public GraphClientDryRunException(string message) : base(message) { }
    public GraphClientDryRunException(string message, Exception inner) : base(message, inner) { }
}