_⚠ This is a hackathon project with no support or quality guarantee_

# Simplify NuGet push!

This project is a prototype to make it easier to push packages... and way more secure too!

## Device Flow authentication

[Device Flow](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Device-Code-Flow) prompts you to login by providing a link and a code:

> To sign in, use a web browser to open the page https://microsoft.com/devicelogin and enter the code ABC to authenticate.

The user opens their browser, navigates to the page, enters the code, and logs in using their Microsoft account. Once logged in, the app receives a token that can used to authenticate on behalf of the user.

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

### Source code

You can find the following projects in the `src` directory:

* `FakeGet` - A minimal NuGet client to prototype new authentication mechanisms
* `NuGetServer` - A minimal NuGet server to prototype new authentication mechanisms
* `TestClient` and `TestServer` - Minimal apps to test Azure Active Directory's device flow authentication

### Links

* [Intro to Device Code Flow](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Device-Code-Flow)
* [Create AAD resources for device flow authentication](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/tree/master/1.%20Desktop%20app%20calls%20Web%20API)
* [Azure's device flow SDK](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.devicecodecredential?view=azure-dotnet)

## Leaked API keys



## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
