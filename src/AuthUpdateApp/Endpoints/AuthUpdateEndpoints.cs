using EntraMfaPrefillinator.AuthUpdateApp.Handlers;

namespace EntraMfaPrefillinator.AuthUpdateApp.Endpoints;

public static class AuthUpdateEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost(
            pattern: "/authupdate",
            handler: AuthUpdateHandler.HandleProcessUserAuthUpdate
        );
    }
}