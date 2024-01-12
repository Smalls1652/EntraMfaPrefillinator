namespace EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

/// <summary>
/// Houses methods for writing to the console.
/// </summary>
public static class ConsoleUtils
{
    /// <summary>
    /// Writes an error message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a warning message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Out.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a success message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Out.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Writes an info message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Out.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a normal message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteOutput(string message)
    {
        Console.Out.WriteLine(message);
    }
}