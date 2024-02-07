using EntraMfaPrefillinator.AuthUpdateApp;
using EntraMfaPrefillinator.AuthUpdateApp.Endpoints;
using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile(
        path: builder.Environment.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json",
        optional: true,
        reloadOnChange: true
    );

builder.Logging
    .AddConsole();

builder.Services
    .AddGraphClientService(
        graphClientConfig: new()
        {
            ClientId = builder.Configuration.GetValue<string>("CLIENT_ID") ?? throw new NullReferenceException("CLIENT_ID is not set"),
            TenantId = builder.Configuration.GetValue<string>("TENANT_ID") ?? throw new NullReferenceException("TENANT_ID is not set"),
            Credential = new GraphClientCredential(
                credentialType: GraphClientCredentialType.ClientSecret,
                clientSecret: builder.Configuration.GetValue<string>("CLIENT_SECRET") ?? throw new NullReferenceException("CLIENT_SECRET is not set")
            ),
            ApiScopes = [ "https://graph.microsoft.com/.default" ]
        },
        disableAuthUpdate: builder.Configuration.GetValue<bool>("ENABLE_DRY_RUN")
    );

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, CoreJsonContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, QueueJsonContext.Default);
});

builder.WebHost.UseKestrelHttpsConfiguration();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

AuthUpdateEndpoints.Map(app);

await app.RunAsync();
