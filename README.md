# Microsoft Entra ID MFA/SSPR PreFillinator

> ⚠️ **Note:**
>
> This is a work-in-progress.

Pre-fill/pre-populate a mobile phone number and/or email address for users, in your ~~Azure AD~~ Entra ID directory, to use with MFA and/or self-service password reset (SSPR).

## 🤔 How it works

Instead of populating the mobile phone number and other emails attributes, this will use the Microsoft Graph API endpoints for managing both the [Phone](https://learn.microsoft.com/en-us/graph/api/resources/phoneauthenticationmethod?view=graph-rest-1.0) and [Email](https://learn.microsoft.com/en-us/graph/api/resources/emailauthenticationmethod?view=graph-rest-1.0) authentication methods for users. This prevents their personal phone number or email address from being visible to everyone.

It can be invoked either through:

* A message being dropped into an Azure Storage Queue.
* A HTTP POST request to `/api/authenticationMethods`.

This is a sample JSON document to use for both an Azure Storage Queue message or in a HTTP POST request body:

```json
{
    "userPrincipalName": "jeff.winger@greendalecc.edu",
    "emailAddress": "tangolawyer10@gmail.com",
    "phoneNumber": "+1 5555555555"
}
```

![A GIF of Jeff Winger, from the show Community, saying "I thought I told you to stop reading my email".](https://cdn.smalls.online/images/misc/stop-reading-my-email-jeff-winger.gif)

## 🧑‍💻 Development

Tools required:

* [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)
* [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)

## 🤝 License

This project is licensed under the [MIT License](./LICENSE).
