using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.FunctionApp.Functions;

public class ProcessUserAuthUpdateHttp
{
    private readonly IGraphClientService _graphClientService;

    public ProcessUserAuthUpdateHttp(IGraphClientService graphClientService)
    {
        _graphClientService = graphClientService;
    }

    [Function("ProcessUserAuthUpdateHttp")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(methods: "post", Route = "authenticationMethods")]
        HttpRequestData requestData,
        FunctionContext executionContext
    )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var logger = executionContext.GetLogger(nameof(ProcessUserAuthUpdateHttp));

        HttpResponseData responseData = requestData.CreateResponse();

        UserAuthUpdateQueueItem? requestItem;
        try
        {
            requestItem = await JsonSerializer.DeserializeAsync(
                utf8Json: requestData.Body,
                jsonTypeInfo: QueueJsonContext.Default.UserAuthUpdateQueueItem
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while deserializing the request body.");

            responseData.StatusCode = System.Net.HttpStatusCode.BadRequest;
            await responseData.WriteStringAsync("An error occurred while deserializing the request body.");
            return responseData;
        }

        if (requestItem is null || requestItem.UserPrincipalName is null)
        {
            var requestItemNullException = new ArgumentNullException(nameof(requestItem));
            logger.LogError(requestItemNullException, "The request body was null.");

            responseData.StatusCode = System.Net.HttpStatusCode.BadRequest;
            await responseData.WriteStringAsync("The request body was null.");
            return responseData;
        }

        logger.LogInformation("Received request for {UserPrincipalName}.", requestItem.UserPrincipalName);

        User user;
        try
        {
            user = await _graphClientService.GetUserAsync(requestItem.UserPrincipalName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting user, {UserPrincipalName}.", requestItem.UserPrincipalName);

            responseData.StatusCode = System.Net.HttpStatusCode.NotFound;
            await responseData.WriteStringAsync("Could not get user in the request.");
            return responseData;
        }

        if (requestItem.EmailAddress is not null)
        {
            EmailAuthenticationMethod[]? emailAuthMethods = await _graphClientService.GetEmailAuthenticationMethodsAsync(user.Id);

            if (emailAuthMethods is not null && emailAuthMethods.Length != 0)
            {
                logger.LogWarning("'{UserPrincipalName}' already has email auth methods configured. Skipping...", requestItem.UserPrincipalName);
            }
            else
            {
                try
                {
                    await _graphClientService.AddEmailAuthenticationMethodAsync(
                        userId: user.Id,
                        emailAddress: requestItem.EmailAddress
                    );

                    logger.LogInformation("Added email auth method for {UserPrincipalName}.", requestItem.UserPrincipalName);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error adding email auth method for {UserPrincipalName}.", requestItem.UserPrincipalName);

                    responseData.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await responseData.WriteStringAsync($"Ran into an error while adding email auth method: {e.Message}");
                    return responseData;
                }
            }
        }
        else
        {
            logger.LogWarning("'{UserPrincipalName}' did not have an email address supplied in the request. Skipping...", requestItem.UserPrincipalName);
        }

        if (requestItem.PhoneNumber is not null)
        {
            PhoneAuthenticationMethod[]? phoneAuthMethods = await _graphClientService.GetPhoneAuthenticationMethodsAsync(user.Id);

            if (phoneAuthMethods is not null && phoneAuthMethods.Length != 0)
            {
                logger.LogWarning("'{UserPrincipalName}' already has phone auth methods configured. Skipping...", requestItem.UserPrincipalName);
            }
            else
            {
                try
                {
                    await _graphClientService.AddPhoneAuthenticationMethodAsync(
                        userId: user.Id,
                        phoneNumber: requestItem.PhoneNumber
                    );

                    logger.LogInformation("Added phone auth method for {UserPrincipalName}.", requestItem.UserPrincipalName);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error adding phone auth method for {UserPrincipalName}.", requestItem.UserPrincipalName);

                    responseData.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await responseData.WriteStringAsync($"Ran into an error while adding phone auth method: {e.Message}");
                    return responseData;
                }
            }
        }
        else
        {
            logger.LogWarning("'{UserPrincipalName}' did not have a phone number supplied in the request. Skipping...", requestItem.UserPrincipalName);
        }

        stopwatch.Stop();
        logger.LogInformation("Processed request for {UserPrincipalName} in {ElapsedMilliseconds}ms.", requestItem.UserPrincipalName, stopwatch.ElapsedMilliseconds);

        responseData.StatusCode = System.Net.HttpStatusCode.OK;
        return responseData;
    }
}