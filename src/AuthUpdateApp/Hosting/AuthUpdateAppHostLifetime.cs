using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.AuthUpdateApp.Hosting;

internal sealed class AuthUpdateAppHostLifetime : IHostLifetime, IDisposable
{
    public AuthUpdateAppHostLifetime(IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory)
    {
        Environment = environment;
        ApplicationLifetime = applicationLifetime;
        Logger = loggerFactory.CreateLogger<AuthUpdateAppHostLifetime>();
    }

    private IHostEnvironment Environment { get; }
    private IHostApplicationLifetime ApplicationLifetime { get; }
    private ILogger Logger { get; }

    private PosixSignalRegistration? _sigIntRegistration;
    private PosixSignalRegistration? _sigQuitRegistration;
    private PosixSignalRegistration? _sigTermRegistration;
    private CancellationTokenRegistration _appStartedRegistration;
    private CancellationTokenRegistration _appStoppingRegistration;

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _appStartedRegistration = ApplicationLifetime.ApplicationStarted.Register(state =>
        {
            ((AuthUpdateAppHostLifetime)state!).OnAppStarted();
        }, this);

        _appStoppingRegistration = ApplicationLifetime.ApplicationStopping.Register(state =>
        {
            ((AuthUpdateAppHostLifetime)state!).OnAppStopping();
        }, this);

        RegisterShutdownHandlers();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        UnregisterShutdownHandlers();
        _appStartedRegistration.Dispose();
        _appStoppingRegistration.Dispose();
    }

    private void OnAppStarted()
    {
        Logger.LogDebug("Application started. Press Ctrl+C to shut down.");
    }

    private void OnAppStopping()
    {
        Logger.LogInformation("Application is shutting down...");
    }

    private void RegisterShutdownHandlers()
    {
        Action<PosixSignalContext> handler = HandlePosixSignal;
        _sigIntRegistration = PosixSignalRegistration.Create(PosixSignal.SIGINT, handler);
        _sigQuitRegistration = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handler);
        _sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handler);
    }

    private void HandlePosixSignal(PosixSignalContext context)
    {
        context.Cancel = true;
        ApplicationLifetime.StopApplication();
    }

    private void UnregisterShutdownHandlers()
    {
        _sigIntRegistration?.Dispose();
        _sigQuitRegistration?.Dispose();
        _sigTermRegistration?.Dispose();
    }
}