namespace EntraMfaPrefillinator.Tools.CsvImporter;

public interface IMainService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
