# Simplify NuGet push!

You can find the following projects here:

* FakeGet - A minimal NuGet client to prototype new authentication mechanisms
* NuGetServer - A minimal NuGet server to prototype new authentication mechanisms
* TestClient and TestServer - Minimal apps to test Azure Active Directory's deviceflow authentication

## Device Flow

Device Flow prompts you to login by providing a link and a code:

> To sign in, use a web browser to open the page https://microsoft.com/devicelogin and enter the code ABC to authenticate.

The user opens their browser, navigates to the page, enters the code, and logs in using their Microsoft account. Once logged in, the app receives a token that it can used to authenticate on behalf of the user.

### NuGet Demo

Prerequisites:

1. Make sure the NuGet server is on (it uses Azure free tier and turns off if there's no activity)
    1. Navigate to: https://loshar-auth-wus2.azurewebsites.net/
    1. Wait until the page loads...
1. Install the fake NuGet client: `dotnet tool install --global FakeGet --version 0.1.0`

Now push a package with interactive mode enabled:

```ps1
fakeget push <package.nupkg> -s https://loshar-auth-wus2.azurewebsites.net/v3/index.json --interactive
```

This will prompt you to login before uploading the package.

### Links

* [Intro to Device Code Flow](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Device-Code-Flow)
* [Create AAD resources for device flow authentication](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/tree/master/1.%20Desktop%20app%20calls%20Web%20API)
* [Azure's device flow SDK](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.devicecodecredential?view=azure-dotnet)