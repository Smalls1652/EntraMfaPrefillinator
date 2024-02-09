# Microsoft Entra ID MFA/SSPR PreFillinator

[![Build status on the "main" branch](https://github.com/Smalls1652/EntraMfaPrefillinator/actions/workflows/build.yml/badge.svg?branch=main&event=push)](https://github.com/Smalls1652/EntraMfaPrefillinator/actions/workflows/build.yml)

> ⚠️ **Note:**
>
> This is a work-in-progress.

Pre-fill/pre-populate a mobile phone number and/or email address for users, in your ~~Azure AD~~ Entra ID directory, to use with MFA and/or self-service password reset (SSPR).

## 🤔 How it works

Instead of populating the mobile phone number and other emails attributes, this will use the Microsoft Graph API endpoints for managing both the [Phone](https://learn.microsoft.com/en-us/graph/api/resources/phoneauthenticationmethod?view=graph-rest-1.0) and [Email](https://learn.microsoft.com/en-us/graph/api/resources/emailauthenticationmethod?view=graph-rest-1.0) authentication methods for users. This prevents their personal phone number or email address from being visible to everyone.

![A GIF of Jeff Winger, from the show Community, saying "I thought I told you to stop reading my email".](https://cdn.smalls.online/images/misc/stop-reading-my-email-jeff-winger.gif)

## 🧑‍💻 Development

Tools required:

* [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)

## 🤝 License

This project is licensed under the [MIT License](./LICENSE).
