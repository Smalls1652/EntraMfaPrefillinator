using EntraMfaPrefillinator.AuthUpdateApp.Handlers;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Services;
using Microsoft.AspNetCore.Mvc;

namespace EntraMfaPrefillinator.AuthUpdateApp.Endpoints;

public static class AuthUpdateEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost(
            pattern: "/auth-update",
            handler: async (
                UserAuthUpdateQueueItem queueItem,
                [FromServices] IGraphClientService graphClientService,
                [FromServices] ILoggerFactory loggerFactory
            ) => await AuthUpdateHandler.HandleProcessUserAuthUpdate(queueItem, graphClientService, loggerFactory));
    }
}